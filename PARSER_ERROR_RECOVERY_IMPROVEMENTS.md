# Parser Error Recovery Improvements

This document describes the improvements made to the BasicLang parser to provide better error recovery and more helpful error messages.

## Summary of Changes

The parser has been enhanced with the following features:

1. **Multiple Error Collection** - The parser now collects multiple errors instead of stopping at the first one
2. **Synchronization Points** - When an error occurs, the parser skips to known recovery points
3. **Descriptive Error Messages with Suggestions** - Errors now include context and helpful suggestions
4. **Context Tracking** - Error messages include information about where in the code structure the error occurred
5. **Maximum Error Limit** - Prevents infinite error loops by limiting errors to 100

## Implementation Details

### 1. New Fields in Parser Class

```csharp
private readonly List<ParseError> _errors;           // Stores all collected errors
private readonly Stack<string> _context;             // Tracks parsing context (namespace, class, function, etc.)
private const int MaxErrors = 100;                    // Maximum errors before aborting
private bool _panicMode;                             // Prevents cascading errors during recovery
```

### 2. Error Collection API

```csharp
/// <summary>
/// Gets all parsing errors collected during parsing
/// </summary>
public IReadOnlyList<ParseError> Errors => _errors.AsReadOnly();
```

Usage:
```csharp
var parser = new Parser(tokens);
var ast = parser.Parse();

if (parser.Errors.Count > 0)
{
    foreach (var error in parser.Errors)
    {
        Console.WriteLine(error.ToString());
    }
}
```

### 3. Enhanced ParseException

The `ParseException` class now includes an optional `Suggestion` property:

```csharp
public class ParseException : Exception
{
    public Token Token { get; }
    public string Suggestion { get; }

    public ParseException(string message, Token token, string suggestion = null)
}
```

Example error output:
```
Parse error at line 10, column 5: Expected 'Then' after If condition but found Identifier
  Suggestion: Did you forget 'Then' after the If condition?
```

### 4. ParseError Class

New class to represent collected errors with context:

```csharp
public class ParseError
{
    public string Message { get; }
    public Token Token { get; }
    public string Context { get; }        // e.g., " in Function 'Calculate' -> Class 'MyClass'"
    public string Suggestion { get; }
}
```

Example error output:
```
Error at line 10, column 5 in Function 'Calculate' -> Class 'Calculator': Expected 'End If'
  Suggestion: Make sure your If statement is properly closed with 'End If'.
```

### 5. Context Tracking

The parser now tracks context using a stack as it enters/exits language constructs:

```csharp
private FunctionNode ParseFunction()
{
    // ... parse function header ...
    _context.Push($"Function '{node.Name}'");

    try
    {
        // ... parse function body ...
    }
    finally
    {
        _context.Pop();
    }
}
```

This provides context like:
- `Namespace 'MyApp'`
- `Class 'Calculator'`
- `Function 'Calculate'`
- `Sub 'Main'`

### 6. Synchronization Points

The `Synchronize()` method skips tokens until it reaches a known safe point to resume parsing:

**Synchronization tokens include:**

**Top-level declarations:**
- Namespace, Module, Class, Interface, Enum, Type, Structure
- Function, Sub, Dim, Const, Auto

**Statements:**
- If, For, While, Do, Select, Try, With
- Return, Exit, Throw

**Block terminators:**
- End If, End Function, End Sub, End Class, End Module, End Namespace
- End Select, End While, End Structure, End Interface, End Enum
- Loop, Next, Else, ElseIf, Case, Catch, Finally

### 7. Helpful Suggestions

The parser provides context-aware suggestions based on the expected token:

| Expected Token | Suggestion |
|----------------|-----------|
| Then | "Did you forget 'Then' after the If condition?" |
| End If | "Make sure your If statement is properly closed with 'End If'." |
| End Function | "Make sure your Function is properly closed with 'End Function'." |
| End Sub | "Make sure your Sub is properly closed with 'End Sub'." |
| End Class | "Make sure your Class is properly closed with 'End Class'." |
| ) | "Did you forget a closing parenthesis ')'?" |
| ( | "Did you forget an opening parenthesis '('?" |
| = | "Did you forget the assignment operator '='?" |
| As | "Did you forget 'As' for type declaration?" |
| Identifier | "Expected an identifier (variable or function name)." |

### 8. Error Recovery in Parse Methods

Multiple parse methods now include error recovery:

**Top-level Parse():**
```csharp
try
{
    var declaration = ParseTopLevelDeclaration();
    program.Declarations.Add(declaration);
}
catch (ParseException ex)
{
    RecordError(ex.Message, ex.Token, ex.Suggestion);
    Synchronize();
}
```

**ParseBlock():**
```csharp
try
{
    var statement = ParseStatement();
    block.Statements.Add(statement);
}
catch (ParseException ex)
{
    RecordError(ex.Message, ex.Token, ex.Suggestion);
    Synchronize();

    // If we've synchronized to an end token, break out
    if (endTokens.Any(t => Check(t)))
        break;
}
```

**ParseClass(), ParseNamespace(), ParseFunction(), ParseSubroutine():**
All now include context tracking and error recovery for member parsing.

### 9. Maximum Error Limit

The `TooManyErrorsException` prevents infinite loops:

```csharp
public class TooManyErrorsException : Exception
{
    public TooManyErrorsException(string message) : base(message)
}
```

When more than 100 errors are encountered:
```
Too many parse errors (>100). Aborting.
```

## Example Usage

### Before (Old Behavior):
```
Input code with 5 syntax errors → Parser throws exception on first error → Stops
```

### After (New Behavior):
```
Input code with 5 syntax errors → Parser collects all 5 errors → Returns AST with partial results

Example output:
Error at line 5, column 10 in Function 'Calculate': Expected 'Then' after If condition but found Identifier
  Suggestion: Did you forget 'Then' after the If condition?

Error at line 12, column 3 in Function 'Calculate': Expected 'End If' but found EndFunction
  Suggestion: Make sure your If statement is properly closed with 'End If'.

Error at line 20, column 15 in Function 'Process' -> Class 'DataHandler': Expected ')' after parameters but found Comma
  Suggestion: Did you forget a closing parenthesis ')'?
```

## Benefits

1. **Better Developer Experience** - See all errors at once instead of fixing one at a time
2. **More Informative Messages** - Context and suggestions help developers fix issues faster
3. **Robust Parsing** - Parser continues even with syntax errors, useful for IDE integration
4. **Prevents Infinite Loops** - Maximum error limit ensures parser doesn't hang
5. **IDE-Friendly** - Collected errors can be displayed as diagnostics in code editors

## Testing Recommendations

Test the error recovery with code containing:

1. **Missing Then in If statements**
```vb
If x > 5
    Print(x)
End If
```

2. **Missing End statements**
```vb
Function Calculate(x As Integer) As Integer
    Return x * 2
' Missing End Function
```

3. **Nested errors**
```vb
Class Calculator
    Function Add(x As Integer, y As Integer
        Return x + y
    ' Missing ), missing End Function
End Class
```

4. **Multiple independent errors**
```vb
Function Test1()
    Return 5
' Missing End Function

Function Test2(
    Return 10
End Function
```

The parser should now collect all errors and provide helpful context for each one.
