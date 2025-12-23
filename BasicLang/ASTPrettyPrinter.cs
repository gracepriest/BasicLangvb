using System;
using System.Text;
using BasicLang.Compiler.AST;

namespace BasicLang.Compiler
{
    /// <summary>
    /// Visitor that pretty-prints the AST
    /// </summary>
    public class ASTPrettyPrinter : IASTVisitor
    {
        private readonly StringBuilder _output;
        private int _indent;
        
        public ASTPrettyPrinter()
        {
            _output = new StringBuilder();
            _indent = 0;
        }
        
        public string GetOutput()
        {
            return _output.ToString();
        }
        
        private void WriteLine(string text)
        {
            _output.AppendLine(new string(' ', _indent * 2) + text);
        }
        
        private void Indent()
        {
            _indent++;
        }
        
        private void Unindent()
        {
            _indent--;
        }
        
        public void Visit(ProgramNode node)
        {
            WriteLine("Program:");
            Indent();
            foreach (var declaration in node.Declarations)
            {
                declaration.Accept(this);
            }
            Unindent();
        }
        
        public void Visit(SubroutineNode node)
        {
            WriteLine($"{node.Access} Sub {node.Name}({node.Parameters.Count} parameters)");
            
            if (node.Parameters.Count > 0)
            {
                Indent();
                WriteLine("Parameters:");
                Indent();
                foreach (var param in node.Parameters)
                {
                    param.Accept(this);
                }
                Unindent();
                Unindent();
            }
            
            if (node.Body != null)
            {
                Indent();
                WriteLine("Body:");
                Indent();
                node.Body.Accept(this);
                Unindent();
                Unindent();
            }
        }
        
        public void Visit(FunctionNode node)
        {
            WriteLine($"{node.Access} Function {node.Name}({node.Parameters.Count} parameters) -> {node.ReturnType}");
            
            if (node.Parameters.Count > 0)
            {
                Indent();
                WriteLine("Parameters:");
                Indent();
                foreach (var param in node.Parameters)
                {
                    param.Accept(this);
                }
                Unindent();
                Unindent();
            }
            
            if (node.Body != null)
            {
                Indent();
                WriteLine("Body:");
                Indent();
                node.Body.Accept(this);
                Unindent();
                Unindent();
            }
        }
        
        public void Visit(ClassNode node)
        {
            var line = $"Class {node.Name}";
            
            if (node.GenericParameters.Count > 0)
            {
                line += $"<{string.Join(", ", node.GenericParameters)}>";
            }
            
            if (node.BaseClass != null)
            {
                line += $" : {node.BaseClass}";
            }
            
            if (node.Interfaces.Count > 0)
            {
                line += $", {string.Join(", ", node.Interfaces)}";
            }
            
            WriteLine(line);
            
            Indent();
            foreach (var member in node.Members)
            {
                member.Accept(this);
            }
            Unindent();
        }
        
        public void Visit(StructureNode node)
        {
            WriteLine($"Structure {node.Name}");
            Indent();
            foreach (var member in node.Members)
            {
                member.Accept(this);
            }
            Unindent();
        }
        
        public void Visit(TypeNode node)
        {
            WriteLine($"Type {node.Name}");
            Indent();
            foreach (var member in node.Members)
            {
                member.Accept(this);
            }
            Unindent();
        }
        
        public void Visit(InterfaceNode node)
        {
            WriteLine($"Interface {node.Name}");
            Indent();
            foreach (var method in node.Methods)
            {
                method.Accept(this);
            }
            Unindent();
        }
        
        public void Visit(ModuleNode node)
        {
            WriteLine($"Module {node.Name}");
            Indent();
            foreach (var member in node.Members)
            {
                member.Accept(this);
            }
            Unindent();
        }
        
        public void Visit(NamespaceNode node)
        {
            WriteLine($"Namespace {node.Name}");
            Indent();
            foreach (var member in node.Members)
            {
                member.Accept(this);
            }
            Unindent();
        }
        
        public void Visit(VariableDeclarationNode node)
        {
            var line = $"{node.Access} Var {node.Name} : {node.Type}";
            
            if (node.Initializer != null)
            {
                line += " = ";
                WriteLine(line);
                Indent();
                node.Initializer.Accept(this);
                Unindent();
            }
            else
            {
                WriteLine(line);
            }
        }
        
        public void Visit(ConstantDeclarationNode node)
        {
            WriteLine($"Const {node.Name} : {node.Type} = ");
            Indent();
            node.Value.Accept(this);
            Unindent();
        }
        
