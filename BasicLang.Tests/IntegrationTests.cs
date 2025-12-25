using System;
using System.Linq;
using Xunit;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;
using BasicLang.Compiler.SemanticAnalysis;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.CodeGen;
using BasicLang.Compiler.CodeGen.CSharp;

namespace BasicLang.Tests
{
    /// <summary>
    /// Integration tests for the full compilation pipeline from source to code generation
    /// </summary>
    public class IntegrationTests
    {
        // ====================================================================
        // Helper Methods
        // ====================================================================

        private ProgramNode ParseProgram(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            return parser.Parse();
        }

        private (ProgramNode program, SemanticAnalyzer analyzer) AnalyzeProgram(string source)
        {
            var program = ParseProgram(source);
            var analyzer = new SemanticAnalyzer();
            bool success = analyzer.Analyze(program);

            if (!success)
            {
                var errors = string.Join("\n", analyzer.Errors.Select(e => $"  - {e.Message}"));
                throw new Exception($"Semantic analysis failed:\n{errors}");
            }

            return (program, analyzer);
        }

        private IRModule BuildIR(string source)
        {
            var (program, analyzer) = AnalyzeProgram(source);
            var irBuilder = new IRBuilder(analyzer);
            return irBuilder.Build(program);
        }

        private string CompileToCSharp(string source)
        {
            var irModule = BuildIR(source);
            var backend = new CSharpCodeGenerator();
            return backend.Generate(irModule);
        }

        // ====================================================================
        // Full Pipeline Tests - Simple Programs
        // ====================================================================

        [Fact]
        public void Integration_SimpleVariableDeclaration_CompletePipeline()
        {
            var source = "Dim x As Integer = 42";

            // Lexing
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            Assert.NotEmpty(tokens);

            // Parsing
            var parser = new Parser(tokens);
            var program = parser.Parse();
            Assert.NotNull(program);
            Assert.Single(program.Declarations);

            // Semantic Analysis
            var analyzer = new SemanticAnalyzer();
            bool success = analyzer.Analyze(program);
            Assert.True(success, "Semantic analysis should succeed");

            // IR Generation
            var irBuilder = new IRBuilder(analyzer);
            var irModule = irBuilder.Build(program);
            Assert.NotNull(irModule);
        }

        [Fact]
        public void Integration_SimpleFunctionDeclaration_CompletePipeline()
        {
            var source = @"
Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function";

            var (program, analyzer) = AnalyzeProgram(source);
            Assert.NotNull(program);
            Assert.Single(program.Declarations);

            var func = Assert.IsType<FunctionNode>(program.Declarations[0]);
            Assert.Equal("Add", func.Name);

            var irModule = new IRBuilder(analyzer).Build(program);
            Assert.NotNull(irModule);
        }

        [Fact]
        public void Integration_HelloWorld_CompletePipeline()
        {
            var source = @"
Sub Main()
    Print(""Hello, World!"")
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.NotNull(csharpCode);
            Assert.Contains("Console.Write", csharpCode);
            Assert.Contains("Hello, World!", csharpCode);
        }

        // ====================================================================
        // Arithmetic and Expressions
        // ====================================================================

        [Fact]
        public void Integration_ArithmeticExpression_GeneratesCorrectCode()
        {
            var source = @"
Function Calculate() As Integer
    Return 2 + 3 * 4
End Function";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("2 + 3 * 4", csharpCode);
        }

        [Fact]
        public void Integration_VariableAssignment_GeneratesCorrectCode()
        {
            var source = @"
Sub Test()
    Dim x As Integer = 10
    x = x + 5
    Print(x)
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("int x", csharpCode);
            Assert.Contains("x = x + 5", csharpCode);
        }

        // ====================================================================
        // Control Flow
        // ====================================================================

        [Fact]
        public void Integration_IfStatement_GeneratesCorrectCode()
        {
            var source = @"
Sub Test(x As Integer)
    If x > 0 Then
        Print(""positive"")
    Else
        Print(""non-positive"")
    End If
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("if", csharpCode);
            Assert.Contains("else", csharpCode);
        }

        [Fact]
        public void Integration_ForLoop_GeneratesCorrectCode()
        {
            var source = @"
Sub Test()
    For i = 0 To 10
        Print(i)
    Next
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("for", csharpCode);
        }

        [Fact]
        public void Integration_WhileLoop_GeneratesCorrectCode()
        {
            var source = @"
Sub Test()
    Dim i As Integer = 0
    While i < 10
        Print(i)
        i = i + 1
    Wend
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("while", csharpCode);
        }

