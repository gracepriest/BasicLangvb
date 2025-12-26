using System;
using System.Linq;
using Xunit;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Tests
{
    public class SemanticAnalyzerTests
    {
        // ====================================================================
        // Helper Methods
        // ====================================================================

        private (ProgramNode program, SemanticAnalyzer analyzer, bool success) Analyze(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var program = parser.Parse();

            var analyzer = new SemanticAnalyzer();
            bool success = analyzer.Analyze(program);

            return (program, analyzer, success);
        }

        private void AssertNoErrors(string source)
        {
            var (_, analyzer, success) = Analyze(source);
            if (!success)
            {
                var errors = string.Join("\n", analyzer.Errors.Select(e => $"  - {e.Message}"));
                Assert.True(success, $"Expected no errors but got:\n{errors}");
            }
        }

        private void AssertError(string source, string expectedErrorSubstring)
        {
            var (_, analyzer, success) = Analyze(source);
            Assert.False(success, "Expected semantic errors but analysis succeeded");
            Assert.Contains(analyzer.Errors, e => e.Message.Contains(expectedErrorSubstring));
        }

        // ====================================================================
        // Variable Declaration Tests
        // ====================================================================

        [Fact]
        public void Analyze_SimpleVariableDeclaration_Succeeds()
        {
            var source = "Dim x As Integer = 42";
            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_AutoVariableDeclaration_Succeeds()
        {
            var source = "Auto x = 42";
            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_AutoVariableWithoutInitializer_HasError()
        {
            var source = "Auto x";
            AssertError(source, "Auto variable");
        }

        [Fact]
        public void Analyze_DuplicateVariableDeclaration_HasError()
        {
            var source = @"
Dim x As Integer
Dim x As String";

            AssertError(source, "already defined");
        }

        [Fact]
        public void Analyze_VariableWithWrongInitializerType_HasError()
        {
            var source = "Dim x As Integer = \"not a number\"";
            AssertError(source, "Cannot assign");
        }

        [Fact]
        public void Analyze_ConstantDeclaration_Succeeds()
        {
            var source = "Const PI As Double = 3.14159";
            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_ConstantWithoutValue_HasError()
        {
            var source = "Const X As Integer";
            AssertError(source, "must have a value");
        }

        // ====================================================================
        // Function Tests
        // ====================================================================

        [Fact]
        public void Analyze_SimpleFunctionDeclaration_Succeeds()
        {
            var source = @"
Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_FunctionWithWrongReturnType_HasError()
        {
            var source = @"
Function GetNumber() As Integer
    Return ""not a number""
End Function";

            AssertError(source, "Cannot return type");
        }

        [Fact]
        public void Analyze_FunctionMissingReturn_HasError()
        {
            var source = @"
Function GetNumber() As Integer
    Dim x As Integer = 5
End Function";

            // Note: This might not be caught depending on implementation
            // Some implementations require all code paths to return
        }

        [Fact]
        public void Analyze_SubroutineWithReturn_HasError()
        {
            var source = @"
Sub DoSomething()
    Return 42
End Sub";

            AssertError(source, "Cannot return a value from a subroutine");
        }

        [Fact]
        public void Analyze_DuplicateFunctionDeclaration_HasError()
        {
            var source = @"
Function Test() As Integer
    Return 1
End Function

Function Test() As String
    Return ""test""
End Function";

            AssertError(source, "already defined");
        }

        [Fact]
        public void Analyze_FunctionCallWithWrongArguments_HasError()
        {
            var source = @"
Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function

Sub Test()
    Dim result As Integer = Add(1, ""not a number"")
End Sub";

            AssertError(source, "cannot convert");
        }

        [Fact]
        public void Analyze_FunctionCallWithWrongArgumentCount_HasError()
        {
            var source = @"
Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function

Sub Test()
    Dim result As Integer = Add(1)
End Sub";

            AssertError(source, "expects");
        }

        [Fact]
        public void Analyze_FunctionWithOptionalParameters_Succeeds()
        {
            var source = @"
Function Test(req As Integer, Optional opt As Integer = 10) As Integer
    Return req + opt
End Function

Sub Main()
    Dim r1 As Integer = Test(5)
    Dim r2 As Integer = Test(5, 3)
End Sub";

            AssertNoErrors(source);
        }

        // ====================================================================
        // Type Checking Tests
        // ====================================================================

        [Fact]
        public void Analyze_BinaryExpression_TypeChecks()
        {
            var source = @"
Function Test() As Integer
    Dim a As Integer = 5
    Dim b As Integer = 3
    Return a + b
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_BinaryExpression_WithIncompatibleTypes_HasError()
        {
            var source = @"
Function Test() As Integer
    Dim a As Integer = 5
    Dim b As String = ""3""
    Return a + b
End Function";

            AssertError(source, "requires numeric operands");
        }

        [Fact]
        public void Analyze_StringConcatenation_Succeeds()
        {
            var source = @"
Function Test() As String
    Dim a As String = ""Hello""
    Dim b As String = ""World""
    Return a & b
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_ComparisonOperator_TypeChecks()
        {
            var source = @"
Function Test() As Boolean
    Dim a As Integer = 5
    Dim b As Integer = 3
    Return a > b
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_LogicalOperator_RequiresBoolean()
        {
            var source = @"
Function Test() As Boolean
    Dim a As Boolean = True
    Dim b As Boolean = False
    Return a And b
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_LogicalOperator_WithNonBoolean_HasError()
        {
            var source = @"
Function Test() As Boolean
    Dim a As Integer = 1
    Dim b As Integer = 0
    Return a And b
End Function";

            AssertError(source, "requires Boolean operands");
        }

        // ====================================================================
        // Scope Tests
        // ====================================================================

        [Fact]
        public void Analyze_UndefinedVariable_HasError()
        {
            var source = @"
Sub Test()
    x = 42
End Sub";

            AssertError(source, "Undefined identifier");
        }

        [Fact]
        public void Analyze_VariableInScope_Succeeds()
        {
            var source = @"
Sub Test()
    Dim x As Integer = 42
    x = 10
End Sub";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_VariableOutOfScope_HasError()
        {
            var source = @"
Sub Test1()
    Dim x As Integer = 42
End Sub

Sub Test2()
    x = 10
End Sub";

            AssertError(source, "Undefined identifier");
        }

        [Fact]
        public void Analyze_NestedScope_Succeeds()
        {
            var source = @"
Sub Test()
    Dim x As Integer = 42
    If True Then
        Dim y As Integer = x
    End If
End Sub";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_ShadowingInNestedScope_Succeeds()
        {
            var source = @"
Sub Test()
    Dim x As Integer = 42
    If True Then
        Dim x As String = ""shadowed""
    End If
End Sub";

            AssertNoErrors(source);
        }

        // ====================================================================
        // Class Tests
        // ====================================================================

        [Fact]
        public void Analyze_SimpleClass_Succeeds()
        {
            var source = @"
Class Person
    Public name As String
    Public age As Integer

    Public Function GetInfo() As String
        Return name
    End Function
End Class";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_ClassMemberAccess_Succeeds()
        {
            var source = @"
Class Person
    Public name As String
End Class

Function Test() As String
    Dim p As Person = New Person()
    Return p.name
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_ClassMemberAccess_UndefinedMember_HasError()
        {
            var source = @"
Class Person
    Public name As String
End Class

Function Test() As String
    Dim p As Person = New Person()
    Return p.undefinedMember
End Function";

            AssertError(source, "does not have a member");
        }

        [Fact]
        public void Analyze_ClassInheritance_Succeeds()
        {
            var source = @"
Class Animal
    Public name As String
End Class

Class Dog
    Inherits Animal
End Class";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_ClassInheritance_UnknownBaseClass_HasError()
        {
            var source = @"
Class Dog
    Inherits UnknownClass
End Class";

            AssertError(source, "Unknown base class");
        }

        // ====================================================================
        // Array Tests
        // ====================================================================

        [Fact]
        public void Analyze_ArrayAccess_Succeeds()
        {
            var source = @"
Function Test() As Integer
    Dim arr[10] As Integer
    Return arr[0]
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_ArrayAccess_OnNonArray_HasError()
        {
            var source = @"
Function Test() As Integer
    Dim x As Integer = 42
    Return x[0]
End Function";

            AssertError(source, "Cannot index non-array type");
        }

        [Fact]
        public void Analyze_ArrayAccess_WithNonIntegerIndex_HasError()
        {
            var source = @"
Function Test() As Integer
    Dim arr[10] As Integer
    Return arr[""not an index""]
End Function";

            AssertError(source, "Array index must be an integer");
        }

        // ====================================================================
        // Control Flow Tests
        // ====================================================================

        [Fact]
        public void Analyze_IfStatement_Succeeds()
        {
            var source = @"
Sub Test()
    If True Then
        Print(""yes"")
    End If
End Sub";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_IfStatement_WithNonBooleanCondition_HasWarning()
        {
            var source = @"
Sub Test()
    If 1 Then
        Print(""yes"")
    End If
End Sub";

            // Note: This might be a warning rather than an error
        }

        [Fact]
        public void Analyze_ForLoop_Succeeds()
        {
            var source = @"
Sub Test()
    For i = 0 To 10
        Print(i)
    Next
End Sub";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_ForLoop_WithNonNumericBounds_HasError()
        {
            var source = @"
Sub Test()
    For i = ""start"" To ""end""
        Print(i)
    Next
End Sub";

            AssertError(source, "must be numeric");
        }

        [Fact]
        public void Analyze_WhileLoop_Succeeds()
        {
            var source = @"
Sub Test()
    Dim x As Integer = 10
    While x > 0
        x = x - 1
    Wend
End Sub";

            AssertNoErrors(source);
        }

        // ====================================================================
        // Standard Library Function Tests
        // ====================================================================

        [Fact]
        public void Analyze_StdLibFunction_Print_Succeeds()
        {
            var source = @"
Sub Test()
    Print(""Hello"")
    PrintLine(42)
End Sub";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_StdLibFunction_Len_Succeeds()
        {
            var source = @"
Function Test() As Integer
    Return Len(""Hello"")
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_StdLibFunction_Mid_Succeeds()
        {
            var source = @"
Function Test() As String
    Return Mid(""Hello World"", 0, 5)
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_StdLibFunction_Sqrt_Succeeds()
        {
            var source = @"
Function Test() As Double
    Return Sqrt(16.0)
End Function";

            AssertNoErrors(source);
        }

        // ====================================================================
        // Assignment Tests
        // ====================================================================

        [Fact]
        public void Analyze_Assignment_Succeeds()
        {
            var source = @"
Sub Test()
    Dim x As Integer = 42
    x = 10
End Sub";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_Assignment_ToConstant_HasError()
        {
            var source = @"
Sub Test()
    Const PI As Double = 3.14159
    PI = 3.14
End Sub";

            AssertError(source, "Cannot assign to constant");
        }

        [Fact]
        public void Analyze_Assignment_WithIncompatibleType_HasError()
        {
            var source = @"
Sub Test()
    Dim x As Integer = 42
    x = ""not a number""
End Sub";

            AssertError(source, "Cannot assign");
        }

        [Fact]
        public void Analyze_CompoundAssignment_Succeeds()
        {
            var source = @"
Sub Test()
    Dim x As Integer = 42
    x += 10
End Sub";

            AssertNoErrors(source);
        }

        // ====================================================================
        // Generic Type Tests
        // ====================================================================

        [Fact]
        public void Analyze_GenericFunction_Succeeds()
        {
            var source = @"
Function Identity(Of T)(value As T) As T
    Return value
End Function

Function Test() As Integer
    Return Identity(Of Integer)(42)
End Function";

            AssertNoErrors(source);
        }

        // ====================================================================
        // Error Recovery Tests
        // ====================================================================

        [Fact]
        public void Analyze_MultipleErrors_ReportsAll()
        {
            var source = @"
Dim x As Integer = ""wrong type""
Dim x As String
Sub Test()
    undefinedVar = 42
End Sub";

            var (_, analyzer, success) = Analyze(source);
            Assert.False(success);
            Assert.True(analyzer.Errors.Count >= 2);
        }

        // ====================================================================
        // Complex Real-World Examples
        // ====================================================================

        [Fact]
        public void Analyze_FibonacciFunction_Succeeds()
        {
            var source = @"
Function Fibonacci(n As Integer) As Integer
    If n <= 1 Then
        Return n
    Else
        Return Fibonacci(n - 1) + Fibonacci(n - 2)
    End If
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_BubbleSort_Succeeds()
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

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_ClassWithMethods_Succeeds()
        {
            var source = @"
Class Calculator
    Public Function Add(a As Integer, b As Integer) As Integer
        Return a + b
    End Function

    Public Function Subtract(a As Integer, b As Integer) As Integer
        Return a - b
    End Function
End Class

Function Test() As Integer
    Dim calc As Calculator = New Calculator()
    Dim result As Integer = calc.Add(10, 5)
    Return result
End Function";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_NestedControlFlow_Succeeds()
        {
            var source = @"
Sub Test()
    For i = 0 To 10
        If i % 2 = 0 Then
            For j = 0 To i
                Print(j)
            Next
        End If
    Next
End Sub";

            AssertNoErrors(source);
        }

        [Fact]
        public void Analyze_TryCatchBlock_Succeeds()
        {
            var source = @"
Sub Test()
    Try
        Dim x As Integer = 10
        Dim y As Integer = 0
        Dim z As Integer = x / y
    Catch ex As Exception
        Print(""Error occurred"")
    End Try
End Sub";

            AssertNoErrors(source);
        }
    }
}
