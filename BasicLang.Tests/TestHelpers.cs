using System;
using System.Collections.Generic;
using System.Linq;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Tests
{
    /// <summary>
    /// Helper utilities for testing BasicLang compiler
    /// </summary>
    public static class TestHelpers
    {
        // ====================================================================
        // Lexer Helpers
        // ====================================================================

        /// <summary>
        /// Tokenize source code and return all tokens
        /// </summary>
        public static List<Token> Tokenize(string source)
        {
            var lexer = new Lexer(source);
            return lexer.Tokenize();
        }

        /// <summary>
        /// Tokenize source code and return a specific token by index
        /// </summary>
        public static Token GetToken(string source, int index = 0)
        {
            var tokens = Tokenize(source);
            if (index < 0 || index >= tokens.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Token index {index} is out of range. Token count: {tokens.Count}");
            }
            return tokens[index];
        }

        /// <summary>
        /// Get all tokens of a specific type
        /// </summary>
        public static List<Token> GetTokensOfType(string source, TokenType type)
        {
            return Tokenize(source).Where(t => t.Type == type).ToList();
        }

        // ====================================================================
        // Parser Helpers
        // ====================================================================

        /// <summary>
        /// Parse source code and return the AST
        /// </summary>
        public static ProgramNode Parse(string source)
        {
            var tokens = Tokenize(source);
            var parser = new Parser(tokens);
            return parser.Parse();
        }

        /// <summary>
        /// Parse source code expecting a single declaration of a specific type
        /// </summary>
        public static T ParseSingle<T>(string source) where T : ASTNode
        {
            var program = Parse(source);

            if (program.Declarations.Count == 0)
            {
                throw new Exception("Expected one declaration but program is empty");
            }

            if (program.Declarations.Count > 1)
            {
                throw new Exception($"Expected one declaration but got {program.Declarations.Count}");
            }

            if (program.Declarations[0] is not T result)
            {
                throw new Exception($"Expected {typeof(T).Name} but got {program.Declarations[0].GetType().Name}");
            }

            return result;
        }

        /// <summary>
        /// Parse source code and return all declarations of a specific type
        /// </summary>
        public static List<T> ParseMultiple<T>(string source) where T : ASTNode
        {
            var program = Parse(source);
            return program.Declarations.OfType<T>().ToList();
        }

        // ====================================================================
        // Semantic Analysis Helpers
        // ====================================================================

        /// <summary>
        /// Perform semantic analysis on source code
        /// </summary>
        public static (ProgramNode program, SemanticAnalyzer analyzer, bool success) Analyze(string source)
        {
            var program = Parse(source);
            var analyzer = new SemanticAnalyzer();
            bool success = analyzer.Analyze(program);
            return (program, analyzer, success);
        }

        /// <summary>
        /// Assert that source code has no semantic errors
        /// </summary>
        public static void AssertNoErrors(string source)
        {
            var (_, analyzer, success) = Analyze(source);
            if (!success)
            {
                var errors = string.Join("\n", analyzer.Errors.Select(e =>
                    $"  Line {e.Line}, Col {e.Column}: {e.Message}"));
                throw new Exception($"Expected no semantic errors but got:\n{errors}");
            }
        }

        /// <summary>
        /// Assert that source code has semantic errors containing a specific message
        /// </summary>
        public static void AssertHasError(string source, string expectedErrorSubstring)
        {
            var (_, analyzer, success) = Analyze(source);

            if (success)
            {
                throw new Exception("Expected semantic errors but analysis succeeded");
            }

            var hasExpectedError = analyzer.Errors.Any(e =>
                e.Message.Contains(expectedErrorSubstring, StringComparison.OrdinalIgnoreCase));

            if (!hasExpectedError)
            {
                var actualErrors = string.Join("\n", analyzer.Errors.Select(e =>
                    $"  - {e.Message}"));
                throw new Exception(
                    $"Expected error containing '{expectedErrorSubstring}' but got:\n{actualErrors}");
            }
        }

        /// <summary>
        /// Get the type of an expression node
        /// </summary>
        public static TypeInfo GetExpressionType(string expressionSource, SemanticAnalyzer analyzer)
        {
            var source = $"Function Test() As Integer\n    Return {expressionSource}\nEnd Function";
            var program = Parse(source);
            analyzer.Analyze(program);

            var func = (FunctionNode)program.Declarations[0];
            var returnStmt = (ReturnStatementNode)func.Body.Statements[0];
            return analyzer.GetNodeType(returnStmt.Value);
        }

        // ====================================================================
        // AST Navigation Helpers
        // ====================================================================

        /// <summary>
        /// Find all nodes of a specific type in an AST
        /// </summary>
        public static List<T> FindNodesOfType<T>(ASTNode root) where T : ASTNode
        {
            var results = new List<T>();
            FindNodesRecursive(root, results);
            return results;
        }

        private static void FindNodesRecursive<T>(ASTNode node, List<T> results) where T : ASTNode
        {
            if (node is T typedNode)
            {
                results.Add(typedNode);
            }

            // Recursively search child nodes
            switch (node)
            {
                case ProgramNode program:
                    program.Declarations.ForEach(d => FindNodesRecursive(d, results));
                    break;
                case FunctionNode func:
                    func.Parameters.ForEach(p => FindNodesRecursive(p, results));
                    if (func.Body != null) FindNodesRecursive(func.Body, results);
                    break;
                case SubroutineNode sub:
                    sub.Parameters.ForEach(p => FindNodesRecursive(p, results));
                    if (sub.Body != null) FindNodesRecursive(sub.Body, results);
                    break;
                case BlockNode block:
                    block.Statements.ForEach(s => FindNodesRecursive(s, results));
                    break;
                case BinaryExpressionNode binary:
                    FindNodesRecursive(binary.Left, results);
                    FindNodesRecursive(binary.Right, results);
                    break;
                case UnaryExpressionNode unary:
                    FindNodesRecursive(unary.Operand, results);
                    break;
                case IfStatementNode ifStmt:
                    FindNodesRecursive(ifStmt.Condition, results);
                    FindNodesRecursive(ifStmt.ThenBlock, results);
                    if (ifStmt.ElseBlock != null) FindNodesRecursive(ifStmt.ElseBlock, results);
                    break;
                // Add more cases as needed for complete traversal
            }
        }

        // ====================================================================
        // Code Generation Helpers
        // ====================================================================

        /// <summary>
        /// Normalize whitespace for code comparison
        /// </summary>
        public static string NormalizeWhitespace(string code)
        {
            // Remove extra whitespace for easier comparison
            return System.Text.RegularExpressions.Regex.Replace(
                code.Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Assert that generated code contains a specific pattern
        /// </summary>
        public static void AssertCodeContains(string generatedCode, string expectedPattern)
        {
            if (!generatedCode.Contains(expectedPattern))
            {
                throw new Exception(
                    $"Expected generated code to contain '{expectedPattern}' but it didn't.\n" +
                    $"Generated code:\n{generatedCode}");
            }
        }

        // ====================================================================
        // Test Data Builders
        // ====================================================================

        /// <summary>
        /// Create a simple variable declaration for testing
        /// </summary>
        public static string CreateVariableDeclaration(string name, string type, string initializer = null)
        {
            if (string.IsNullOrEmpty(initializer))
            {
                return $"Dim {name} As {type}";
            }
            return $"Dim {name} As {type} = {initializer}";
        }

        /// <summary>
        /// Create a simple function for testing
        /// </summary>
        public static string CreateFunction(string name, string returnType, string body)
        {
            return $"Function {name}() As {returnType}\n{body}\nEnd Function";
        }

        /// <summary>
        /// Create a simple class for testing
        /// </summary>
        public static string CreateClass(string name, string members)
        {
            return $"Class {name}\n{members}\nEnd Class";
        }

        // ====================================================================
        // Assertion Helpers
        // ====================================================================

        /// <summary>
        /// Assert that a token has specific properties
        /// </summary>
        public static void AssertToken(Token token, TokenType expectedType,
            string expectedLexeme = null, object expectedValue = null)
        {
            if (token.Type != expectedType)
            {
                throw new Exception($"Expected token type {expectedType} but got {token.Type}");
            }

            if (expectedLexeme != null && token.Lexeme != expectedLexeme)
            {
                throw new Exception($"Expected lexeme '{expectedLexeme}' but got '{token.Lexeme}'");
            }

            if (expectedValue != null && !Equals(token.Value, expectedValue))
            {
                throw new Exception($"Expected value '{expectedValue}' but got '{token.Value}'");
            }
        }

        /// <summary>
        /// Assert that two AST nodes are structurally equivalent
        /// </summary>
        public static void AssertASTEquals(ASTNode expected, ASTNode actual)
        {
            if (expected == null && actual == null) return;

            if (expected == null || actual == null)
            {
                throw new Exception("One node is null while the other is not");
            }

            if (expected.GetType() != actual.GetType())
            {
                throw new Exception(
                    $"Node types don't match: expected {expected.GetType().Name}, " +
                    $"got {actual.GetType().Name}");
            }

            // Add more detailed comparison based on node type
            // This is a simplified version - extend as needed
        }

        // ====================================================================
        // Diagnostic Helpers
        // ====================================================================

        /// <summary>
        /// Print all tokens for debugging
        /// </summary>
        public static void PrintTokens(string source)
        {
            var tokens = Tokenize(source);
            Console.WriteLine("Tokens:");
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                Console.WriteLine($"  [{i}] {token.Type,-25} '{token.Lexeme}' = {token.Value}");
            }
        }

        /// <summary>
        /// Print AST structure for debugging
        /// </summary>
        public static void PrintAST(ASTNode node, int indent = 0)
        {
            var indentStr = new string(' ', indent * 2);
            Console.WriteLine($"{indentStr}{node.GetType().Name}");

            // Add recursive printing for child nodes
            // This is simplified - extend as needed
        }

        /// <summary>
        /// Print semantic errors for debugging
        /// </summary>
        public static void PrintSemanticErrors(SemanticAnalyzer analyzer)
        {
            if (analyzer.Errors.Count == 0)
            {
                Console.WriteLine("No semantic errors");
                return;
            }

            Console.WriteLine($"Semantic Errors ({analyzer.Errors.Count}):");
            foreach (var error in analyzer.Errors)
            {
                Console.WriteLine($"  Line {error.Line}, Col {error.Column}: {error.Message}");
            }
        }
    }
}
