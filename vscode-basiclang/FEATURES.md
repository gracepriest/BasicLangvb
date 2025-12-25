# BasicLang VS Code Extension - Features

This extension provides comprehensive support for the BasicLang programming language in Visual Studio Code.

## Features

### 1. Syntax Highlighting
- Full syntax highlighting for BasicLang files (.bas, .basic, .bl)
- Semantic token support for better code understanding
- Custom language configuration with auto-closing pairs and brackets

### 2. Code Snippets
The extension includes snippets for common BasicLang constructs:

- **class** - Class definition
- **sub** - Sub procedure
- **function** - Function definition
- **if** / **ife** / **ifel** - If statements with various forms
- **for** - For...Next loop
- **foreach** - For Each...Next loop
- **while** - While...Wend loop
- **dowhile** / **dountil** - Do loops
- **select** - Select Case statement
- **try** / **tryf** - Try...Catch statements
- **prop** / **propa** / **propr** - Property definitions
- **module** - Module definition
- **interface** - Interface definition
- **struct** - Structure definition
- **enum** - Enumeration
- **namespace** - Namespace declaration
- **dim** - Variable declaration
- **const** - Constant declaration
- **with** - With statement
- **main** - Main entry point

### 3. Commands

Access these commands via the Command Palette (Ctrl+Shift+P / Cmd+Shift+P):

- **BasicLang: Build** - Compile the current BasicLang file
- **BasicLang: Run** - Run the current BasicLang file
- **BasicLang: Build and Run** - Compile and run in one step
- **BasicLang: Format Document** - Format the current document
- **BasicLang: Select Backend** - Choose compilation backend (C#, JavaScript, Python, LLVM)
- **BasicLang: Restart Language Server** - Restart the LSP server
- **BasicLang: Show Output** - Show the BasicLang output channel

### 4. Status Bar Items

#### Backend Indicator
- Shows the currently selected compilation backend (e.g., "CSHARP")
- Located on the right side of the status bar
- Click to quickly change the backend

#### Error/Warning Counter
- Shows the number of errors and warnings in BasicLang files
- Located on the left side of the status bar
- Click to show the output channel
- Only visible when there are errors or warnings

### 5. Configuration Settings

Configure BasicLang in your VS Code settings:

#### Language Server
- **basiclang.server.path** - Path to the BasicLang compiler executable
- **basiclang.server.projectPath** - Path to the BasicLang compiler project directory
- **basiclang.trace.server** - Trace LSP communication (off/messages/verbose)

#### Compiler Options
- **basiclang.compiler.backend** - Code generation backend (csharp/javascript/python/llvm)
- **basiclang.compiler.outputPath** - Output directory for compiled files (default: "./out")
- **basiclang.compiler.optimizationLevel** - Optimization level (none/basic/aggressive)
- **basiclang.compiler.generateDebugInfo** - Generate debug information (default: true)
- **basiclang.compiler.strictMode** - Enable strict type checking (default: false)
- **basiclang.compiler.warnings** - Show compiler warnings (default: true)

#### Formatting
- **basiclang.format.indentSize** - Number of spaces for indentation (default: 4)
- **basiclang.format.insertSpaces** - Use spaces instead of tabs (default: true)

### 6. Build Tasks

Copy `tasks.json.template` to `.vscode/tasks.json` in your project to use predefined tasks:

- **Build BasicLang** (Ctrl+Shift+B) - Compile the current file
- **Run BasicLang** - Run the current file
- **Build and Run BasicLang** - Compile and run
- **Clean Output** - Remove compiled files

### 7. Debug Configuration

Copy `launch.json.template` to `.vscode/launch.json` in your project for debugging support:

- **Debug Current File** - Debug with breakpoint support
- **Debug with C# Backend** - Debug using C# backend
- **Debug with JavaScript Backend** - Debug using JavaScript backend
- **Debug with Python Backend** - Debug using Python backend

Debug configuration properties:
- **program** - Path to the BasicLang file to debug
- **stopOnEntry** - Stop on the first line
- **backend** - Backend to use for compilation
- **args** - Command line arguments
- **cwd** - Working directory

### 8. Language Server Protocol (LSP)

The extension includes full LSP support for:
- Code completion
- Go to definition
- Find references
- Hover information
- Diagnostics (errors and warnings)
- Document symbols
- Workspace symbols

## Getting Started

1. Install the extension
2. Open a BasicLang file (.bas, .basic, or .bl)
3. Configure the compiler path in settings:
   - Set `basiclang.server.projectPath` to your BasicLang compiler directory
4. Start coding with full IntelliSense support!

## Quick Tips

- Use snippets by typing the prefix (e.g., "class") and pressing Tab
- Click the backend indicator in the status bar to quickly switch backends
- Use Ctrl+Shift+B (Cmd+Shift+B on macOS) to build your project
- The error counter shows real-time compilation issues

## Requirements

- .NET SDK (for running the BasicLang compiler)
- BasicLang compiler installed

## Extension Settings

All settings can be found under "BasicLang" in VS Code settings (File > Preferences > Settings).

## Known Issues

Please report issues on the GitHub repository.

## Release Notes

### 0.1.0
- Initial release
- Syntax highlighting
- Code snippets
- LSP support
- Build and run commands
- Status bar integration
- Debug support
- Multiple backend support

---

For more information, visit the BasicLang repository.
