# Parser Error Recovery - Quick Reference

## Quick Start

### Basic Usage

```csharp
using BasicLang.Compiler;

// Parse code
var lexer = new BasicLangLexer(sourceCode);
var tokens = lexer.Tokenize();
var parser = new Parser(tokens);
var ast = parser.Parse();

// Check for errors
if (parser.Errors.Count > 0)
{
    foreach (var error in parser.Errors)
    {
        Console.WriteLine(error.ToString());
    }
}
```

## Error Properties

### ParseError Class

| Property | Type | Description |
|----------|------|-------------|
| Message | string | The error message |
| Token | Token | The token where error occurred |
| Context | string | Where in code structure (e.g., "in Function 'Main'") |
| Suggestion | string | Helpful suggestion to fix the error |

### ParseException Class

| Property | Type | Description |
|----------|------|-------------|
| Message | string | The error message |
| Token | Token | The token where error occurred |
| Suggestion | string | Helpful suggestion to fix the error |

## Common Error Suggestions

| Missing Token | Suggestion |
|---------------|------------|
| Then | "Did you forget 'Then' after the If condition?" |
| End If | "Make sure your If statement is properly closed with 'End If'." |
| End Function | "Make sure your Function is properly closed with 'End Function'." |
| End Sub | "Make sure your Sub is properly closed with 'End Sub'." |
| End Class | "Make sure your Class is properly closed with 'End Class'." |
| ) | "Did you forget a closing parenthesis ')'?" |
| ( | "Did you forget an opening parenthesis '('?" |
| = | "Did you forget the assignment operator '='?" |

## Synchronization Points

The parser recovers at these token types:

### Top-Level
- Namespace, Module, Class, Interface, Enum, Type, Structure
- Function, Sub, Dim, Const, Auto

### Statements
- If, For, While, Do, Select, Try, With
- Return, Exit, Throw

### Block Terminators
- End If, End Function, End Sub, End Class
- End Module, End Namespace, End Select
- Loop, Next, Else, ElseIf, Case, Catch, Finally

## Error Limits

- **Maximum Errors**: 100 errors before aborting
- **Exception**: `TooManyErrorsException` thrown when limit exceeded

## Example Error Output

```
Error at line 10, column 5 in Function 'Calculate' -> Class 'Math': Expected 'Then' after If condition but found Newline
  Suggestion: Did you forget 'Then' after the If condition?
```

## Processing Errors

### Get Error Count

```csharp
int errorCount = parser.Errors.Count;
```

### Filter Errors by Line

```csharp
var errorsOnLine10 = parser.Errors.Where(e => e.Token.Line == 10);
```

### Group Errors by Context

```csharp
var errorsByContext = parser.Errors
    .GroupBy(e => e.Context)
    .OrderByDescending(g => g.Count());
```

### Find Errors with Suggestions

```csharp
var errorsWithFixes = parser.Errors
    .Where(e => !string.IsNullOrEmpty(e.Suggestion));
```

## Integration Patterns

### Compiler Integration

```csharp
var parser = new Parser(tokens);
var ast = parser.Parse();

if (parser.Errors.Count > 0)
{
    // Show errors and exit
    foreach (var error in parser.Errors)
        Console.Error.WriteLine(error);
    return 1; // Error exit code
}

// Continue compilation
```

### IDE Integration

```csharp
var parser = new Parser(tokens);
var ast = parser.Parse();

// Add errors to diagnostics
foreach (var error in parser.Errors)
{
    AddDiagnostic(
        severity: DiagnosticSeverity.Error,
        line: error.Token.Line,
        column: error.Token.Column,
        message: error.Message,
        quickFix: error.Suggestion
    );
}

// Still provide IntelliSense on partial AST
```

### REPL Integration

```csharp
var parser = new Parser(tokens);
var ast = parser.Parse();

if (parser.Errors.Count > 0)
{
    // Show errors but continue REPL
    Console.WriteLine($"⚠ {parser.Errors.Count} syntax error(s)");
    foreach (var error in parser.Errors)
        Console.WriteLine($"  {error.Message}");
}
else
{
    // Execute code
    Execute(ast);
}
```

## Best Practices

1. **Always Check Errors**: Even if AST is returned, check `parser.Errors`
2. **Show All Errors**: Display all errors to user, not just the first one
3. **Use Suggestions**: Present suggestions to help users fix issues
4. **Partial AST**: Even with errors, partial AST may be useful for analysis
5. **Context Matters**: Show context to help locate errors in large files

## Backward Compatibility

The enhanced parser is backward compatible:

```csharp
// Old code still works
try
{
    var parser = new Parser(tokens);
    var ast = parser.Parse();
    // No errors
}
catch (ParseException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

// New code can collect multiple errors
var parser = new Parser(tokens);
var ast = parser.Parse();
if (parser.Errors.Count > 0)
{
    // Handle all errors
}
```

## Common Patterns

### Pattern 1: Fail Fast

```csharp
var parser = new Parser(tokens);
var ast = parser.Parse();

if (parser.Errors.Count > 0)
{
    throw new Exception($"Parsing failed with {parser.Errors.Count} errors");
}
```

### Pattern 2: Collect and Report

```csharp
var parser = new Parser(tokens);
var ast = parser.Parse();

foreach (var error in parser.Errors)
{
    logger.Error(error.ToString());
}

return parser.Errors.Count == 0;
```

### Pattern 3: IDE Diagnostics

```csharp
var parser = new Parser(tokens);
var ast = parser.Parse();

var diagnostics = parser.Errors.Select(e => new Diagnostic
{
    Severity = DiagnosticSeverity.Error,
    Range = new Range(e.Token.Line, e.Token.Column),
    Message = e.Message,
    Code = "PARSE_ERROR",
    Source = "BasicLang",
    CodeActions = e.Suggestion != null
        ? new[] { new CodeAction { Title = e.Suggestion } }
        : null
});

return diagnostics;
```

## Testing

### Test Error Recovery

```csharp
[Test]
public void TestErrorRecovery()
{
    var code = @"
        Function Test1(
            Return 5
        End Function
    ";

    var lexer = new BasicLangLexer(code);
    var parser = new Parser(lexer.Tokenize());
    var ast = parser.Parse();

    Assert.AreEqual(1, parser.Errors.Count);
    Assert.IsTrue(parser.Errors[0].Message.Contains("Expected ')'"));
    Assert.IsNotNull(parser.Errors[0].Suggestion);
}
```

### Test Multiple Errors

```csharp
[Test]
public void TestMultipleErrors()
{
    var code = @"
        If x > 0
            Print(x
        End If
    ";

    var lexer = new BasicLangLexer(code);
    var parser = new Parser(lexer.Tokenize());
    var ast = parser.Parse();

    Assert.AreEqual(2, parser.Errors.Count);
    // Error 1: Missing 'Then'
    // Error 2: Missing ')'
}
```

## Performance

The error recovery adds minimal overhead:
- **Normal case** (no errors): <1% overhead
- **Error case**: Synchronization is O(n) worst case
- **Memory**: Stores error list (negligible for <100 errors)

## Limitations

1. **Max 100 errors**: To prevent infinite loops
2. **Partial AST**: AST may be incomplete after errors
3. **Cascading errors**: Some errors may be side-effects of earlier errors
4. **Context depth**: Very deep nesting may truncate context display
