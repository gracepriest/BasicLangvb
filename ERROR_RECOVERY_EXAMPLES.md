# Parser Error Recovery Examples

This document shows examples of how the improved error recovery works in the BasicLang parser.

## Example 1: Missing 'Then' in If Statement

### Input Code:
```vb
Module TestModule
    Function Calculate(x As Integer) As Integer
        If x > 5
            Return x * 2
        End If
        Return 0
    End Function
End Module
```

### Old Behavior (Before Improvements):
```
Parse error at line 3, column 9: Expected 'Then' after if condition but found Newline
[Parsing stops here - user doesn't see other potential errors]
```

### New Behavior (After Improvements):
```
Error at line 3, column 9 in Function 'Calculate' -> Module 'TestModule': Expected 'Then' after If condition but found Newline
  Suggestion: Did you forget 'Then' after the If condition?

[Parser continues and returns partial AST]
```

## Example 2: Missing End Function

### Input Code:
```vb
Module MathOps
    Function Add(x As Integer, y As Integer) As Integer
        Return x + y
    ' Oops! Forgot End Function

    Function Subtract(x As Integer, y As Integer) As Integer
        Return x - y
    End Function
End Module
```

### Old Behavior:
```
Parse error at line 6, column 5: Expected 'End Function' but found Function
[Parsing stops]
```

### New Behavior:
```
Error at line 6, column 5 in Function 'Add' -> Module 'MathOps': Expected 'End Function' but found Function
  Suggestion: Make sure your Function is properly closed with 'End Function'.

[Parser synchronizes to 'Function' keyword, continues parsing Subtract function]
[Parser returns AST with both functions, first one incomplete]
```

## Example 3: Multiple Errors in Nested Structures

### Input Code:
```vb
Namespace MyApp
    Class Calculator
        Function Add(x As Integer, y As Integer As Integer
            If x > 0
                Return x + y
            End If
            Return 0
        ' Missing End Function

        Sub PrintResult(value As Integer)
            Print(value
        End Sub
    End Class
End Namespace
```

### Old Behavior:
```
Parse error at line 3, column 50: Expected ')' after parameters but found As
[Stops immediately]
```

### New Behavior:
```
Error at line 3, column 50 in Function 'Add' -> Class 'Calculator' -> Namespace 'MyApp': Expected ')' after parameters but found As
  Suggestion: Did you forget a closing parenthesis ')'?

Error at line 9, column 9 in Function 'Add' -> Class 'Calculator' -> Namespace 'MyApp': Expected 'End Function' but found Sub
  Suggestion: Make sure your Function is properly closed with 'End Function'.

Error at line 12, column 13 in Sub 'PrintResult' -> Class 'Calculator' -> Namespace 'MyApp': Expected ')' after event arguments but found EndSub
  Suggestion: Did you forget a closing parenthesis ')'?

[Parser returns AST with partial structure for analysis]
```

## Example 4: Maximum Error Limit

### Input Code (100+ syntax errors):
```vb
Function Test1(
Function Test2(
Function Test3(
... [97 more similar errors]
Function Test100(
Function Test101(
```

### Behavior:
```
Error at line 1, column 15 in Function 'Test1': Expected ')' after parameters but found Function
  Suggestion: Did you forget a closing parenthesis ')'?

Error at line 2, column 15 in Function 'Test2': Expected ')' after parameters but found Function
  Suggestion: Did you forget a closing parenthesis ')'?

... [continues collecting errors] ...

Error at line 100, column 15 in Function 'Test100': Expected ')' after parameters but found Function
  Suggestion: Did you forget a closing parenthesis ')'?

Too many parse errors (>100). Aborting.
```

## Example 5: Error Recovery in Blocks

### Input Code:
```vb
Sub Main()
    Dim x As Integer = 5

    ' Error: missing Then
    If x > 0
        Print("positive")
    End If

    ' This statement should still be parsed despite the error above
    Dim y As Integer = 10

    ' Another error: missing closing paren
    Print(y

    ' This should also be parsed
    Dim z As Integer = 15
End Sub
```

### New Behavior:
```
Error at line 5, column 5 in Sub 'Main': Expected 'Then' after If condition but found Newline
  Suggestion: Did you forget 'Then' after the If condition?

Error at line 13, column 5 in Sub 'Main': Expected ')' after event arguments but found Dim
  Suggestion: Did you forget a closing parenthesis ')'?

[Parser recovers and parses all three Dim statements successfully]
[AST includes all variable declarations: x, y, and z]
```

## Example 6: Context Information in Nested Structures

### Input Code:
```vb
Namespace Company.Product
    Module DataAccess
        Class UserRepository
            Function GetUser(id As Integer) As User
                ' Error here
                If id > 0
                    Return New User()
                End If
                Return Nothing
            End Function
        End Class
    End Module
End Namespace
```

### Error with Full Context:
```
Error at line 6, column 17 in Function 'GetUser' -> Class 'UserRepository' -> Module 'DataAccess' -> Namespace 'Company.Product': Expected 'Then' after If condition but found Newline
  Suggestion: Did you forget 'Then' after the If condition?
```

The context chain shows exactly where in the nested structure the error occurred:
- Namespace: Company.Product
- Module: DataAccess
- Class: UserRepository
- Function: GetUser

## How to Use the Error Collection API

### Example Usage in Compiler/IDE:
```csharp
using BasicLang.Compiler;

// Tokenize
var lexer = new BasicLangLexer(sourceCode);
var tokens = lexer.Tokenize();

// Parse with error recovery
var parser = new Parser(tokens);
var ast = parser.Parse();

// Check for errors
if (parser.Errors.Count > 0)
{
    Console.WriteLine($"Found {parser.Errors.Count} parsing error(s):\n");

    foreach (var error in parser.Errors)
    {
        // Display in IDE error list or console
        Console.WriteLine(error.ToString());

        // Access individual properties
        Console.WriteLine($"  Line: {error.Token.Line}");
        Console.WriteLine($"  Column: {error.Token.Column}");
        Console.WriteLine($"  Context: {error.Context}");
        Console.WriteLine($"  Message: {error.Message}");
        if (!string.IsNullOrEmpty(error.Suggestion))
        {
            Console.WriteLine($"  Suggestion: {error.Suggestion}");
        }
        Console.WriteLine();
    }

    // Still can analyze partial AST even with errors
    if (ast != null)
    {
        // Perform semantic analysis on successful portions
        // Generate diagnostics
        // Provide autocomplete suggestions
    }
}
else
{
    Console.WriteLine("Parsing completed successfully!");
    // Continue with code generation
}
```

## Benefits Demonstrated

1. **Multiple Error Detection**: All errors are found in one pass instead of fix-one-run-again cycle
2. **Better Context**: Each error shows exactly where in the code structure it occurred
3. **Helpful Suggestions**: Each error includes actionable advice for fixing it
4. **Robust Recovery**: Parser continues even with errors, useful for IDEs and development tools
5. **Prevents Infinite Loops**: Maximum error limit ensures parser doesn't hang on severely broken code

## Synchronization Strategy

The parser uses intelligent synchronization to recovery points:

1. **After Newlines**: Assumes next line might start a valid statement
2. **At Statement Keywords**: If, For, While, Return, etc.
3. **At Declaration Keywords**: Function, Sub, Class, Dim, etc.
4. **At Block Terminators**: End If, End Function, End Class, etc.
5. **At Block Separators**: Else, ElseIf, Case, Catch, Finally

This strategy minimizes cascading errors and maximizes the amount of code that can be successfully parsed.
