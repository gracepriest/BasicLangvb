using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BasicLang.Compiler;

namespace BasicLang.Tests
{
    public class LexerTests
    {
        // ====================================================================
        // Helper Methods
        // ====================================================================

        private List<Token> Tokenize(string source)
        {
            var lexer = new Lexer(source);
            return lexer.Tokenize();
        }

        private Token GetToken(string source, int index = 0)
        {
            var tokens = Tokenize(source);
            return tokens[index];
        }

        // ====================================================================
        // Literal Tests
        // ====================================================================

        [Fact]
        public void Tokenize_IntegerLiteral_ReturnsCorrectToken()
        {
            var token = GetToken("42");

            Assert.Equal(TokenType.IntegerLiteral, token.Type);
            Assert.Equal("42", token.Lexeme);
            Assert.Equal(42, token.Value);
        }

        [Fact]
        public void Tokenize_LongLiteral_ReturnsCorrectToken()
        {
            var token = GetToken("9223372036854775807L");

            Assert.Equal(TokenType.LongLiteral, token.Type);
            Assert.Equal(9223372036854775807L, token.Value);
        }

        [Fact]
        public void Tokenize_SingleLiteral_ReturnsCorrectToken()
        {
            var token = GetToken("3.14f");

            Assert.Equal(TokenType.SingleLiteral, token.Type);
            Assert.Equal(3.14f, (float)token.Value, 2);
        }

        [Fact]
        public void Tokenize_DoubleLiteral_ReturnsCorrectToken()
        {
            var token = GetToken("3.14159");

            Assert.Equal(TokenType.DoubleLiteral, token.Type);
            Assert.Equal(3.14159, (double)token.Value, 5);
        }

        [Fact]
        public void Tokenize_StringLiteral_ReturnsCorrectToken()
        {
            var token = GetToken("\"Hello, World!\"");

            Assert.Equal(TokenType.StringLiteral, token.Type);
            Assert.Equal("Hello, World!", token.Value);
        }

        [Fact]
        public void Tokenize_StringLiteral_WithEscapeSequences_ReturnsCorrectToken()
        {
            var token = GetToken("\"Line1\\nLine2\\tTabbed\"");

            Assert.Equal(TokenType.StringLiteral, token.Type);
            Assert.Equal("Line1\nLine2\tTabbed", token.Value);
        }

        [Fact]
        public void Tokenize_InterpolatedString_ReturnsCorrectToken()
        {
            var token = GetToken("$\"Hello {name}!\"");

            Assert.Equal(TokenType.InterpolatedStringLiteral, token.Type);
            Assert.Equal("Hello {name}!", token.Value);
        }

        [Fact]
        public void Tokenize_BooleanLiteral_True_ReturnsCorrectToken()
        {
            var token = GetToken("True");

            Assert.Equal(TokenType.BooleanLiteral, token.Type);
            Assert.Equal(true, token.Value);
        }

        [Fact]
        public void Tokenize_BooleanLiteral_False_ReturnsCorrectToken()
        {
            var token = GetToken("False");

            Assert.Equal(TokenType.BooleanLiteral, token.Type);
            Assert.Equal(false, token.Value);
        }

        // ====================================================================
        // Keyword Tests
        // ====================================================================

        [Theory]
        [InlineData("Dim", TokenType.Dim)]
        [InlineData("As", TokenType.As)]
        [InlineData("Integer", TokenType.Integer)]
        [InlineData("String", TokenType.String)]
        [InlineData("Boolean", TokenType.Boolean)]
        [InlineData("If", TokenType.If)]
        [InlineData("Then", TokenType.Then)]
        [InlineData("Else", TokenType.Else)]
        [InlineData("For", TokenType.For)]
        [InlineData("While", TokenType.While)]
        [InlineData("Function", TokenType.Function)]
        [InlineData("Sub", TokenType.Sub)]
        [InlineData("Class", TokenType.Class)]
        [InlineData("Public", TokenType.Public)]
        [InlineData("Private", TokenType.Private)]
        public void Tokenize_Keyword_ReturnsCorrectTokenType(string keyword, TokenType expectedType)
        {
            var token = GetToken(keyword);
            Assert.Equal(expectedType, token.Type);
        }

        [Fact]
        public void Tokenize_Keywords_AreCaseInsensitive()
        {
            var tokens1 = Tokenize("DIM");
            var tokens2 = Tokenize("dim");
            var tokens3 = Tokenize("Dim");

            Assert.Equal(TokenType.Dim, tokens1[0].Type);
            Assert.Equal(TokenType.Dim, tokens2[0].Type);
            Assert.Equal(TokenType.Dim, tokens3[0].Type);
        }