        public void Visit(TypeDefineNode node)
        {
            WriteLine($"TypeDefine {node.AliasName} = {node.BaseType}");
        }
        
        public void Visit(ParameterNode node)
        {
            var line = $"{node.Name} : {node.Type}";
            
            if (node.DefaultValue != null)
            {
                line += " = ";
                WriteLine(line);
                Indent();
                node.DefaultValue.Accept(this);
                Unindent();
            }
            else
            {
                WriteLine(line);
            }
        }
        
        public void Visit(BlockNode node)
        {
            WriteLine("Block:");
            Indent();
            foreach (var statement in node.Statements)
            {
                statement.Accept(this);
            }
            Unindent();
        }
        
        public void Visit(IfStatementNode node)
        {
            WriteLine("If:");
            Indent();
            WriteLine("Condition:");
            Indent();
            node.Condition.Accept(this);
            Unindent();
            
            WriteLine("Then:");
            Indent();
            node.ThenBlock.Accept(this);
            Unindent();
            
            foreach (var (condition, block) in node.ElseIfClauses)
            {
                WriteLine("ElseIf:");
                Indent();
                WriteLine("Condition:");
                Indent();
                condition.Accept(this);
                Unindent();
                
                WriteLine("Then:");
                Indent();
                block.Accept(this);
                Unindent();
                Unindent();
            }
            
            if (node.ElseBlock != null)
            {
                WriteLine("Else:");
                Indent();
                node.ElseBlock.Accept(this);
                Unindent();
            }
            
            Unindent();
        }
        
        public void Visit(SelectStatementNode node)
        {
            WriteLine("Select:");
            Indent();
            WriteLine("Expression:");
            Indent();
            node.Expression.Accept(this);
            Unindent();
            
            foreach (var caseClause in node.Cases)
            {
                caseClause.Accept(this);
            }
            Unindent();
        }
        
        public void Visit(CaseClauseNode node)
        {
            if (node.IsElse)
            {
                WriteLine("Case Else:");
            }
            else
            {
                WriteLine("Case:");
                Indent();
                foreach (var value in node.Values)
                {
                    value.Accept(this);
                }
                Unindent();
            }
            
            Indent();
            node.Body.Accept(this);
            Unindent();
        }
        
        public void Visit(ForLoopNode node)
        {
            WriteLine($"For {node.Variable} = ");
            Indent();
            
            WriteLine("Start:");
            Indent();
            node.Start.Accept(this);
            Unindent();
            
            WriteLine("End:");
            Indent();
            node.End.Accept(this);
            Unindent();
            
            if (node.Step != null)
            {
                WriteLine("Step:");
                Indent();
                node.Step.Accept(this);
                Unindent();
            }
            
            WriteLine("Body:");
            Indent();
            node.Body.Accept(this);
            Unindent();
            
            Unindent();
        }
        
        public void Visit(WhileLoopNode node)
        {
            WriteLine("While:");
            Indent();
            
            WriteLine("Condition:");
            Indent();
            node.Condition.Accept(this);
            Unindent();
            
            WriteLine("Body:");
            Indent();
            node.Body.Accept(this);
            Unindent();
            
            Unindent();
        }
        
        public void Visit(DoLoopNode node)
        {
            WriteLine($"Do{(node.IsWhile ? " While" : "")}:");
            Indent();
            
            WriteLine("Body:");
            Indent();
            node.Body.Accept(this);
            Unindent();
            
            if (node.Condition != null)
            {
                WriteLine("Condition:");
                Indent();
                node.Condition.Accept(this);
                Unindent();
            }
            
            Unindent();
        }
        
        public void Visit(ForEachLoopNode node)
        {
            WriteLine($"ForEach {node.Variable} : {node.VariableType} In:");
            Indent();
            
            WriteLine("Collection:");
            Indent();
            node.Collection.Accept(this);
            Unindent();
            
            WriteLine("Body:");
            Indent();
            node.Body.Accept(this);
            Unindent();
            
            Unindent();
        }
        
        public void Visit(TryStatementNode node)
        {
            WriteLine("Try:");
            Indent();
            
            WriteLine("Body:");
            Indent();
            node.TryBlock.Accept(this);
            Unindent();
            
            foreach (var catchClause in node.CatchClauses)
            {
                catchClause.Accept(this);
            }
            
            Unindent();
        }
        
