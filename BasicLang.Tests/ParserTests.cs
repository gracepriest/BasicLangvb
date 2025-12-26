using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;

namespace BasicLang.Tests
{
    public class ParserTests
    {
        // ====================================================================
        // Helper Methods
        // ====================================================================

        private ProgramNode Parse(string source)
        {
            var lexer = new Lexer(source);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            return parser.Parse();
        }

        private T ParseSingle<T>(string source) where T : ASTNode
        {
            var program = Parse(source);
            Assert.NotNull(program);
            Assert.Single(program.Declarations);
            return Assert.IsType<T>(program.Declarations[0]);
        }

        // ====================================================================
        // Variable Declaration Tests
        // ====================================================================

        [Fact]
        public void Parse_SimpleVariableDeclaration_ReturnsCorrectAST()
        {
            var varDecl = ParseSingle<VariableDeclarationNode>("Dim x As Integer");

            Assert.Equal("x", varDecl.Name);
            Assert.Equal("Integer", varDecl.Type.Name);
            Assert.Null(varDecl.Initializer);
        }

        [Fact]
        public void Parse_VariableDeclarationWithInitializer_ReturnsCorrectAST()
        {
            var varDecl = ParseSingle<VariableDeclarationNode>("Dim x As Integer = 42");

            Assert.Equal("x", varDecl.Name);
            Assert.Equal("Integer", varDecl.Type.Name);
            Assert.NotNull(varDecl.Initializer);

            var literal = Assert.IsType<LiteralExpressionNode>(varDecl.Initializer);
            Assert.Equal(42, literal.Value);
        }

        [Fact]
        public void Parse_AutoVariableDeclaration_ReturnsCorrectAST()
        {
            var varDecl = ParseSingle<VariableDeclarationNode>("Auto x = 42");

            Assert.Equal("x", varDecl.Name);
            Assert.True(varDecl.IsAuto);
            Assert.NotNull(varDecl.Initializer);
        }

        [Fact]
        public void Parse_ConstantDeclaration_ReturnsCorrectAST()
        {
            var constDecl = ParseSingle<ConstantDeclarationNode>("Const PI As Double = 3.14159");

            Assert.Equal("PI", constDecl.Name);
            Assert.Equal("Double", constDecl.Type.Name);
            Assert.NotNull(constDecl.Value);
        }

        [Fact]
        public void Parse_ArrayDeclaration_ReturnsCorrectAST()
        {
            var varDecl = ParseSingle<VariableDeclarationNode>("Dim arr[10] As Integer");

            Assert.Equal("arr", varDecl.Name);
            Assert.True(varDecl.Type.IsArray);
            Assert.Equal("Integer", varDecl.Type.Name);
            Assert.Single(varDecl.Type.ArrayDimensions);
        }

        // ====================================================================
        // Function and Subroutine Tests
        // ====================================================================

        [Fact]
        public void Parse_SimpleFunctionDeclaration_ReturnsCorrectAST()
        {
            var source = @"
Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function";

            var func = ParseSingle<FunctionNode>(source);

            Assert.Equal("Add", func.Name);
            Assert.Equal(2, func.Parameters.Count);
            Assert.Equal("a", func.Parameters[0].Name);
            Assert.Equal("b", func.Parameters[1].Name);
            Assert.Equal("Integer", func.ReturnType.Name);
            Assert.NotNull(func.Body);
        }

        [Fact]
        public void Parse_SubroutineDeclaration_ReturnsCorrectAST()
        {
            var source = @"
Sub PrintMessage(msg As String)
    Print(msg)
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);

            Assert.Equal("PrintMessage", sub.Name);
            Assert.Single(sub.Parameters);
            Assert.Equal("msg", sub.Parameters[0].Name);
            Assert.NotNull(sub.Body);
        }

        [Fact]
        public void Parse_FunctionWithOptionalParameters_ReturnsCorrectAST()
        {
            var source = "Function Test(req As Integer, Optional opt As Integer = 10) As Integer\nEnd Function";

            var func = ParseSingle<FunctionNode>(source);

            Assert.Equal(2, func.Parameters.Count);
            Assert.False(func.Parameters[0].IsOptional);
            Assert.True(func.Parameters[1].IsOptional);
            Assert.NotNull(func.Parameters[1].DefaultValue);
        }