        [Theory]
        [InlineData("End If", TokenType.EndIf)]
        [InlineData("End Sub", TokenType.EndSub)]
        [InlineData("End Function", TokenType.EndFunction)]
        [InlineData("End Class", TokenType.EndClass)]
        [InlineData("End Type", TokenType.EndType)]
        public void Tokenize_MultiWordKeyword_ReturnsCorrectToken(string keyword, TokenType expectedType)
        {
            var token = GetToken(keyword);
            Assert.Equal(expectedType, token.Type);
        }

        // ====================================================================
        // Identifier Tests
        // ====================================================================

        [Fact]
        public void Tokenize_Identifier_ReturnsCorrectToken()
        {
            var token = GetToken("myVariable");

            Assert.Equal(TokenType.Identifier, token.Type);
            Assert.Equal("myVariable", token.Lexeme);
            Assert.Equal("myVariable", token.Value);
        }

        [Fact]
        public void Tokenize_IdentifierWithUnderscore_ReturnsCorrectToken()
        {
            var token = GetToken("_privateVar");

            Assert.Equal(TokenType.Identifier, token.Type);
            Assert.Equal("_privateVar", token.Lexeme);
        }

        [Fact]
        public void Tokenize_IdentifierWithNumbers_ReturnsCorrectToken()
        {
            var token = GetToken("var123");

            Assert.Equal(TokenType.Identifier, token.Type);
            Assert.Equal("var123", token.Lexeme);
        }

        // ====================================================================
        // Operator Tests
        // ====================================================================

        [Theory]
        [InlineData("+", TokenType.Plus)]
        [InlineData("-", TokenType.Minus)]
        [InlineData("*", TokenType.Multiply)]
        [InlineData("/", TokenType.Divide)]
        [InlineData("\\", TokenType.IntegerDivide)]
        [InlineData("%", TokenType.Modulo)]
        [InlineData("=", TokenType.Assignment)]
        [InlineData("<", TokenType.LessThan)]
        [InlineData(">", TokenType.GreaterThan)]
        [InlineData("<=", TokenType.LessThanOrEqual)]
        [InlineData(">=", TokenType.GreaterThanOrEqual)]
        [InlineData("<>", TokenType.NotEqual)]
        [InlineData("!=", TokenType.NotEqual)]
        [InlineData("==", TokenType.IsEqual)]
        [InlineData("&", TokenType.Concatenate)]
        [InlineData("&&", TokenType.AndAnd)]
        [InlineData("||", TokenType.OrOr)]
        public void Tokenize_Operator_ReturnsCorrectToken(string op, TokenType expectedType)
        {
            var token = GetToken(op);
            Assert.Equal(expectedType, token.Type);
        }

        [Fact]
        public void Tokenize_IncrementOperator_ReturnsCorrectToken()
        {
            var token = GetToken("++");
            Assert.Equal(TokenType.Increment, token.Type);
        }

        [Fact]
        public void Tokenize_DecrementOperator_ReturnsCorrectToken()
        {
            var token = GetToken("--");
            Assert.Equal(TokenType.Decrement, token.Type);
        }

        [Theory]
        [InlineData("+=", TokenType.PlusAssign)]
        [InlineData("-=", TokenType.MinusAssign)]
        [InlineData("*=", TokenType.MultiplyAssign)]
        [InlineData("/=", TokenType.DivideAssign)]
        public void Tokenize_CompoundAssignment_ReturnsCorrectToken(string op, TokenType expectedType)
        {
            var token = GetToken(op);
            Assert.Equal(expectedType, token.Type);
        }

        [Theory]
        [InlineData("<<", TokenType.LeftShift)]
        [InlineData(">>", TokenType.RightShift)]
        public void Tokenize_BitwiseShift_ReturnsCorrectToken(string op, TokenType expectedType)
        {
            var token = GetToken(op);
            Assert.Equal(expectedType, token.Type);
        }

        // ====================================================================
        // Punctuation Tests
        // ====================================================================

        [Theory]
        [InlineData("(", TokenType.LeftParen)]
        [InlineData(")", TokenType.RightParen)]
        [InlineData("[", TokenType.LeftBracket)]
        [InlineData("]", TokenType.RightBracket)]
        [InlineData("{", TokenType.LeftBrace)]
        [InlineData("}", TokenType.RightBrace)]
        [InlineData(",", TokenType.Comma)]
        [InlineData(".", TokenType.Dot)]
        [InlineData(":", TokenType.Colon)]
        [InlineData(";", TokenType.Semicolon)]
        public void Tokenize_Punctuation_ReturnsCorrectToken(string punct, TokenType expectedType)
        {
            var token = GetToken(punct);
            Assert.Equal(expectedType, token.Type);
        }