        [Fact]
        public void Integration_SelectCase_GeneratesCorrectCode()
        {
            var source = @"
Sub Test(x As Integer)
    Select Case x
        Case 1
            Print(""one"")
        Case 2
            Print(""two"")
        Case Else
            Print(""other"")
    End Select
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("switch", csharpCode);
            Assert.Contains("case", csharpCode);
        }

        // ====================================================================
        // Functions and Recursion
        // ====================================================================

        [Fact]
        public void Integration_Fibonacci_GeneratesCorrectCode()
        {
            var source = @"
Function Fibonacci(n As Integer) As Integer
    If n <= 1 Then
        Return n
    Else
        Return Fibonacci(n - 1) + Fibonacci(n - 2)
    End If
End Function";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("int Fibonacci", csharpCode);
            Assert.Contains("return", csharpCode);
            Assert.Contains("Fibonacci(", csharpCode); // Recursive call
        }

        [Fact]
        public void Integration_Factorial_GeneratesCorrectCode()
        {
            var source = @"
Function Factorial(n As Integer) As Integer
    If n <= 1 Then
        Return 1
    Else
        Return n * Factorial(n - 1)
    End If
End Function";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("int Factorial", csharpCode);
            Assert.Contains("return", csharpCode);
        }

        // ====================================================================
        // Arrays
        // ====================================================================

        [Fact]
        public void Integration_ArrayDeclaration_GeneratesCorrectCode()
        {
            var source = @"
Sub Test()
    Dim arr[10] As Integer
    arr[0] = 42
    Print(arr[0])
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("int[]", csharpCode);
            Assert.Contains("arr[0]", csharpCode);
        }

        [Fact]
        public void Integration_ArrayIteration_GeneratesCorrectCode()
        {
            var source = @"
Sub Test()
    Dim arr[5] As Integer
    For i = 0 To 4
        arr[i] = i * 2
    Next
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("int[]", csharpCode);
            Assert.Contains("for", csharpCode);
        }

        // ====================================================================
        // String Operations
        // ====================================================================

        [Fact]
        public void Integration_StringConcatenation_GeneratesCorrectCode()
        {
            var source = @"
Function Greet(name As String) As String
    Return ""Hello, "" & name & ""!""
End Function";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("string", csharpCode);
            Assert.Contains("Hello,", csharpCode);
        }

        [Fact]
        public void Integration_StringFunctions_GeneratesCorrectCode()
        {
            var source = @"
Sub Test()
    Dim s As String = ""Hello World""
    Dim length As Integer = Len(s)
    Dim substr As String = Mid(s, 0, 5)
    Print(substr)
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("string", csharpCode);
        }

        // ====================================================================
        // Classes and Objects
        // ====================================================================

        [Fact]
        public void Integration_SimpleClass_GeneratesCorrectCode()
        {
            var source = @"
Class Person
    Public name As String
    Public age As Integer

    Public Function GetInfo() As String
        Return name
    End Function
End Class";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("class Person", csharpCode);
            Assert.Contains("public string name", csharpCode);
            Assert.Contains("public int age", csharpCode);
        }

        [Fact]
        public void Integration_ClassInstantiation_GeneratesCorrectCode()
        {
            var source = @"
Class Point
    Public x As Integer
    Public y As Integer
End Class

Sub Test()
    Dim p As Point = New Point()
    p.x = 10
    p.y = 20
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("new Point()", csharpCode);
            Assert.Contains("p.x", csharpCode);
        }

        // ====================================================================
        // Error Handling
        // ====================================================================

        [Fact]
        public void Integration_TryCatch_GeneratesCorrectCode()
        {
            var source = @"
Sub Test()
    Try
        Dim x As Integer = 10
        Dim y As Integer = 0
    Catch ex As Exception
        Print(""Error"")
    End Try
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("try", csharpCode);
            Assert.Contains("catch", csharpCode);
        }

        // ====================================================================
        // Standard Library Functions
        // ====================================================================

        [Fact]
        public void Integration_MathFunctions_GeneratesCorrectCode()
        {
            var source = @"
Function Test() As Double
    Return Sqrt(16.0)
End Function";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("Math.Sqrt", csharpCode);
        }

        [Fact]
        public void Integration_PrintFunction_GeneratesCorrectCode()
        {
            var source = @"
Sub Test()
    Print(""Hello"")
    PrintLine(42)
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("Console.Write", csharpCode);
        }

        // ====================================================================
        // Complex Real-World Examples
        // ====================================================================

