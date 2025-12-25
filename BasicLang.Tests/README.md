# BasicLang.Tests

This is the unit test project for BasicLang compiler using xUnit testing framework.

## Test Structure

The test project is organized into the following test files:

### LexerTests.cs
Tests for the lexical analyzer (tokenizer) covering:
- **Literal tokens**: integers, longs, floats, doubles, strings, booleans
- **Keywords**: all BasicLang keywords (Dim, If, For, Function, etc.)
- **Operators**: arithmetic, comparison, logical, bitwise
- **Punctuation**: parentheses, brackets, commas, etc.
- **Comments**: single-line comments
- **Complex expressions**: function calls, member access, array indexing
- **Edge cases**: empty input, unterminated strings, unknown characters

### ParserTests.cs
Tests for the parser (AST generation) covering:
- **Variable declarations**: simple, with initializers, auto, arrays
- **Functions and subroutines**: parameters, return types, optional parameters
- **Expressions**: binary, unary, literals, identifiers, calls
- **Statements**: if/else, loops (for, while, do), select/case, assignments
- **Classes**: simple classes, inheritance, interfaces
- **Types**: type definitions, enums
- **Error handling**: try/catch blocks
- **Complex programs**: Fibonacci, nested structures

### SemanticAnalyzerTests.cs
Tests for semantic analysis (type checking and scope resolution) covering:
- **Type checking**: variable types, function return types, expression types
- **Scope resolution**: variable visibility, shadowing, nested scopes
- **Function calls**: argument count, argument types, optional parameters
- **Classes**: member access, inheritance validation
- **Arrays**: bounds checking, element type validation
- **Standard library**: built-in function validation
- **Error detection**: undefined variables, type mismatches, duplicate declarations
- **Edge cases**: constants, type inference (auto)

### IntegrationTests.cs
Tests for the complete compilation pipeline covering:
- **End-to-end**: source → lexer → parser → semantic analyzer → IR → code generation
- **Control flow**: if/else, loops, switch statements
- **Functions**: simple functions, recursion (Fibonacci, factorial)
- **Arrays**: declaration, access, iteration
- **Strings**: concatenation, built-in functions
- **Classes**: declaration, instantiation, member access
- **Error handling**: try/catch blocks
- **Real-world examples**: BubbleSort, prime checking, calculator class
- **Backend validation**: C# code generation quality

## Running Tests

### Visual Studio
1. Open the solution in Visual Studio
2. Go to Test → Test Explorer
3. Click "Run All" to run all tests

### Command Line
```bash
cd BasicLang.Tests
dotnet test
```

### With Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

## Test Patterns

### Happy Path Tests
These test normal, expected usage:
```csharp
[Fact]
public void Tokenize_IntegerLiteral_ReturnsCorrectToken()
{
    var token = GetToken("42");
    Assert.Equal(TokenType.IntegerLiteral, token.Type);
    Assert.Equal(42, token.Value);
}
```

### Error Cases
These test error detection and reporting:
```csharp
[Fact]
public void Analyze_UndefinedVariable_HasError()
{
    var source = "Sub Test()\n    x = 42\nEnd Sub";
    AssertError(source, "Undefined identifier");
}
```

### Edge Cases
These test boundary conditions:
```csharp
[Fact]
public void Parse_EmptyProgram_ReturnsEmptyAST()
{
    var program = Parse("");
    Assert.NotNull(program);
    Assert.Empty(program.Declarations);
}
```

### Theory Tests
These test multiple similar cases with different inputs:
```csharp
[Theory]
[InlineData("Dim", TokenType.Dim)]
[InlineData("If", TokenType.If)]
[InlineData("For", TokenType.For)]
public void Tokenize_Keyword_ReturnsCorrectTokenType(string keyword, TokenType expectedType)
{
    var token = GetToken(keyword);
    Assert.Equal(expectedType, token.Type);
}
```

## Test Coverage Goals

- **Lexer**: 90%+ line coverage
- **Parser**: 85%+ line coverage
- **Semantic Analyzer**: 80%+ line coverage
- **Integration**: All major language features tested

## Adding New Tests

When adding new language features:

1. Add lexer tests for any new tokens
2. Add parser tests for the AST structure
3. Add semantic tests for type checking/validation
4. Add integration tests for end-to-end functionality

Example workflow:
```csharp
// 1. Test tokenization
[Fact]
public void Tokenize_NewKeyword_ReturnsCorrectToken()
{
    var token = GetToken("NewKeyword");
    Assert.Equal(TokenType.NewKeyword, token.Type);
}

// 2. Test parsing
[Fact]
public void Parse_NewFeature_ReturnsCorrectAST()
{
    var node = ParseSingle<NewFeatureNode>("NewKeyword ...");
    Assert.NotNull(node);
}

// 3. Test semantics
[Fact]
public void Analyze_NewFeature_Succeeds()
{
    var source = "NewKeyword ...";
    AssertNoErrors(source);
}

// 4. Test integration
[Fact]
public void Integration_NewFeature_GeneratesCorrectCode()
{
    var source = "NewKeyword ...";
    var csharpCode = CompileToCSharp(source);
    Assert.Contains("expected output", csharpCode);
}
```

## Common Assertions

- `Assert.Equal(expected, actual)` - Values are equal
- `Assert.True(condition)` - Condition is true
- `Assert.False(condition)` - Condition is false
- `Assert.NotNull(value)` - Value is not null
- `Assert.Empty(collection)` - Collection is empty
- `Assert.Single(collection)` - Collection has exactly one element
- `Assert.Contains(expectedSubstring, actualString)` - String contains substring
- `Assert.IsType<T>(object)` - Object is of specific type
- `Assert.Throws<TException>(() => code)` - Code throws specific exception

## Debugging Tests

To debug a specific test:
1. Set a breakpoint in the test method
2. Right-click the test in Test Explorer
3. Select "Debug Selected Tests"

Or use the VS Code test explorer for better integration.
