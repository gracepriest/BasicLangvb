const vscode = require('vscode');
const path = require('path');
const { spawn } = require('child_process');
const {
    LanguageClient,
    TransportKind,
    StreamInfo
} = require('vscode-languageclient/node');

let client;
let outputChannel;
let projectPath;
let backendStatusBarItem;
let errorStatusBarItem;
let diagnosticCollection;

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {
    outputChannel = vscode.window.createOutputChannel('BasicLang');
    outputChannel.appendLine('BasicLang extension activating...');

    const config = vscode.workspace.getConfiguration('basiclang');

    // Create diagnostic collection
    diagnosticCollection = vscode.languages.createDiagnosticCollection('basiclang');
    context.subscriptions.push(diagnosticCollection);

    // Create status bar items
    createStatusBarItems(context);

    // Get the server path from settings or use default
    let serverPath = config.get('server.path');
    projectPath = config.get('server.projectPath');

    // If no explicit path, try to find the compiler
    if (!serverPath && !projectPath) {
        // Try to find the BasicLang project relative to the extension
        const extensionPath = context.extensionPath;
        const possiblePaths = [
            path.join(extensionPath, '..', 'BasicLang'),  // Sibling to extension
            path.join(extensionPath, '..', '..', 'BasicLang', 'BasicLang'),  // Up two levels
            'C:\\Users\\melvi\\source\\repos\\BasicLang\\BasicLang'  // Hardcoded fallback
        ];

        for (const p of possiblePaths) {
            try {
                const fs = require('fs');
                if (fs.existsSync(path.join(p, 'BasicLang.csproj'))) {
                    projectPath = p;
                    outputChannel.appendLine(`Found BasicLang project at: ${p}`);
                    break;
                }
            } catch (e) {
                // Continue searching
            }
        }
    }

    if (!serverPath && !projectPath) {
        vscode.window.showErrorMessage(
            'BasicLang: Could not find the BasicLang compiler. Please set basiclang.server.projectPath in settings.'
        );
        return;
    }

    // Server options - launch the LSP server
    const serverOptions = () => {
        return new Promise((resolve, reject) => {
            let serverProcess;

            if (serverPath) {
                // Use explicit server executable
                outputChannel.appendLine(`Starting LSP server: ${serverPath}`);
                serverProcess = spawn(serverPath, ['--lsp'], {
                    stdio: ['pipe', 'pipe', 'pipe']
                });
            } else {
                // Use dotnet run
                outputChannel.appendLine(`Starting LSP server via dotnet run in: ${projectPath}`);
                serverProcess = spawn('dotnet', ['run', '--', '--lsp'], {
                    cwd: projectPath,
                    stdio: ['pipe', 'pipe', 'pipe'],
                    shell: true
                });
            }

            serverProcess.on('error', (err) => {
                outputChannel.appendLine(`Failed to start server: ${err.message}`);
                reject(err);
            });

            serverProcess.stderr.on('data', (data) => {
                outputChannel.appendLine(`[Server stderr]: ${data.toString()}`);
            });

            serverProcess.on('exit', (code, signal) => {
                outputChannel.appendLine(`Server process exited with code ${code}, signal ${signal}`);
            });

            // Give the server a moment to start
            setTimeout(() => {
                resolve({
                    writer: serverProcess.stdin,
                    reader: serverProcess.stdout,
                    detached: false
                });
            }, 1000);
        });
    };

    // Client options
    const clientOptions = {
        documentSelector: [
            { scheme: 'file', language: 'basiclang' }
        ],
        synchronize: {
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.{bas,basic,bl}')
        },
        outputChannel: outputChannel,
        traceOutputChannel: outputChannel
    };

    // Create the language client
    client = new LanguageClient(
        'basiclang',
        'BasicLang Language Server',
        serverOptions,
        clientOptions
    );

    // Start the client
    outputChannel.appendLine('Starting BasicLang language client...');
    client.start().then(() => {
        outputChannel.appendLine('BasicLang language client started successfully!');
        vscode.window.showInformationMessage('BasicLang Language Server started');
    }).catch((error) => {
        outputChannel.appendLine(`Failed to start client: ${error}`);
        vscode.window.showErrorMessage(`BasicLang: Failed to start language server: ${error.message}`);
    });

    // Register debug adapter
    const debugAdapterFactory = new BasicLangDebugAdapterFactory();
    context.subscriptions.push(
        vscode.debug.registerDebugAdapterDescriptorFactory('basiclang', debugAdapterFactory)
    );

    // Register commands
    context.subscriptions.push(
        vscode.commands.registerCommand('basiclang.restartServer', async () => {
            outputChannel.appendLine('Restarting language server...');
            if (client) {
                await client.stop();
                await client.start();
                vscode.window.showInformationMessage('BasicLang Language Server restarted');
            }
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('basiclang.showOutput', () => {
            outputChannel.show();
        })
    );

    // Register build command
    context.subscriptions.push(
        vscode.commands.registerCommand('basiclang.build', async () => {
            await buildProject();
        })
    );

    // Register run command
    context.subscriptions.push(
        vscode.commands.registerCommand('basiclang.run', async () => {
            await runProject();
        })
    );

    // Register build and run command
    context.subscriptions.push(
        vscode.commands.registerCommand('basiclang.buildAndRun', async () => {
            if (await buildProject()) {
                await runProject();
            }
        })
    );

    // Register format document command
    context.subscriptions.push(
        vscode.commands.registerCommand('basiclang.formatDocument', async () => {
            await formatDocument();
        })
    );

    // Register select backend command
    context.subscriptions.push(
        vscode.commands.registerCommand('basiclang.selectBackend', async () => {
            await selectBackend();
        })
    );

    // Listen for configuration changes
    context.subscriptions.push(
        vscode.workspace.onDidChangeConfiguration(e => {
            if (e.affectsConfiguration('basiclang.compiler.backend')) {
                updateBackendStatusBar();
            }
        })
    );

    // Listen for diagnostics updates
    context.subscriptions.push(
        vscode.languages.onDidChangeDiagnostics(() => {
            updateErrorStatusBar();
        })
    );
}

/**
 * Debug Adapter Factory for BasicLang
 */
class BasicLangDebugAdapterFactory {
    createDebugAdapterDescriptor(session, executable) {
        outputChannel.appendLine('Creating debug adapter...');

        // Launch the debug adapter
        let debugProcess;

        if (projectPath) {
            outputChannel.appendLine(`Starting debug adapter via dotnet run in: ${projectPath}`);
            debugProcess = spawn('dotnet', ['run', '--', '--debug-adapter'], {
                cwd: projectPath,
                stdio: ['pipe', 'pipe', 'pipe'],
                shell: true
            });
        } else {
            outputChannel.appendLine('No project path configured for debug adapter');
            return undefined;
        }

        debugProcess.on('error', (err) => {
            outputChannel.appendLine(`Failed to start debug adapter: ${err.message}`);
        });

        debugProcess.stderr.on('data', (data) => {
            outputChannel.appendLine(`[Debug Adapter stderr]: ${data.toString()}`);
        });

        debugProcess.on('exit', (code, signal) => {
            outputChannel.appendLine(`Debug adapter exited with code ${code}, signal ${signal}`);
        });

        return new vscode.DebugAdapterInlineImplementation(
            new BasicLangDebugAdapter(debugProcess)
        );
    }
}

/**
 * Inline Debug Adapter that wraps the external process
 */
class BasicLangDebugAdapter {
    constructor(process) {
        this._process = process;
        this._sendMessage = null;
    }

    handleMessage(message) {
        // Forward message to debug adapter process
        const json = JSON.stringify(message);
        const header = `Content-Length: ${Buffer.byteLength(json)}\r\n\r\n`;
        this._process.stdin.write(header + json);
    }

    start(sendMessage) {
        this._sendMessage = sendMessage;

        // Handle incoming messages from debug adapter
        let buffer = '';
        this._process.stdout.on('data', (data) => {
            buffer += data.toString();

            while (true) {
                const headerEnd = buffer.indexOf('\r\n\r\n');
                if (headerEnd === -1) break;

                const header = buffer.substring(0, headerEnd);
                const lengthMatch = header.match(/Content-Length: (\d+)/);
                if (!lengthMatch) break;

                const contentLength = parseInt(lengthMatch[1]);
                const messageStart = headerEnd + 4;
                const messageEnd = messageStart + contentLength;

                if (buffer.length < messageEnd) break;

                const content = buffer.substring(messageStart, messageEnd);
                buffer = buffer.substring(messageEnd);

                try {
                    const message = JSON.parse(content);
                    this._sendMessage(message);
                } catch (e) {
                    outputChannel.appendLine(`Error parsing debug message: ${e.message}`);
                }
            }
        });
    }

    dispose() {
        if (this._process) {
            this._process.kill();
        }
    }
}

/**
 * Create and initialize status bar items
 */
function createStatusBarItems(context) {
    // Backend status bar item
    backendStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    backendStatusBarItem.command = 'basiclang.selectBackend';
    backendStatusBarItem.tooltip = 'Click to change backend';
    context.subscriptions.push(backendStatusBarItem);
    updateBackendStatusBar();
    backendStatusBarItem.show();

    // Error count status bar item
    errorStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
    errorStatusBarItem.command = 'basiclang.showOutput';
    context.subscriptions.push(errorStatusBarItem);
    updateErrorStatusBar();
}

/**
 * Update backend status bar item
 */
function updateBackendStatusBar() {
    const config = vscode.workspace.getConfiguration('basiclang');
    const backend = config.get('compiler.backend', 'csharp');
    backendStatusBarItem.text = `$(code) ${backend.toUpperCase()}`;
}

/**
 * Update error count status bar item
 */
function updateErrorStatusBar() {
    const diagnostics = vscode.languages.getDiagnostics();
    let errorCount = 0;
    let warningCount = 0;

    for (const [uri, diags] of diagnostics) {
        if (uri.toString().endsWith('.bas') || uri.toString().endsWith('.basic') || uri.toString().endsWith('.bl')) {
            for (const diag of diags) {
                if (diag.severity === vscode.DiagnosticSeverity.Error) {
                    errorCount++;
                } else if (diag.severity === vscode.DiagnosticSeverity.Warning) {
                    warningCount++;
                }
            }
        }
    }

    if (errorCount > 0 || warningCount > 0) {
        errorStatusBarItem.text = `$(error) ${errorCount} $(warning) ${warningCount}`;
        errorStatusBarItem.show();
    } else {
        errorStatusBarItem.hide();
    }
}

/**
 * Build the current project
 */
async function buildProject() {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showErrorMessage('No active BasicLang file');
        return false;
    }

    const document = editor.document;
    if (document.languageId !== 'basiclang') {
        vscode.window.showErrorMessage('Current file is not a BasicLang file');
        return false;
    }

    await document.save();

    const config = vscode.workspace.getConfiguration('basiclang');
    const backend = config.get('compiler.backend', 'csharp');
    const outputPath = config.get('compiler.outputPath', './out');
    const optimizationLevel = config.get('compiler.optimizationLevel', 'basic');
    const generateDebugInfo = config.get('compiler.generateDebugInfo', true);
    const strictMode = config.get('compiler.strictMode', false);

    outputChannel.clear();
    outputChannel.show(true);
    outputChannel.appendLine('Building BasicLang project...');
    outputChannel.appendLine(`Backend: ${backend}`);
    outputChannel.appendLine(`File: ${document.fileName}`);

    return new Promise((resolve) => {
        const args = [
            'run', '--',
            '--compile', document.fileName,
            '--backend', backend,
            '--output', outputPath,
            '--optimization', optimizationLevel
        ];

        if (generateDebugInfo) {
            args.push('--debug');
        }

        if (strictMode) {
            args.push('--strict');
        }

        const buildProcess = spawn('dotnet', args, {
            cwd: projectPath,
            shell: true
        });

        buildProcess.stdout.on('data', (data) => {
            outputChannel.append(data.toString());
        });

        buildProcess.stderr.on('data', (data) => {
            outputChannel.append(data.toString());
        });

        buildProcess.on('close', (code) => {
            if (code === 0) {
                outputChannel.appendLine('\nBuild succeeded!');
                vscode.window.showInformationMessage('BasicLang: Build succeeded');
                resolve(true);
            } else {
                outputChannel.appendLine(`\nBuild failed with exit code ${code}`);
                vscode.window.showErrorMessage('BasicLang: Build failed');
                resolve(false);
            }
        });

        buildProcess.on('error', (err) => {
            outputChannel.appendLine(`\nBuild error: ${err.message}`);
            vscode.window.showErrorMessage(`BasicLang: Build error - ${err.message}`);
            resolve(false);
        });
    });
}

/**
 * Run the current project
 */
async function runProject() {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showErrorMessage('No active BasicLang file');
        return;
    }

    const document = editor.document;
    if (document.languageId !== 'basiclang') {
        vscode.window.showErrorMessage('Current file is not a BasicLang file');
        return;
    }

    const config = vscode.workspace.getConfiguration('basiclang');
    const backend = config.get('compiler.backend', 'csharp');
    const outputPath = config.get('compiler.outputPath', './out');

    outputChannel.clear();
    outputChannel.show(true);
    outputChannel.appendLine('Running BasicLang program...');
    outputChannel.appendLine(`Backend: ${backend}`);

    return new Promise((resolve) => {
        const args = ['run', '--', '--run', document.fileName, '--backend', backend];

        const runProcess = spawn('dotnet', args, {
            cwd: projectPath,
            shell: true
        });

        runProcess.stdout.on('data', (data) => {
            outputChannel.append(data.toString());
        });

        runProcess.stderr.on('data', (data) => {
            outputChannel.append(data.toString());
        });

        runProcess.on('close', (code) => {
            outputChannel.appendLine(`\nProgram exited with code ${code}`);
            resolve(code === 0);
        });

        runProcess.on('error', (err) => {
            outputChannel.appendLine(`\nRun error: ${err.message}`);
            vscode.window.showErrorMessage(`BasicLang: Run error - ${err.message}`);
            resolve(false);
        });
    });
}

/**
 * Format the current document
 */
async function formatDocument() {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showErrorMessage('No active BasicLang file');
        return;
    }

    const document = editor.document;
    if (document.languageId !== 'basiclang') {
        vscode.window.showErrorMessage('Current file is not a BasicLang file');
        return;
    }

    // Use the language server's formatting if available
    try {
        await vscode.commands.executeCommand('editor.action.formatDocument');
    } catch (err) {
        vscode.window.showErrorMessage(`Format error: ${err.message}`);
    }
}

/**
 * Select compilation backend
 */
async function selectBackend() {
    const backends = ['csharp', 'javascript', 'python', 'llvm'];
    const selected = await vscode.window.showQuickPick(backends, {
        placeHolder: 'Select a compilation backend'
    });

    if (selected) {
        const config = vscode.workspace.getConfiguration('basiclang');
        await config.update('compiler.backend', selected, vscode.ConfigurationTarget.Workspace);
        vscode.window.showInformationMessage(`Backend changed to ${selected.toUpperCase()}`);
    }
}

function deactivate() {
    if (outputChannel) {
        outputChannel.appendLine('BasicLang extension deactivating...');
    }
    if (client) {
        return client.stop();
    }
    return undefined;
}

module.exports = {
    activate,
    deactivate
};
