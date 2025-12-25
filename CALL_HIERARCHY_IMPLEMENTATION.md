# Call Hierarchy Implementation for BasicLang LSP

## Overview
This document describes the Call Hierarchy feature implementation for the BasicLang Language Server Protocol (LSP).

## Implementation Files

### Main Implementation
**File:** `C:\Users\melvi\source\repos\BasicLang\BasicLang\LSP\CallHierarchyHandler.cs`

This file contains three handler classes that work together to provide call hierarchy functionality:

1. **CallHierarchyPrepareHandler** - Prepares call hierarchy at cursor position
2. **CallHierarchyIncomingHandler** - Finds callers of a function (who calls this?)
3. **CallHierarchyOutgoingHandler** - Finds callees from a function (what does this call?)

### Registration
**File:** `C:\Users\melvi\source\repos\BasicLang\BasicLang\LSP\BasicLangLanguageServer.cs`

The handlers are registered in the language server setup:
- Line 47: `.WithHandler<CallHierarchyPrepareHandler>()`
- Line 48: `.WithHandler<CallHierarchyIncomingHandler>()`
- Line 49: `.WithHandler<CallHierarchyOutgoingHandler>()`

## Features

### 1. CallHierarchyPrepareHandler
**Purpose:** Initialize call hierarchy at the cursor position

**Functionality:**
- Identifies if cursor is on a function or subroutine name
- Validates the symbol is callable (function/sub)
- Returns a `CallHierarchyItem` representing the selected callable
- Supports functions and subs defined at module level or within classes

**Key Methods:**
- `Handle()` - Main entry point for prepare requests
- `FindCallableAtPosition()` - Locates callable nodes by name
- `FindCallableInNode()` - Recursively searches AST for callables
- `CreateCallHierarchyItem()` - Creates LSP CallHierarchyItem from AST node

### 2. CallHierarchyIncomingHandler
**Purpose:** Find all functions/subs that call the target function

**Functionality:**
- Searches all open documents for callers
- Tracks call sites (line/column where calls occur)
- Returns `CallHierarchyIncomingCall` for each unique caller
- Includes multiple call sites if a caller calls the target multiple times

**Key Methods:**
- `Handle()` - Main entry point for incoming call requests
- `FindCallersInDocument()` - Searches a document for callers
- `FindCallersInNode()` - Recursively searches AST nodes
- `FindCallsInBody()` - Finds calls within a function/sub body
- `FindCallsInStatement()` - Analyzes statements for calls
- `FindCallsInExpression()` - Analyzes expressions for calls (recursive)

**Supported Statement Types:**
- Expression statements
- Assignment statements
- If/ElseIf/Else statements
- For/While/Do loops
- ForEach loops
- Return statements
- Variable declarations with initializers

**Supported Expression Types:**
- Direct calls (`FunctionName()`)
- Member calls (`object.Method()`)
- Calls in binary expressions (`a + Function()`)
- Calls in unary expressions (`-Function()`)
- Calls in array access (`array[Function()]`)
- Calls in constructor arguments (`New Type(Function())`)
- Calls in cast expressions (`CType(Function(), Type)`)

### 3. CallHierarchyOutgoingHandler
**Purpose:** Find all functions/subs called by the source function

**Functionality:**
- Analyzes the body of the source function
- Identifies all function/sub calls made
- Groups calls by target name
- Returns `CallHierarchyOutgoingCall` for each unique callee
- Includes multiple call sites if the source calls a target multiple times

**Key Methods:**
- `Handle()` - Main entry point for outgoing call requests
- `FindCallableByName()` - Locates callable definition by name
- `FindCalleesInNode()` - Extracts all calls from function body
- `FindAllCallsInBody()` - Searches function body for calls
- `FindCallsInStatement()` - Analyzes statements (same as incoming handler)
- `FindCallsInExpression()` - Analyzes expressions (same as incoming handler)

## AST Node Support

The implementation works with the following BasicLang AST nodes:

### Callable Nodes
- `FunctionNode` - Functions that return values
- `SubroutineNode` - Procedures that don't return values

### Call Expression Nodes
- `CallExpressionNode` - Function/sub calls
  - `Callee` can be:
    - `IdentifierExpressionNode` - Direct call (e.g., `DoSomething()`)
    - `MemberAccessExpressionNode` - Member call (e.g., `obj.DoSomething()`)

### Container Nodes
- `ClassNode` - Classes containing member functions/subs
- `ModuleNode` - Modules containing functions/subs
- `BlockNode` - Statement blocks

