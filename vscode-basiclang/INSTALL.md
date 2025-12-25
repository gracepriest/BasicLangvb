# BasicLang VS Code Extension - Installation Guide

## Prerequisites

1. **Visual Studio Code** - Version 1.75.0 or higher
2. **.NET SDK** - .NET 8.0 or later
3. **BasicLang Compiler** - The BasicLang compiler project

## Installation Steps

### For Development (Testing the Extension)

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd vscode-basiclang
   ```

2. **Install Dependencies**
   ```bash
   npm install
   ```

3. **Configure the Extension**
   - Open VS Code settings (File > Preferences > Settings)
   - Search for "BasicLang"
   - Set `basiclang.server.projectPath` to your BasicLang compiler directory
     - Example: `C:\Users\YourName\source\repos\BasicLang\BasicLang`

4. **Launch the Extension**
   - Open the `vscode-basiclang` folder in VS Code
   - Press `F5` to start debugging
   - A new VS Code window will open with the extension loaded

5. **Test the Extension**
   - In the new window, open a `.bas`, `.basic`, or `.bl` file
   - Try the snippets, commands, and other features

### For Production Use (Package as VSIX)

1. **Install vsce (VS Code Extension Manager)**
   ```bash
   npm install -g @vscode/vsce
   ```

2. **Package the Extension**
   ```bash
   cd vscode-basiclang
   vsce package
   ```
   This creates a `.vsix` file in the directory.

3. **Install the Extension**
   - In VS Code, go to Extensions view (Ctrl+Shift+X)
   - Click the "..." menu at the top
   - Select "Install from VSIX..."
   - Choose the generated `.vsix` file

## Post-Installation Setup

### 1. Configure Your Workspace

Create or update your `.vscode/settings.json` in your BasicLang project:

```json
{
  "basiclang.server.projectPath": "C:\\Path\\To\\BasicLang\\Compiler",
  "basiclang.compiler.backend": "csharp",
  "basiclang.compiler.outputPath": "./out",
  "basiclang.compiler.optimizationLevel": "basic"
}
```

### 2. Add Build Tasks (Optional)

Copy the tasks template:
```bash
cp tasks.json.template .vscode/tasks.json
```

Or manually create `.vscode/tasks.json` in your project and paste the contents from `tasks.json.template`.

### 3. Add Debug Configuration (Optional)

Copy the launch configuration template:
```bash
cp launch.json.template .vscode/launch.json
```

Or manually create `.vscode/launch.json` in your project and paste the contents from `launch.json.template`.

## Verify Installation

1. **Check Extension is Active**
   - Open a BasicLang file (`.bas`, `.basic`, or `.bl`)
   - Look for syntax highlighting
   - Check the status bar for the backend indicator (e.g., "CSHARP")

2. **Test Language Server**
   - Type some BasicLang code
   - Verify autocomplete works (Ctrl+Space)
   - Check for error diagnostics

3. **Test Commands**
   - Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
   - Type "BasicLang"
   - You should see all the BasicLang commands

4. **Test Snippets**
   - Create a new `.bas` file
   - Type `class` and press Tab
   - A class snippet should expand

## Troubleshooting

### Extension Not Loading

- Check that VS Code version is 1.75.0 or higher
- Verify the extension is installed (Extensions view)
- Check the Output panel (View > Output) and select "BasicLang" from the dropdown

### Language Server Not Starting

1. Verify `.NET SDK` is installed:
   ```bash
   dotnet --version
   ```

2. Check the `basiclang.server.projectPath` setting points to the correct directory

3. Look for errors in the BasicLang output channel:
   - View > Output
   - Select "BasicLang" from the dropdown

4. Try restarting the language server:
   - Command Palette (Ctrl+Shift+P)
   - Run "BasicLang: Restart Language Server"

### Snippets Not Working

- Make sure you're in a BasicLang file (`.bas`, `.basic`, or `.bl`)
- Check file association in the bottom-right corner of VS Code
- If it shows "Plain Text", click it and select "BasicLang"

### Commands Not Appearing

- Reload VS Code (Developer: Reload Window)
- Check for extension activation events in package.json
- Verify the extension is not disabled

### Build/Run Commands Fail

1. Ensure the BasicLang compiler project path is correct
2. Verify you can run the compiler manually:
   ```bash
   cd <BasicLang-Compiler-Path>
   dotnet run -- --help
   ```
3. Check the BasicLang output channel for error messages

## Updating the Extension

### Development Version
```bash
cd vscode-basiclang
git pull
npm install
# Press F5 to reload
```

### Production Version
1. Uninstall the current version
2. Install the new `.vsix` file
3. Reload VS Code

## Getting Help

- Check [FEATURES.md](FEATURES.md) for detailed feature documentation
- Review [README.md](README.md) for general information
- Look at the sample files in the `test` folder
- Check the BasicLang output channel for error messages

## Additional Configuration

### Windows-Specific Settings

If you're on Windows and paths contain spaces, make sure to use double backslashes:

```json
{
  "basiclang.server.projectPath": "C:\\Program Files\\BasicLang\\Compiler"
}
```

### macOS/Linux-Specific Settings

Use forward slashes for paths:

```json
{
  "basiclang.server.projectPath": "/usr/local/basiclang/compiler"
}
```

### Multi-Root Workspaces

For multi-root workspaces, configure settings per folder:

1. Right-click on a folder in the Explorer
2. Select "Settings"
3. Configure BasicLang settings for that specific folder

---

For more information, see the [README.md](README.md) and [FEATURES.md](FEATURES.md) files.
