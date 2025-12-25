# Changelog - BasicLang VS Code Extension

## [0.1.0] - Enhanced Features Release

This release adds comprehensive enhancements to the BasicLang VS Code extension, making it a full-featured development environment for BasicLang programming.

### Added Features

#### 1. Code Snippets
- **File**: `snippets/basiclang.json`
- **Count**: 25+ snippets for common BasicLang constructs
- **Snippets Include**:
  - Class, Module, Interface, Structure, Enum, Namespace definitions
  - Sub and Function procedures
  - Property definitions (auto, read-only, full)
  - Control structures (If/Then, Select Case, For, While, Do)
  - Exception handling (Try/Catch/Finally)
  - Variable and constant declarations
  - With statements
  - Main entry point

#### 2. Build & Run Commands
- **Files Modified**: `package.json`, `extension.js`
- **New Commands**:
  - `basiclang.build` - Compile the current BasicLang file
  - `basiclang.run` - Execute the current BasicLang file
  - `basiclang.buildAndRun` - Compile and run in sequence
  - `basiclang.formatDocument` - Format the current document
  - `basiclang.selectBackend` - Quick picker to change compilation backend

#### 3. Status Bar Integration
- **Files Modified**: `extension.js`
- **Status Bar Items**:
  - **Backend Indicator** (Right Side)
    - Shows current backend (CSHARP, JAVASCRIPT, PYTHON, LLVM)
    - Click to open backend selector
    - Auto-updates when configuration changes
  - **Error/Warning Counter** (Left Side)
    - Shows count of errors and warnings in BasicLang files
    - Only visible when errors/warnings exist
    - Click to show output channel

#### 4. Compiler Configuration Settings
- **File Modified**: `package.json`
- **New Settings**:
  - `basiclang.compiler.backend` - Backend selection (csharp/javascript/python/llvm)
  - `basiclang.compiler.outputPath` - Output directory for compiled files
  - `basiclang.compiler.optimizationLevel` - Optimization level (none/basic/aggressive)
  - `basiclang.compiler.generateDebugInfo` - Toggle debug info generation
  - `basiclang.compiler.strictMode` - Enable strict type checking
  - `basiclang.compiler.warnings` - Toggle compiler warnings

#### 5. Formatting Settings
- **File Modified**: `package.json`
- **New Settings**:
  - `basiclang.format.indentSize` - Number of spaces for indentation (default: 4)
  - `basiclang.format.insertSpaces` - Use spaces vs tabs (default: true)

#### 6. Enhanced Debug Configuration
- **Files Modified**: `package.json`, `launch.json.template`
- **Enhancements**:
  - Added `backend` parameter to debug configurations
  - Added `args` parameter for command-line arguments
  - Added `cwd` parameter for working directory
  - Created debug configuration snippets for each backend
  - Multiple debug templates in `launch.json.template`

#### 7. Build Tasks
- **File Created**: `tasks.json.template`
- **Tasks**:
  - Build BasicLang - Compile current file (default build task)
  - Run BasicLang - Execute current file
  - Build and Run BasicLang - Compile and execute (default test task)
  - Clean Output - Remove compiled files

#### 8. Documentation
- **Files Created**:
  - `FEATURES.md` - Comprehensive feature documentation
  - `INSTALL.md` - Installation and setup guide
  - `CHANGELOG.md` - This file
- **Files Updated**:
  - `README.md` - Updated with new features and configuration

### Implementation Details

#### Extension.js Enhancements

**New Variables**:
```javascript
let backendStatusBarItem;
let errorStatusBarItem;
let diagnosticCollection;
```

**New Functions**:
- `createStatusBarItems()` - Initialize status bar items
- `updateBackendStatusBar()` - Update backend indicator
- `updateErrorStatusBar()` - Update error/warning counter
- `buildProject()` - Build current file with compiler
- `runProject()` - Execute current file
- `formatDocument()` - Format document via LSP
- `selectBackend()` - Show backend quick picker

**Event Listeners**:
- Configuration change listener for backend updates
- Diagnostic change listener for error count updates

#### Package.json Enhancements

**New Contributions**:
- 5 new commands
- 1 snippet contribution
- 8 new compiler configuration properties
- 2 new format configuration properties
- Enhanced debug configuration attributes
- New debug configuration snippets

### Files Modified

1. **package.json** - Added commands, settings, snippets, enhanced debug config
2. **extension.js** - Added build/run/format commands, status bar items, event listeners

### Files Created

1. **snippets/basiclang.json** - Code snippets
2. **tasks.json.template** - Build task templates
3. **launch.json.template** - Debug configuration templates
4. **FEATURES.md** - Feature documentation
5. **INSTALL.md** - Installation guide
6. **CHANGELOG.md** - This changelog

### Usage Examples

#### Using Snippets
```basic
' Type "class" and press Tab
Class MyClass
    ' Class body
End Class

' Type "function" and press Tab
Function Calculate(x As Integer, y As Integer) As Integer
    Return x + y
End Function
```

#### Building and Running
1. Open a BasicLang file
2. Press `Ctrl+Shift+P` (Command Palette)
3. Type "BasicLang: Build and Run"
4. View output in the output panel

#### Changing Backend
1. Click the backend indicator in status bar (e.g., "CSHARP")
2. Select desired backend from quick picker
3. Backend changes immediately

#### Using Tasks
1. Copy `tasks.json.template` to `.vscode/tasks.json`
2. Press `Ctrl+Shift+B` to build
3. Or run any task via Command Palette

### Dependencies

No new dependencies added. All features use existing packages:
- vscode-languageclient: ^9.0.1
- @vscode/debugadapter: ^1.65.0
- @types/vscode: ^1.75.0

### Breaking Changes

None. All changes are additive and backward compatible.

### Notes for Developers

- Extension remains in JavaScript (no TypeScript compilation needed)
- All new commands integrate with existing language server
- Status bar items are created on activation
- Build/run commands spawn dotnet processes with appropriate arguments
- Configuration changes are reactive and update UI in real-time

### Future Enhancements (Not Implemented)

Potential future additions:
- Problem matchers for build tasks
- Test runner integration
- Code formatting provider (currently delegates to LSP)
- Refactoring commands
- Workspace symbol search
- Multi-file project support

### Testing

To test the enhancements:

1. **Snippets**: Open a `.bas` file and type snippet prefixes
2. **Commands**: Use Command Palette to run build/run commands
3. **Status Bar**: Check backend indicator and error counter
4. **Tasks**: Copy template and run build tasks
5. **Debug**: Copy launch template and start debugging
6. **Settings**: Change compiler/format settings and verify behavior

### Known Issues

- Build/run commands require `basiclang.server.projectPath` to be configured
- Compiler must support the command-line arguments used by build/run
- Error counter only counts BasicLang files (.bas, .basic, .bl)

---

For more information, see:
- [README.md](README.md) - Overview and basic setup
- [FEATURES.md](FEATURES.md) - Detailed feature documentation
- [INSTALL.md](INSTALL.md) - Installation and troubleshooting guide