        // ====================================================================
        // Comment Tests
        // ====================================================================

        [Fact]
        public void Tokenize_Comment_ReturnsCorrectToken()
        {
            var token = GetToken("' This is a comment");

            Assert.Equal(TokenType.Comment, token.Type);
            Assert.Contains("This is a comment", token.Lexeme);
        }

        [Fact]
        public void Tokenize_CommentDoesNotIncludeNewline()
        {
            var tokens = Tokenize("' Comment\n42");

            Assert.Equal(TokenType.Comment, tokens[0].Type);
            Assert.Equal(TokenType.Newline, tokens[1].Type);
            Assert.Equal(TokenType.IntegerLiteral, tokens[2].Type);
        }

        // ====================================================================
        // Directive Tests
        // ====================================================================

        [Theory]
        [InlineData("#if", TokenType.PreprocessorIf)]
        [InlineData("#else", TokenType.PreprocessorElse)]
        [InlineData("#elseif", TokenType.PreprocessorElseIf)]
        [InlineData("#endif", TokenType.PreprocessorEndIf)]
        [InlineData("#define", TokenType.PreprocessorDefine)]
        [InlineData("#include", TokenType.PreprocessorInclude)]
        [InlineData("#const", TokenType.PreprocessorConst)]
        [InlineData("#region", TokenType.PreprocessorRegion)]
        public void Tokenize_Directive_ReturnsCorrectToken(string directive, TokenType expectedType)
        {
            var token = GetToken(directive);
            Assert.Equal(expectedType, token.Type);
        }

        // ====================================================================
        // Complex Expression Tests
        // ====================================================================

        [Fact]
        public void Tokenize_SimpleExpression_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("x = 42");

            Assert.Equal(4, tokens.Count); // x, =, 42, EOF
            Assert.Equal(TokenType.Identifier, tokens[0].Type);
            Assert.Equal(TokenType.Assignment, tokens[1].Type);
            Assert.Equal(TokenType.IntegerLiteral, tokens[2].Type);
            Assert.Equal(TokenType.EOF, tokens[3].Type);
        }

        [Fact]
        public void Tokenize_ArithmeticExpression_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("result = a + b * c");