        [Fact]
        public void Parse_FunctionWithByRefParameter_ReturnsCorrectAST()
        {
            var source = "Function Swap(ByRef a As Integer, ByRef b As Integer) As Integer\nEnd Function";

            var func = ParseSingle<FunctionNode>(source);

            Assert.True(func.Parameters[0].IsByRef);
            Assert.True(func.Parameters[1].IsByRef);
        }

        // ====================================================================
        // Expression Tests
        // ====================================================================

        [Fact]
        public void Parse_BinaryExpression_ReturnsCorrectAST()
        {
            var source = @"
Function Test() As Integer
    Return 2 + 3
End Function";

            var func = ParseSingle<FunctionNode>(source);
            var returnStmt = Assert.IsType<ReturnStatementNode>(func.Body.Statements[0]);
            var binExpr = Assert.IsType<BinaryExpressionNode>(returnStmt.Value);

            Assert.Equal("+", binExpr.Operator);
            Assert.IsType<LiteralExpressionNode>(binExpr.Left);
            Assert.IsType<LiteralExpressionNode>(binExpr.Right);
        }

        [Fact]
        public void Parse_ComplexExpression_ReturnsCorrectAST()
        {
            var source = @"
Function Test() As Integer
    Return a + b * c
End Function";

            var func = ParseSingle<FunctionNode>(source);
            var returnStmt = Assert.IsType<ReturnStatementNode>(func.Body.Statements[0]);
            var addExpr = Assert.IsType<BinaryExpressionNode>(returnStmt.Value);

            Assert.Equal("+", addExpr.Operator);
            var multExpr = Assert.IsType<BinaryExpressionNode>(addExpr.Right);
            Assert.Equal("*", multExpr.Operator);
        }

        [Fact]
        public void Parse_UnaryExpression_ReturnsCorrectAST()
        {
            var source = @"
Function Test() As Integer
    Return -x
End Function";

            var func = ParseSingle<FunctionNode>(source);
            var returnStmt = Assert.IsType<ReturnStatementNode>(func.Body.Statements[0]);
            var unaryExpr = Assert.IsType<UnaryExpressionNode>(returnStmt.Value);

            Assert.Equal("-", unaryExpr.Operator);
            Assert.IsType<IdentifierExpressionNode>(unaryExpr.Operand);
        }

        [Fact]
        public void Parse_FunctionCall_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    Print(""Hello"")
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var exprStmt = Assert.IsType<ExpressionStatementNode>(sub.Body.Statements[0]);
            var callExpr = Assert.IsType<CallExpressionNode>(exprStmt.Expression);