        [Fact]
        public void Integration_BubbleSort_CompletePipeline()
        {
            var source = @"
Sub BubbleSort(arr[100] As Integer)
    Dim n As Integer = UBound(arr)
    For i = 0 To n - 1
        For j = 0 To n - i - 1
            If arr[j] > arr[j + 1] Then
                Dim temp As Integer = arr[j]
                arr[j] = arr[j + 1]
                arr[j + 1] = temp
            End If
        Next
    Next
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("void BubbleSort", csharpCode);
            Assert.Contains("for", csharpCode);
            Assert.Contains("if", csharpCode);
        }

        [Fact]
        public void Integration_PrimeCheck_CompletePipeline()
        {
            var source = @"
Function IsPrime(n As Integer) As Boolean
    If n <= 1 Then
        Return False
    End If

    For i = 2 To n - 1
        If n % i = 0 Then
            Return False
        End If
    Next

    Return True
End Function";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("bool IsPrime", csharpCode);
            Assert.Contains("return", csharpCode);
        }

        [Fact]
        public void Integration_Calculator_CompletePipeline()
        {
            var source = @"
Class Calculator
    Public Function Add(a As Integer, b As Integer) As Integer
        Return a + b
    End Function

    Public Function Subtract(a As Integer, b As Integer) As Integer
        Return a - b
    End Function

    Public Function Multiply(a As Integer, b As Integer) As Integer
        Return a * b
    End Function

    Public Function Divide(a As Integer, b As Integer) As Integer
        If b = 0 Then
            Return 0
        End If
        Return a / b
    End Function
End Class";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("class Calculator", csharpCode);
            Assert.Contains("int Add", csharpCode);
            Assert.Contains("int Subtract", csharpCode);
            Assert.Contains("int Multiply", csharpCode);
            Assert.Contains("int Divide", csharpCode);
        }

        // ====================================================================
        // Edge Cases
        // ====================================================================

        [Fact]
        public void Integration_EmptyProgram_Succeeds()
        {
            var source = "";
            var program = ParseProgram(source);
            Assert.NotNull(program);
            Assert.Empty(program.Declarations);
        }

        [Fact]
        public void Integration_OnlyComments_Succeeds()
        {
            var source = "' This is a comment\n' Another comment";
            var program = ParseProgram(source);
            Assert.NotNull(program);
            Assert.Empty(program.Declarations);
        }

        [Fact]
        public void Integration_MultipleDeclarations_Succeeds()
        {
            var source = @"
Dim x As Integer = 1
Dim y As String = ""test""

Function Test() As Integer
    Return x
End Function

Sub Main()
    Print(y)
End Sub";

            var csharpCode = CompileToCSharp(source);
            Assert.Contains("int x", csharpCode);
            Assert.Contains("string y", csharpCode);
        }

        // ====================================================================
        // Error Cases
        // ====================================================================

        [Fact]
        public void Integration_SemanticError_ThrowsException()
        {
            var source = @"
Function Test() As Integer
    Return ""not a number""
End Function";

            Assert.Throws<Exception>(() => AnalyzeProgram(source));
        }

        [Fact]
        public void Integration_UndefinedVariable_ThrowsException()
        {
            var source = @"
Sub Test()
    x = 42
End Sub";

            Assert.Throws<Exception>(() => AnalyzeProgram(source));
        }

        [Fact]
        public void Integration_TypeMismatch_ThrowsException()
        {
            var source = @"
Sub Test()
    Dim x As Integer = ""not a number""
End Sub";

            Assert.Throws<Exception>(() => AnalyzeProgram(source));
        }

        // ====================================================================
        // Performance Tests (Optional)
        // ====================================================================

        [Fact]
        public void Integration_LargeProgram_CompletesInReasonableTime()
        {
            var sourceBuilder = new System.Text.StringBuilder();

            // Generate a large program with many functions
            for (int i = 0; i < 50; i++)
            {
                sourceBuilder.AppendLine($@"
Function Func{i}(x As Integer) As Integer
    Return x + {i}
End Function");
            }

            var source = sourceBuilder.ToString();
            var startTime = DateTime.Now;

            var csharpCode = CompileToCSharp(source);

            var elapsed = DateTime.Now - startTime;
            Assert.True(elapsed.TotalSeconds < 10, "Compilation should complete in less than 10 seconds");
            Assert.NotNull(csharpCode);
        }

        // ====================================================================
        // Backend-Specific Tests
        // ====================================================================

        [Fact]
        public void Integration_CSharpBackend_GeneratesValidCSharpSyntax()
        {
            var source = @"
Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function";

            var csharpCode = CompileToCSharp(source);

            // Basic syntax validation
            Assert.DoesNotContain("Dim", csharpCode); // BasicLang keyword should be converted
            Assert.DoesNotContain("End Function", csharpCode); // Should use C# braces
            Assert.Contains("int Add", csharpCode);
            Assert.Contains("return", csharpCode);
        }
    }
}