### Statement Nodes (analyzed for calls)
- `ExpressionStatementNode`
- `AssignmentStatementNode`
- `IfStatementNode` (with ElseIf and Else branches)
- `ForLoopNode`, `WhileLoopNode`, `DoLoopNode`, `ForEachLoopNode`
- `ReturnStatementNode`
- `VariableDeclarationNode` (with initializer)

## Call Hierarchy Items

Each callable is represented as a `CallHierarchyItem` with:
- **Name** - Function/sub name
- **Kind** - `SymbolKind.Function` or `SymbolKind.Method`
- **Detail** - Signature including parameters and return type
- **Uri** - Document URI
- **Range** - Full range of the function/sub declaration
- **SelectionRange** - Just the name identifier

Example:
```
Name: "Calculate"
Kind: Function
Detail: "(x As Integer, y As Integer) As Integer"
```

## Usage Example

See `C:\Users\melvi\source\repos\BasicLang\examples\call_hierarchy_example.bas` for a complete example.

### Example Scenario

```vb
Function Main() As Integer
    Dim result As Integer
    result = Calculate(10, 5)
    Return 0
End Function

Function Calculate(x As Integer, y As Integer) As Integer
    Dim sum As Integer
    sum = Add(x, y)
    Return sum
End Function

Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function
```

#### Call Hierarchy for `Calculate`:

**Incoming Calls (Who calls Calculate?):**
- `Main()` at line 3

**Outgoing Calls (What does Calculate call?):**
- `Add()` at line 9

## Implementation Details

### Case Insensitivity
BasicLang is case-insensitive, so all name comparisons use `StringComparison.OrdinalIgnoreCase`.

### Multi-Document Support
The incoming handler searches across all open documents to find callers, supporting cross-file call hierarchies.

### Call Site Tracking
Both incoming and outgoing handlers track exact call locations (line and column) to provide precise navigation.

### Recursive Expression Analysis
The expression analysis is recursive to handle nested calls:
```vb
result = Calculate(Add(5, 3), Multiply(2, 4))
```
This correctly identifies calls to `Calculate`, `Add`, and `Multiply`.

### Generic Support
The implementation handles generic type arguments in `CallExpressionNode.GenericArguments`, though the BasicLang parser may need to populate this.

## Limitations

1. **Built-in Functions**: The handler doesn't create `CallHierarchyItem` for built-in functions like `PrintLine`, `CStr`, etc. It only tracks user-defined functions/subs.

2. **Single File Analysis**: The outgoing handler only searches the current document for callee definitions. Cross-file callees may not resolve.

3. **Dynamic Calls**: Cannot track dynamic calls through delegates, lambdas, or function pointers.

4. **Partial AST**: If the document has parse errors and AST is incomplete, call hierarchy may be incomplete.

5. **Approximate Ranges**: Function body ranges use `Line + 10` as an approximation. The actual end line should ideally come from the parser.

## Testing

To test the implementation:

1. Open `call_hierarchy_example.bas` in VS Code with BasicLang LSP
2. Place cursor on a function name (e.g., `Calculate`)
3. Trigger call hierarchy (usually Shift+Alt+H or right-click → "Peek → Peek Call Hierarchy")
4. Verify:
   - Incoming calls show `Main()`
   - Outgoing calls show `Add()` and `Multiply()`

## Future Enhancements

1. **Cross-file resolution**: Search all workspace files for callees
2. **Built-in function support**: Handle built-in runtime functions
3. **Lambda support**: Track calls through lambda expressions
4. **Delegate support**: Track calls through delegate invocations
5. **Accurate ranges**: Get precise function end positions from parser
6. **Constructor calls**: Track `New` expressions that invoke constructors
7. **Property accessors**: Track property getter/setter calls
8. **Event handlers**: Track event subscriptions and invocations
9. **Performance optimization**: Cache call graphs to avoid re-parsing
10. **Recursive call detection**: Highlight recursive calls specially

## Integration with Other Features

The Call Hierarchy handler integrates with:
- **DocumentManager** - Retrieves parsed document state and AST
- **SymbolService** - Could be extended to share callable lookup logic
- **ReferencesHandler** - Complementary feature (references finds all uses, call hierarchy focuses on call relationships)
- **DefinitionHandler** - Used to navigate from call sites to definitions

## Conclusion

This implementation provides comprehensive call hierarchy support for BasicLang, enabling developers to:
- Understand code structure and dependencies
- Navigate between callers and callees
- Refactor with confidence
- Debug call chains
- Analyze code complexity

The handler follows LSP best practices and integrates seamlessly with the existing BasicLang language server infrastructure.