            var callee = Assert.IsType<IdentifierExpressionNode>(callExpr.Callee);
            Assert.Equal("Print", callee.Name);
            Assert.Single(callExpr.Arguments);
        }

        [Fact]
        public void Parse_ArrayAccess_ReturnsCorrectAST()
        {
            var source = @"
Function Test() As Integer
    Return arr[0]
End Function";

            var func = ParseSingle<FunctionNode>(source);
            var returnStmt = Assert.IsType<ReturnStatementNode>(func.Body.Statements[0]);
            var arrayAccess = Assert.IsType<ArrayAccessExpressionNode>(returnStmt.Value);

            var arr = Assert.IsType<IdentifierExpressionNode>(arrayAccess.Array);
            Assert.Equal("arr", arr.Name);
            Assert.Single(arrayAccess.Indices);
        }

        [Fact]
        public void Parse_MemberAccess_ReturnsCorrectAST()
        {
            var source = @"
Function Test() As Integer
    Return obj.Property
End Function";

            var func = ParseSingle<FunctionNode>(source);
            var returnStmt = Assert.IsType<ReturnStatementNode>(func.Body.Statements[0]);
            var memberAccess = Assert.IsType<MemberAccessExpressionNode>(returnStmt.Value);

            Assert.Equal("Property", memberAccess.MemberName);
        }

        // ====================================================================
        // Statement Tests
        // ====================================================================

        [Fact]
        public void Parse_IfStatement_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    If x > 0 Then
        Print(""positive"")
    End If
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var ifStmt = Assert.IsType<IfStatementNode>(sub.Body.Statements[0]);

            Assert.NotNull(ifStmt.Condition);
            Assert.NotNull(ifStmt.ThenBlock);
            Assert.Null(ifStmt.ElseBlock);
        }

        [Fact]
        public void Parse_IfElseStatement_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    If x > 0 Then
        Print(""positive"")
    Else
        Print(""non-positive"")
    End If
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var ifStmt = Assert.IsType<IfStatementNode>(sub.Body.Statements[0]);

            Assert.NotNull(ifStmt.ThenBlock);
            Assert.NotNull(ifStmt.ElseBlock);
        }

        [Fact]
        public void Parse_IfElseIfStatement_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    If x > 0 Then
        Print(""positive"")
    ElseIf x < 0 Then
        Print(""negative"")
    Else
        Print(""zero"")
    End If
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var ifStmt = Assert.IsType<IfStatementNode>(sub.Body.Statements[0]);

            Assert.Single(ifStmt.ElseIfClauses);
            Assert.NotNull(ifStmt.ElseBlock);
        }

        [Fact]
        public void Parse_ForLoop_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    For i = 0 To 10
        Print(i)
    Next
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var forLoop = Assert.IsType<ForLoopNode>(sub.Body.Statements[0]);

            Assert.Equal("i", forLoop.Variable);
            Assert.NotNull(forLoop.Start);
            Assert.NotNull(forLoop.End);
            Assert.Null(forLoop.Step);
            Assert.NotNull(forLoop.Body);
        }

        [Fact]
        public void Parse_ForLoopWithStep_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    For i = 0 To 10 Step 2
        Print(i)
    Next
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var forLoop = Assert.IsType<ForLoopNode>(sub.Body.Statements[0]);

            Assert.NotNull(forLoop.Step);
        }

        [Fact]
        public void Parse_WhileLoop_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    While x > 0
        x = x - 1
    Wend
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var whileLoop = Assert.IsType<WhileLoopNode>(sub.Body.Statements[0]);

            Assert.NotNull(whileLoop.Condition);
            Assert.NotNull(whileLoop.Body);
        }

        [Fact]
        public void Parse_DoLoop_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    Do
        x = x - 1
    Loop While x > 0
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var doLoop = Assert.IsType<DoLoopNode>(sub.Body.Statements[0]);

            Assert.NotNull(doLoop.Body);
            Assert.NotNull(doLoop.Condition);
        }

        [Fact]
        public void Parse_SelectCase_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    Select Case x
        Case 1
            Print(""one"")
        Case 2
            Print(""two"")
        Case Else
            Print(""other"")
    End Select
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var selectStmt = Assert.IsType<SelectStatementNode>(sub.Body.Statements[0]);

            Assert.NotNull(selectStmt.Expression);
            Assert.Equal(3, selectStmt.Cases.Count);
            Assert.True(selectStmt.Cases[2].IsElse);
        }

        [Fact]
        public void Parse_Assignment_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    x = 42
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var assignment = Assert.IsType<AssignmentStatementNode>(sub.Body.Statements[0]);

            var target = Assert.IsType<IdentifierExpressionNode>(assignment.Target);
            Assert.Equal("x", target.Name);
            Assert.Equal("=", assignment.Operator);
        }

        [Fact]
        public void Parse_CompoundAssignment_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    x += 5
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var assignment = Assert.IsType<AssignmentStatementNode>(sub.Body.Statements[0]);

            Assert.Equal("+=", assignment.Operator);
        }

        // ====================================================================
        // Class Tests
        // ====================================================================

        [Fact]
        public void Parse_SimpleClass_ReturnsCorrectAST()
        {
            var source = @"
Class Person
    Private name As String
    Public Function GetName() As String
        Return name
    End Function
End Class";

            var classNode = ParseSingle<ClassNode>(source);

            Assert.Equal("Person", classNode.Name);
            Assert.Equal(2, classNode.Members.Count);
        }

        [Fact]
        public void Parse_ClassWithInheritance_ReturnsCorrectAST()
        {
            var source = @"
Class Employee
    Inherits Person
End Class";

            var classNode = ParseSingle<ClassNode>(source);

            Assert.Equal("Employee", classNode.Name);
            Assert.Equal("Person", classNode.BaseClass);
        }

        [Fact]
        public void Parse_ClassWithInterface_ReturnsCorrectAST()
        {
            var source = @"
Class MyClass
    Implements IDisposable
End Class";

            var classNode = ParseSingle<ClassNode>(source);

            Assert.Single(classNode.Interfaces);
            Assert.Equal("IDisposable", classNode.Interfaces[0]);
        }

        // ====================================================================
        // Type Tests
        // ====================================================================

        [Fact]
        public void Parse_TypeDeclaration_ReturnsCorrectAST()
        {
            var source = @"
Type Point
    x As Integer
    y As Integer
End Type";

            var typeNode = ParseSingle<TypeNode>(source);

            Assert.Equal("Point", typeNode.Name);
            Assert.Equal(2, typeNode.Members.Count);
            Assert.Equal("x", typeNode.Members[0].Name);
            Assert.Equal("y", typeNode.Members[1].Name);
        }

        [Fact]
        public void Parse_EnumDeclaration_ReturnsCorrectAST()
        {
            var source = @"
Enum Color
    Red
    Green
    Blue
End Enum";

            var enumNode = ParseSingle<EnumNode>(source);

            Assert.Equal("Color", enumNode.Name);
            Assert.Equal(3, enumNode.Members.Count);
            Assert.Equal("Red", enumNode.Members[0].Name);
        }

        // ====================================================================
        // Error Handling Tests
        // ====================================================================

        [Fact]
        public void Parse_TryCatchFinally_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    Try
        DoSomething()
    Catch ex As Exception
        Print(ex)
    Finally
        Cleanup()
    End Try
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var tryStmt = Assert.IsType<TryStatementNode>(sub.Body.Statements[0]);

            Assert.NotNull(tryStmt.TryBlock);
            Assert.Single(tryStmt.CatchClauses);
            Assert.NotNull(tryStmt.FinallyBlock);
        }

        // ====================================================================
        // Edge Cases
        // ====================================================================

        [Fact]
        public void Parse_EmptyProgram_ReturnsEmptyAST()
        {
            var program = Parse("");

            Assert.NotNull(program);
            Assert.Empty(program.Declarations);
        }

        [Fact]
        public void Parse_OnlyComments_ReturnsEmptyAST()
        {
            var program = Parse("' This is a comment\n' Another comment");

            Assert.NotNull(program);
            Assert.Empty(program.Declarations);
        }

        [Fact]
        public void Parse_MultipleDeclarations_ReturnsAllDeclarations()
        {
            var source = @"
Dim x As Integer
Dim y As String
Function Test() As Integer
    Return 42
End Function";

            var program = Parse(source);

            Assert.Equal(3, program.Declarations.Count);
        }

        // ====================================================================
        // Complex Real-World Examples
        // ====================================================================

        [Fact]
        public void Parse_FibonacciFunction_ReturnsCorrectAST()
        {
            var source = @"
Function Fibonacci(n As Integer) As Integer
    If n <= 1 Then
        Return n
    Else
        Return Fibonacci(n - 1) + Fibonacci(n - 2)
    End If
End Function";

            var func = ParseSingle<FunctionNode>(source);

            Assert.Equal("Fibonacci", func.Name);
            Assert.Single(func.Parameters);
            Assert.NotNull(func.Body);

            var ifStmt = Assert.IsType<IfStatementNode>(func.Body.Statements[0]);
            Assert.NotNull(ifStmt.ThenBlock);
            Assert.NotNull(ifStmt.ElseBlock);
        }

        [Fact]
        public void Parse_GenericFunction_ReturnsCorrectAST()
        {
            var source = "Function Identity(Of T)(value As T) As T\n    Return value\nEnd Function";

            var func = ParseSingle<FunctionNode>(source);

            Assert.Equal("Identity", func.Name);
            Assert.Single(func.GenericParameters);
            Assert.Equal("T", func.GenericParameters[0]);
        }

        [Fact]
        public void Parse_PropertyDeclaration_ReturnsCorrectAST()
        {
            var source = @"
Class MyClass
    Private _value As Integer

    Public Property Value As Integer
        Get
            Return _value
        End Get
        Set
            _value = value
        End Set
    End Property
End Class";

            var classNode = ParseSingle<ClassNode>(source);
            var property = classNode.Members.OfType<PropertyNode>().First();

            Assert.Equal("Value", property.Name);
            Assert.NotNull(property.Getter);
            Assert.NotNull(property.Setter);
        }

        [Fact]
        public void Parse_NestedBlocks_ReturnsCorrectAST()
        {
            var source = @"
Sub Test()
    For i = 0 To 10
        If i % 2 = 0 Then
            Print(i)
        End If
    Next
End Sub";

            var sub = ParseSingle<SubroutineNode>(source);
            var forLoop = Assert.IsType<ForLoopNode>(sub.Body.Statements[0]);
            var ifStmt = Assert.IsType<IfStatementNode>(forLoop.Body.Statements[0]);

            Assert.NotNull(ifStmt.ThenBlock);
        }
    }
}