            Assert.Equal(8, tokens.Count); // result, =, a, +, b, *, c, EOF
            Assert.Equal("result", tokens[0].Lexeme);
            Assert.Equal(TokenType.Assignment, tokens[1].Type);
            Assert.Equal("a", tokens[2].Lexeme);
            Assert.Equal(TokenType.Plus, tokens[3].Type);
            Assert.Equal("b", tokens[4].Lexeme);
            Assert.Equal(TokenType.Multiply, tokens[5].Type);
            Assert.Equal("c", tokens[6].Lexeme);
        }

        [Fact]
        public void Tokenize_FunctionCall_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("Print(\"Hello\")");

            Assert.Equal(5, tokens.Count); // Print, (, "Hello", ), EOF
            Assert.Equal(TokenType.Identifier, tokens[0].Type);
            Assert.Equal("Print", tokens[0].Lexeme);
            Assert.Equal(TokenType.LeftParen, tokens[1].Type);
            Assert.Equal(TokenType.StringLiteral, tokens[2].Type);
            Assert.Equal(TokenType.RightParen, tokens[3].Type);
        }

        [Fact]
        public void Tokenize_ArrayAccess_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("arr[0]");

            Assert.Equal(5, tokens.Count); // arr, [, 0, ], EOF
            Assert.Equal(TokenType.Identifier, tokens[0].Type);
            Assert.Equal(TokenType.LeftBracket, tokens[1].Type);
            Assert.Equal(TokenType.IntegerLiteral, tokens[2].Type);
            Assert.Equal(TokenType.RightBracket, tokens[3].Type);
        }

        [Fact]
        public void Tokenize_MemberAccess_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("obj.Property");

            Assert.Equal(4, tokens.Count); // obj, ., Property, EOF
            Assert.Equal(TokenType.Identifier, tokens[0].Type);
            Assert.Equal(TokenType.Dot, tokens[1].Type);
            Assert.Equal(TokenType.Identifier, tokens[2].Type);
        }

        // ====================================================================
        // Whitespace and Newline Tests
        // ====================================================================

        [Fact]
        public void Tokenize_WhitespaceIsSkipped()
        {
            var tokens = Tokenize("x    =    42");

            Assert.Equal(4, tokens.Count); // x, =, 42, EOF
            Assert.DoesNotContain(tokens, t => t.Lexeme.Contains(" "));
        }

        [Fact]
        public void Tokenize_NewlineToken_ReturnsCorrectToken()
        {
            var tokens = Tokenize("x\n");

            Assert.Equal(3, tokens.Count); // x, newline, EOF
            Assert.Equal(TokenType.Newline, tokens[1].Type);
        }

        [Fact]
        public void Tokenize_MultipleLines_TracksLineNumbers()
        {
            var tokens = Tokenize("x = 1\ny = 2\nz = 3");

            var xToken = tokens.First(t => t.Lexeme == "x");
            var yToken = tokens.First(t => t.Lexeme == "y");
            var zToken = tokens.First(t => t.Lexeme == "z");

            Assert.Equal(1, xToken.Line);
            Assert.Equal(2, yToken.Line);
            Assert.Equal(3, zToken.Line);
        }

        // ====================================================================
        // Error Cases
        // ====================================================================

        [Fact]
        public void Tokenize_UnterminatedString_ThrowsException()
        {
            var lexer = new Lexer("\"unterminated");

            Assert.Throws<Exception>(() => lexer.Tokenize());
        }

        [Fact]
        public void Tokenize_UnknownCharacter_ReturnsUnknownToken()
        {
            var token = GetToken("@");

            Assert.Equal(TokenType.Unknown, token.Type);
        }

        // ====================================================================
        // Edge Cases
        // ====================================================================

        [Fact]
        public void Tokenize_EmptyString_ReturnsOnlyEOF()
        {
            var tokens = Tokenize("");

            Assert.Single(tokens);
            Assert.Equal(TokenType.EOF, tokens[0].Type);
        }

        [Fact]
        public void Tokenize_OnlyWhitespace_ReturnsOnlyEOF()
        {
            var tokens = Tokenize("   \t  \n  ");

            Assert.Equal(2, tokens.Count); // newline, EOF
        }

        [Fact]
        public void Tokenize_NumberFollowedByIdentifier_SeparatesCorrectly()
        {
            var tokens = Tokenize("42x");

            Assert.Equal(3, tokens.Count); // 42, x, EOF
            Assert.Equal(TokenType.IntegerLiteral, tokens[0].Type);
            Assert.Equal(TokenType.Identifier, tokens[1].Type);
        }

        [Fact]
        public void Tokenize_NegativeNumber_ReturnsMinusAndNumber()
        {
            var tokens = Tokenize("-42");

            Assert.Equal(3, tokens.Count); // -, 42, EOF
            Assert.Equal(TokenType.Minus, tokens[0].Type);
            Assert.Equal(TokenType.IntegerLiteral, tokens[1].Type);
        }

        // ====================================================================
        // Real Code Examples
        // ====================================================================

        [Fact]
        public void Tokenize_VariableDeclaration_ReturnsCorrectTokens()
        {
            var tokens = Tokenize("Dim x As Integer = 10");

            Assert.Contains(tokens, t => t.Type == TokenType.Dim);
            Assert.Contains(tokens, t => t.Type == TokenType.Identifier && t.Lexeme == "x");
            Assert.Contains(tokens, t => t.Type == TokenType.As);
            Assert.Contains(tokens, t => t.Type == TokenType.Integer);
            Assert.Contains(tokens, t => t.Type == TokenType.Assignment);
            Assert.Contains(tokens, t => t.Type == TokenType.IntegerLiteral);
        }

        [Fact]
        public void Tokenize_FunctionDeclaration_ReturnsCorrectTokens()
        {
            var source = "Function Add(a As Integer, b As Integer) As Integer";
            var tokens = Tokenize(source);

            Assert.Contains(tokens, t => t.Type == TokenType.Function);
            Assert.Contains(tokens, t => t.Type == TokenType.Identifier && t.Lexeme == "Add");
            Assert.Contains(tokens, t => t.Type == TokenType.LeftParen);
            Assert.Contains(tokens, t => t.Type == TokenType.Comma);
            Assert.Contains(tokens, t => t.Type == TokenType.RightParen);
        }

        [Fact]
        public void Tokenize_IfStatement_ReturnsCorrectTokens()
        {
            var source = "If x > 0 Then";
            var tokens = Tokenize(source);

            Assert.Contains(tokens, t => t.Type == TokenType.If);
            Assert.Contains(tokens, t => t.Type == TokenType.GreaterThan);
            Assert.Contains(tokens, t => t.Type == TokenType.Then);
        }

        [Fact]
        public void Tokenize_ClassDeclaration_ReturnsCorrectTokens()
        {
            var source = "Public Class MyClass";
            var tokens = Tokenize(source);

            Assert.Contains(tokens, t => t.Type == TokenType.Public);
            Assert.Contains(tokens, t => t.Type == TokenType.Class);
            Assert.Contains(tokens, t => t.Type == TokenType.Identifier && t.Lexeme == "MyClass");
        }
    }
}
