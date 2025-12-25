using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;

namespace BasicLang.Tests
{
    /// <summary>
    /// Tests for Collections parsing and lexing
    /// </summary>
    public class CollectionsTests
    {
        // ====================================================================
        // Helper Methods
        // ====================================================================

        private List<Token> Tokenize(string source)
        {
            var lexer = new Lexer(source);
            return lexer.Tokenize();
        }

        private ProgramNode Parse(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            return parser.Parse();
        }

        // ====================================================================
        // List Operation Lexer Tests
        // ====================================================================

        [Theory]
        [InlineData("CreateList")]
        [InlineData("ListAdd")]
        [InlineData("ListGet")]
        [InlineData("ListSet")]
        [InlineData("ListCount")]
        [InlineData("ListRemove")]
        [InlineData("ListContains")]
        public void Tokenize_ListFunction_ReturnsIdentifier(string funcName)
        {
            var tokens = Tokenize(funcName);

            Assert.Equal(TokenType.Identifier, tokens[0].Type);
            Assert.Equal(funcName, tokens[0].Lexeme);
        }

        [Fact]
        public void Tokenize_CreateListCall_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("Dim myList = CreateList()");

            Assert.Contains(tokens, t => t.Type == TokenType.Dim);
            Assert.Contains(tokens, t => t.Lexeme == "myList");
            Assert.Contains(tokens, t => t.Lexeme == "CreateList");
            Assert.Contains(tokens, t => t.Type == TokenType.LeftParen);
            Assert.Contains(tokens, t => t.Type == TokenType.RightParen);
        }

        [Fact]
        public void Tokenize_ListAddCall_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("ListAdd(myList, \"value\")");

            Assert.Contains(tokens, t => t.Lexeme == "ListAdd");
            Assert.Contains(tokens, t => t.Type == TokenType.LeftParen);
            Assert.Contains(tokens, t => t.Type == TokenType.StringLiteral);
            Assert.Contains(tokens, t => t.Type == TokenType.Comma);
        }

        // ====================================================================
        // Dictionary Operation Lexer Tests
        // ====================================================================

        [Theory]
        [InlineData("CreateDictionary")]
        [InlineData("DictSet")]
        [InlineData("DictGet")]
        [InlineData("DictRemove")]
        [InlineData("DictContainsKey")]
        [InlineData("DictKeys")]
        [InlineData("DictValues")]
        public void Tokenize_DictionaryFunction_ReturnsIdentifier(string funcName)
        {
            var tokens = Tokenize(funcName);

            Assert.Equal(TokenType.Identifier, tokens[0].Type);
            Assert.Equal(funcName, tokens[0].Lexeme);
        }

        [Fact]
        public void Tokenize_CreateDictionaryCall_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("Dim dict = CreateDictionary()");

            Assert.Contains(tokens, t => t.Type == TokenType.Dim);
            Assert.Contains(tokens, t => t.Lexeme == "dict");
            Assert.Contains(tokens, t => t.Lexeme == "CreateDictionary");
        }

        [Fact]
        public void Tokenize_DictSetCall_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("DictSet(dict, \"key\", \"value\")");

            Assert.Contains(tokens, t => t.Lexeme == "DictSet");
            Assert.Equal(2, tokens.Count(t => t.Type == TokenType.Comma));
        }

        // ====================================================================
        // HashSet Operation Lexer Tests
        // ====================================================================

        [Theory]
        [InlineData("CreateHashSet")]
        [InlineData("SetAdd")]
        [InlineData("SetRemove")]
        [InlineData("SetContains")]
        [InlineData("SetCount")]
        [InlineData("SetClear")]
        public void Tokenize_HashSetFunction_ReturnsIdentifier(string funcName)
        {
            var tokens = Tokenize(funcName);

            Assert.Equal(TokenType.Identifier, tokens[0].Type);
            Assert.Equal(funcName, tokens[0].Lexeme);
        }

        // ====================================================================
        // LINQ-style Operations Lexer Tests
        // ====================================================================

        [Theory]
        [InlineData("OrderBy")]
        [InlineData("FirstOrDefault")]
        [InlineData("LastOrDefault")]
        [InlineData("SingleOrDefault")]
        [InlineData("ToList")]
        [InlineData("ToArray")]
        public void Tokenize_LinqFunction_ReturnsIdentifier(string funcName)
        {
            var tokens = Tokenize(funcName);

            Assert.Equal(TokenType.Identifier, tokens[0].Type);
            Assert.Equal(funcName, tokens[0].Lexeme);
        }

        [Fact]
        public void Tokenize_WhereWithLambda_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("Where(items, Function(x) x > 0)");

            // Where is a keyword token
            Assert.Contains(tokens, t => t.Type == TokenType.Where);
            Assert.Contains(tokens, t => t.Type == TokenType.Function);
            Assert.Contains(tokens, t => t.Type == TokenType.GreaterThan);
        }

        [Fact]
        public void Tokenize_SelectWithLambda_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("Select(items, Function(x) x * 2)");

            // Select is a keyword token
            Assert.Contains(tokens, t => t.Type == TokenType.Select);
            Assert.Contains(tokens, t => t.Type == TokenType.Function);
            Assert.Contains(tokens, t => t.Type == TokenType.Multiply);
        }

        // ====================================================================
        // String Interpolation Tests
        // ====================================================================

        [Fact]
        public void Tokenize_InterpolatedString_ReturnsCorrectType()
        {
            var tokens = Tokenize("$\"Hello {name}!\"");

            Assert.Equal(TokenType.InterpolatedStringLiteral, tokens[0].Type);
        }

        [Fact]
        public void Tokenize_InterpolatedString_WithExpression_ReturnsCorrectType()
        {
            var tokens = Tokenize("$\"Result: {2 + 2}\"");

            Assert.Equal(TokenType.InterpolatedStringLiteral, tokens[0].Type);
        }

        // ====================================================================
        // Nullable Type Tests
        // ====================================================================

        [Fact]
        public void Tokenize_NullableType_ReturnsQuestionMark()
        {
            var tokens = Tokenize("Dim x As Integer?");

            Assert.Contains(tokens, t => t.Type == TokenType.QuestionMark);
        }

        // ====================================================================
        // Variable Declaration Tests (simpler parsing)
        // ====================================================================

        [Fact]
        public void Parse_SimpleVariableDeclaration_Succeeds()
        {
            var source = "Dim x As Integer = 42";
            var ast = Parse(source);

            Assert.NotNull(ast);
        }

        [Fact]
        public void Parse_FunctionDeclaration_Succeeds()
        {
            var source = @"Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function";
            var ast = Parse(source);

            Assert.NotNull(ast);
            Assert.NotEmpty(ast.Declarations);
        }

        [Fact]
        public void Parse_ClassDeclaration_Succeeds()
        {
            var source = @"Class MyClass
    Private x As Integer
End Class";
            var ast = Parse(source);

            Assert.NotNull(ast);
            Assert.NotEmpty(ast.Declarations);
        }
    }
}
