# BasicLang VS Code Extension

Language support for the BasicLang programming language.

## Features

### Language Support
- **Syntax Highlighting** - Full syntax highlighting for BasicLang code
- **Error Diagnostics** - Real-time error and warning highlighting
- **Autocomplete** - Code completion for keywords, built-in functions, and types
- **Hover Documentation** - Documentation on hover for symbols
- **Go to Definition** - Navigate to function/class definitions
- **Document Outline** - View document symbols in the outline panel

### Code Snippets
- 25+ code snippets for common BasicLang constructs
- Type prefixes like `class`, `function`, `if`, `for`, `while`, etc.
- See [FEATURES.md](FEATURES.md) for complete list

### Build & Run
- **Build Command** - Compile BasicLang files with configurable backends
- **Run Command** - Execute BasicLang programs directly
- **Build and Run** - One-click compile and execute
- **Multiple Backends** - Support for C#, JavaScript, Python, and LLVM

### Status Bar Integration
- **Backend Indicator** - Shows current compilation backend (click to change)
- **Error Counter** - Real-time error and warning count display

### Debug Support
- Breakpoint debugging for BasicLang programs
- Multiple debug configurations for different backends
- Step-through execution and variable inspection

## Requirements

- .NET 8.0 or later (for the language server)
- BasicLang compiler project

## Setup

1. Open this folder in VS Code
2. Press F5 to launch the extension in debug mode
3. Open a `.bas`, `.basic`, or `.bl` file

## Configuration

The extension supports the following settings:

### Language Server
- `basiclang.server.path` - Path to the BasicLang compiler executable
- `basiclang.server.projectPath` - Path to the BasicLang compiler project directory
- `basiclang.trace.server` - Trace level for debugging (off/messages/verbose)

### Compiler Options
- `basiclang.compiler.backend` - Code generation backend (csharp/javascript/python/llvm)
- `basiclang.compiler.outputPath` - Output directory for compiled files
- `basiclang.compiler.optimizationLevel` - Optimization level (none/basic/aggressive)
- `basiclang.compiler.generateDebugInfo` - Generate debug information
- `basiclang.compiler.strictMode` - Enable strict type checking
- `basiclang.compiler.warnings` - Show compiler warnings

### Formatting
- `basiclang.format.indentSize` - Number of spaces for indentation
- `basiclang.format.insertSpaces` - Use spaces instead of tabs

See [FEATURES.md](FEATURES.md) for detailed configuration information.

## Commands

Access via Command Palette (Ctrl+Shift+P):

- **BasicLang: Build** - Compile the current file
- **BasicLang: Run** - Run the current file
- **BasicLang: Build and Run** - Compile and run in one step
- **BasicLang: Format Document** - Format the current document
- **BasicLang: Select Backend** - Choose compilation backend
- **BasicLang: Restart Language Server** - Restart the LSP server
- **BasicLang: Show Output** - Show the extension output channel

## Tasks & Debug Configuration

1. Copy `tasks.json.template` to `.vscode/tasks.json` in your project for build tasks
2. Copy `launch.json.template` to `.vscode/launch.json` in your project for debug configurations

These templates provide pre-configured tasks and debug settings for BasicLang development.

## Development

```bash
# Install dependencies
npm install

# Debug the extension
# Press F5 in VS Code
```

## File Extensions

- `.bas` - BasicLang source file
- `.basic` - BasicLang source file
- `.bl` - BasicLang source file
