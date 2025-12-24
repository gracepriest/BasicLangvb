# BasicLang VS Code Extension

Language support for the BasicLang programming language.

## Features

- **Syntax Highlighting** - Full syntax highlighting for BasicLang code
- **Error Diagnostics** - Real-time error and warning highlighting
- **Autocomplete** - Code completion for keywords, built-in functions, and types
- **Hover Documentation** - Documentation on hover for symbols
- **Go to Definition** - Navigate to function/class definitions
- **Document Outline** - View document symbols in the outline panel

## Requirements

- .NET 8.0 or later (for the language server)
- BasicLang compiler project

## Setup

1. Open this folder in VS Code
2. Press F5 to launch the extension in debug mode
3. Open a `.bas`, `.basic`, or `.bl` file

## Configuration

The extension supports the following settings:

- `basiclang.server.path` - Path to the BasicLang compiler executable
- `basiclang.server.projectPath` - Path to the BasicLang compiler project directory
- `basiclang.trace.server` - Trace level for debugging (off/messages/verbose)

## Commands

- **BasicLang: Restart Language Server** - Restart the LSP server
- **BasicLang: Show Output** - Show the extension output channel

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