        public void Visit(CatchClauseNode node)
        {
            WriteLine($"Catch {node.ExceptionVariable} : {node.ExceptionType}");
            Indent();
            node.Body.Accept(this);
            Unindent();
        }
        
        public void Visit(ReturnStatementNode node)
        {
            WriteLine("Return:");
            if (node.Value != null)
            {
                Indent();
                node.Value.Accept(this);
                Unindent();
            }
        }

        public void Visit(ExitStatementNode node)
        {
            WriteLine($"Exit {node.Kind}");
        }

        public void Visit(AssignmentStatementNode node)
        {
            WriteLine($"Assignment ({node.Operator}):");
            Indent();
            
            WriteLine("Target:");
            Indent();
            node.Target.Accept(this);
            Unindent();
            
            WriteLine("Value:");
            Indent();
            node.Value.Accept(this);
            Unindent();
            
            Unindent();
        }
        
        public void Visit(ExpressionStatementNode node)
        {
            WriteLine("ExpressionStatement:");
            Indent();
            node.Expression.Accept(this);
            Unindent();
        }
        
        public void Visit(BinaryExpressionNode node)
        {
            WriteLine($"BinaryOp ({node.Operator}):");
            Indent();
            
            WriteLine("Left:");
            Indent();
            node.Left.Accept(this);
            Unindent();
            
            WriteLine("Right:");
            Indent();
            node.Right.Accept(this);
            Unindent();
            
            Unindent();
        }
        
        public void Visit(UnaryExpressionNode node)
        {
            WriteLine($"UnaryOp ({node.Operator}, {(node.IsPostfix ? "Postfix" : "Prefix")}):");
            Indent();
            node.Operand.Accept(this);
            Unindent();
        }
        
        public void Visit(LiteralExpressionNode node)
        {
            WriteLine($"Literal: {node.Value} ({node.LiteralType})");
        }
        
        public void Visit(IdentifierExpressionNode node)
        {
            WriteLine($"Identifier: {node.Name}");
        }
        
        public void Visit(MemberAccessExpressionNode node)
        {
            WriteLine($"MemberAccess: .{node.MemberName}");
            Indent();
            WriteLine("Object:");
            Indent();
            node.Object.Accept(this);
            Unindent();
            Unindent();
        }
        
        public void Visit(CallExpressionNode node)
        {
            WriteLine($"Call ({node.Arguments.Count} arguments):");
            Indent();
            
            WriteLine("Callee:");
            Indent();
            node.Callee.Accept(this);
            Unindent();
            
            if (node.Arguments.Count > 0)
            {
                WriteLine("Arguments:");
                Indent();
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }
                Unindent();
            }
            
            Unindent();
        }
        
        public void Visit(ArrayAccessExpressionNode node)
        {
            WriteLine($"ArrayAccess ({node.Indices.Count} indices):");
            Indent();
            
            WriteLine("Array:");
            Indent();
            node.Array.Accept(this);
            Unindent();
            
            WriteLine("Indices:");
            Indent();
            foreach (var index in node.Indices)
            {
                index.Accept(this);
            }
            Unindent();
            
            Unindent();
        }
        
        public void Visit(NewExpressionNode node)
        {
            WriteLine($"New {node.Type} ({node.Arguments.Count} arguments)");
            
            if (node.Arguments.Count > 0)
            {
                Indent();
                WriteLine("Arguments:");
                Indent();
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }
                Unindent();
                Unindent();
            }
        }
        
        public void Visit(CastExpressionNode node)
        {
            WriteLine($"Cast to {node.TargetType}:");
            Indent();
            node.Expression.Accept(this);
            Unindent();
        }
        
        public void Visit(TemplateDeclarationNode node)
        {
            WriteLine($"Template <{string.Join(", ", node.TypeParameters)}>");
            Indent();
            node.Declaration.Accept(this);
            Unindent();
        }
        
        public void Visit(DelegateDeclarationNode node)
        {
            var returnType = node.ReturnType != null ? $" -> {node.ReturnType}" : "";
            WriteLine($"Delegate {node.Name}({node.Parameters.Count} parameters){returnType}");
        }
        
        public void Visit(ExtensionMethodNode node)
        {
            WriteLine($"Extension for {node.ExtendedType}:");
            Indent();
            node.Method.Accept(this);
            Unindent();
        }
        
        public void Visit(UsingDirectiveNode node)
        {
            WriteLine($"Using {node.Namespace}");
        }
        
        public void Visit(ImportDirectiveNode node)
        {
            WriteLine($"Import {node.Module}");
        }
    }
}
