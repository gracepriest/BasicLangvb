using System;
using System.Collections.Generic;
using BasicLang.Compiler;

namespace BasicLang.Compiler.AST
{
    /// <summary>
    /// Base class for all AST nodes
    /// </summary>
    public abstract class ASTNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
        
        protected ASTNode(int line, int column)
        {
            Line = line;
            Column = column;
        }
        
        public abstract void Accept(IASTVisitor visitor);
    }
    
    /// <summary>
    /// Visitor interface for traversing the AST
    /// </summary>
    public interface IASTVisitor
    {
        void Visit(ProgramNode node);
        void Visit(SubroutineNode node);
        void Visit(FunctionNode node);
        void Visit(ClassNode node);
        void Visit(StructureNode node);
        void Visit(TypeNode node);
        void Visit(InterfaceNode node);
        void Visit(ModuleNode node);
        void Visit(NamespaceNode node);
        void Visit(VariableDeclarationNode node);
        void Visit(ConstantDeclarationNode node);
        void Visit(TypeDefineNode node);
        void Visit(ParameterNode node);
        void Visit(BlockNode node);
        void Visit(IfStatementNode node);
        void Visit(SelectStatementNode node);
        void Visit(CaseClauseNode node);
        void Visit(ForLoopNode node);
        void Visit(WhileLoopNode node);
        void Visit(DoLoopNode node);
        void Visit(ForEachLoopNode node);
        void Visit(TryStatementNode node);
        void Visit(CatchClauseNode node);
        void Visit(ReturnStatementNode node);
        void Visit(AssignmentStatementNode node);
        void Visit(ExpressionStatementNode node);
        void Visit(BinaryExpressionNode node);
        void Visit(UnaryExpressionNode node);
        void Visit(LiteralExpressionNode node);
        void Visit(IdentifierExpressionNode node);
        void Visit(MemberAccessExpressionNode node);
        void Visit(CallExpressionNode node);
        void Visit(ArrayAccessExpressionNode node);
        void Visit(NewExpressionNode node);
        void Visit(CastExpressionNode node);
        void Visit(TemplateDeclarationNode node);
        void Visit(DelegateDeclarationNode node);
        void Visit(ExtensionMethodNode node);
        void Visit(UsingDirectiveNode node);
        void Visit(ImportDirectiveNode node);
    }
    
    // ============================================================================
    // Program Structure
    // ============================================================================
    
    public class ProgramNode : ASTNode
    {
        public List<ASTNode> Declarations { get; set; }
        
        public ProgramNode(int line, int column) : base(line, column)
        {
            Declarations = new List<ASTNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    // ============================================================================
    // Type Definitions
    // ============================================================================
    
    public class TypeReference
    {
        public string Name { get; set; }
        public bool IsPointer { get; set; }
        public bool IsArray { get; set; }
        public List<int> ArrayDimensions { get; set; }
        public List<TypeReference> GenericArguments { get; set; }
        
        public TypeReference(string name)
        {
            Name = name;
            IsPointer = false;
            IsArray = false;
            ArrayDimensions = new List<int>();
            GenericArguments = new List<TypeReference>();
        }
        
        public override string ToString()
        {
            var result = Name;
            
            if (GenericArguments.Count > 0)
            {
                result += $"(Of {string.Join(", ", GenericArguments)})";
            }
            
            if (IsPointer)
            {
                result = $"Pointer To {result}";
            }
            
            if (IsArray)
            {
                foreach (var dim in ArrayDimensions)
                {
                    result += $"[{(dim >= 0 ? dim.ToString() : "")}]";
                }
            }
            
            return result;
        }
    }
    
    public class TypeDefineNode : ASTNode
    {
        public string AliasName { get; set; }
        public TypeReference BaseType { get; set; }
        
        public TypeDefineNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class TypeNode : ASTNode
    {
        public string Name { get; set; }
        public List<VariableDeclarationNode> Members { get; set; }
        
        public TypeNode(int line, int column) : base(line, column)
        {
            Members = new List<VariableDeclarationNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class StructureNode : ASTNode
    {
        public string Name { get; set; }
        public List<VariableDeclarationNode> Members { get; set; }
        
        public StructureNode(int line, int column) : base(line, column)
        {
            Members = new List<VariableDeclarationNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    // ============================================================================
    // Object-Oriented Programming
    // ============================================================================
    
    public enum AccessModifier
    {
        Public,
        Private,
        Protected
    }
    
    public class ClassNode : ASTNode
    {
        public string Name { get; set; }
        public List<string> GenericParameters { get; set; }
        public string BaseClass { get; set; }
        public List<string> Interfaces { get; set; }
        public List<ASTNode> Members { get; set; }
        
        public ClassNode(int line, int column) : base(line, column)
        {
            GenericParameters = new List<string>();
            Interfaces = new List<string>();
            Members = new List<ASTNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class InterfaceNode : ASTNode
    {
        public string Name { get; set; }
        public List<FunctionNode> Methods { get; set; }
        
        public InterfaceNode(int line, int column) : base(line, column)
        {
            Methods = new List<FunctionNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    // ============================================================================
    // Modules and Namespaces
    // ============================================================================
    
    public class ModuleNode : ASTNode
    {
        public string Name { get; set; }
        public List<ASTNode> Members { get; set; }
        
        public ModuleNode(int line, int column) : base(line, column)
        {
            Members = new List<ASTNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class NamespaceNode : ASTNode
    {
        public string Name { get; set; }
        public List<ASTNode> Members { get; set; }
        
        public NamespaceNode(int line, int column) : base(line, column)
        {
            Members = new List<ASTNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class UsingDirectiveNode : ASTNode
    {
        public string Namespace { get; set; }
        
        public UsingDirectiveNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class ImportDirectiveNode : ASTNode
    {
        public string Module { get; set; }
        
        public ImportDirectiveNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    // ============================================================================
    // Functions and Subroutines
    // ============================================================================
    
    public class ParameterNode : ASTNode
    {
        public string Name { get; set; }
        public TypeReference Type { get; set; }
        public ExpressionNode DefaultValue { get; set; }
        
        public ParameterNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class SubroutineNode : ASTNode
    {
        public string Name { get; set; }
        public AccessModifier Access { get; set; }
        public List<ParameterNode> Parameters { get; set; }
        public BlockNode Body { get; set; }
        public string ImplementsInterface { get; set; }
        
        public SubroutineNode(int line, int column) : base(line, column)
        {
            Access = AccessModifier.Public;
            Parameters = new List<ParameterNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class FunctionNode : ASTNode
    {
        public string Name { get; set; }
        public AccessModifier Access { get; set; }
        public List<ParameterNode> Parameters { get; set; }
        public TypeReference ReturnType { get; set; }
        public BlockNode Body { get; set; }
        public string ImplementsInterface { get; set; }
        public bool IsAbstract { get; set; }
        
        public FunctionNode(int line, int column) : base(line, column)
        {
            Access = AccessModifier.Public;
            Parameters = new List<ParameterNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    // ============================================================================
    // Templates and Delegates
    // ============================================================================
    
    public class TemplateDeclarationNode : ASTNode
    {
        public string Name { get; set; }
        public List<string> TypeParameters { get; set; }
        public ASTNode Declaration { get; set; }
        
        public TemplateDeclarationNode(int line, int column) : base(line, column)
        {
            TypeParameters = new List<string>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class DelegateDeclarationNode : ASTNode
    {
        public string Name { get; set; }
        public List<ParameterNode> Parameters { get; set; }
        public TypeReference ReturnType { get; set; }
        
        public DelegateDeclarationNode(int line, int column) : base(line, column)
        {
            Parameters = new List<ParameterNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class ExtensionMethodNode : ASTNode
    {
        public string ExtendedType { get; set; }
        public FunctionNode Method { get; set; }
        
        public ExtensionMethodNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    // ============================================================================
    // Variable Declarations
    // ============================================================================
    
    public class VariableDeclarationNode : StatementNode
    {
        public string Name { get; set; }
        public TypeReference Type { get; set; }
        public ExpressionNode Initializer { get; set; }
        public AccessModifier Access { get; set; }
        public bool IsAuto { get; set; }
        
        public VariableDeclarationNode(int line, int column) : base(line, column)
        {
            Access = AccessModifier.Public;
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class ConstantDeclarationNode : StatementNode
    {
        public string Name { get; set; }
        public TypeReference Type { get; set; }
        public ExpressionNode Value { get; set; }
        
        public ConstantDeclarationNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    // ============================================================================
    // Statements
    // ============================================================================
    
    public abstract class StatementNode : ASTNode
    {
        protected StatementNode(int line, int column) : base(line, column) { }
    }
    
    public class BlockNode : StatementNode
    {
        public List<StatementNode> Statements { get; set; }
        
        public BlockNode(int line, int column) : base(line, column)
        {
            Statements = new List<StatementNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class IfStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public BlockNode ThenBlock { get; set; }
        public List<(ExpressionNode Condition, BlockNode Block)> ElseIfClauses { get; set; }
        public BlockNode ElseBlock { get; set; }
        
        public IfStatementNode(int line, int column) : base(line, column)
        {
            ElseIfClauses = new List<(ExpressionNode, BlockNode)>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class CaseClauseNode : ASTNode
    {
        public List<ExpressionNode> Values { get; set; }
        public BlockNode Body { get; set; }
        public bool IsElse { get; set; }
        
        public CaseClauseNode(int line, int column) : base(line, column)
        {
            Values = new List<ExpressionNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class SelectStatementNode : StatementNode
    {
        public ExpressionNode Expression { get; set; }
        public List<CaseClauseNode> Cases { get; set; }
        
        public SelectStatementNode(int line, int column) : base(line, column)
        {
            Cases = new List<CaseClauseNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class ForLoopNode : StatementNode
    {
        public string Variable { get; set; }
        public ExpressionNode Start { get; set; }
        public ExpressionNode End { get; set; }
        public ExpressionNode Step { get; set; }
        public BlockNode Body { get; set; }
        
        public ForLoopNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class WhileLoopNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public BlockNode Body { get; set; }
        
        public WhileLoopNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class DoLoopNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public BlockNode Body { get; set; }
        public bool IsWhile { get; set; }
        
        public DoLoopNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class ForEachLoopNode : StatementNode
    {
        public string Variable { get; set; }
        public TypeReference VariableType { get; set; }
        public ExpressionNode Collection { get; set; }
        public BlockNode Body { get; set; }
        
        public ForEachLoopNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class CatchClauseNode : ASTNode
    {
        public string ExceptionVariable { get; set; }
        public TypeReference ExceptionType { get; set; }
        public BlockNode Body { get; set; }
        
        public CatchClauseNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class TryStatementNode : StatementNode
    {
        public BlockNode TryBlock { get; set; }
        public List<CatchClauseNode> CatchClauses { get; set; }
        
        public TryStatementNode(int line, int column) : base(line, column)
        {
            CatchClauses = new List<CatchClauseNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class ReturnStatementNode : StatementNode
    {
        public ExpressionNode Value { get; set; }
        
        public ReturnStatementNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class AssignmentStatementNode : StatementNode
    {
        public ExpressionNode Target { get; set; }
        public string Operator { get; set; }
        public ExpressionNode Value { get; set; }
        
        public AssignmentStatementNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class ExpressionStatementNode : StatementNode
    {
        public ExpressionNode Expression { get; set; }
        
        public ExpressionStatementNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    // ============================================================================
    // Expressions
    // ============================================================================
    
    public abstract class ExpressionNode : ASTNode
    {
        protected ExpressionNode(int line, int column) : base(line, column) { }
    }
    
    public class BinaryExpressionNode : ExpressionNode
    {
        public ExpressionNode Left { get; set; }
        public string Operator { get; set; }
        public ExpressionNode Right { get; set; }
        
        public BinaryExpressionNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class UnaryExpressionNode : ExpressionNode
    {
        public string Operator { get; set; }
        public ExpressionNode Operand { get; set; }
        public bool IsPostfix { get; set; }
        
        public UnaryExpressionNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class LiteralExpressionNode : ExpressionNode
    {
        public object Value { get; set; }
        public TokenType LiteralType { get; set; }
        
        public LiteralExpressionNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class IdentifierExpressionNode : ExpressionNode
    {
        public string Name { get; set; }
        
        public IdentifierExpressionNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class MemberAccessExpressionNode : ExpressionNode
    {
        public ExpressionNode Object { get; set; }
        public string MemberName { get; set; }
        
        public MemberAccessExpressionNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class CallExpressionNode : ExpressionNode
    {
        public ExpressionNode Callee { get; set; }
        public List<ExpressionNode> Arguments { get; set; }
        
        public CallExpressionNode(int line, int column) : base(line, column)
        {
            Arguments = new List<ExpressionNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class ArrayAccessExpressionNode : ExpressionNode
    {
        public ExpressionNode Array { get; set; }
        public List<ExpressionNode> Indices { get; set; }
        
        public ArrayAccessExpressionNode(int line, int column) : base(line, column)
        {
            Indices = new List<ExpressionNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class NewExpressionNode : ExpressionNode
    {
        public TypeReference Type { get; set; }
        public List<ExpressionNode> Arguments { get; set; }
        
        public NewExpressionNode(int line, int column) : base(line, column)
        {
            Arguments = new List<ExpressionNode>();
        }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
    
    public class CastExpressionNode : ExpressionNode
    {
        public ExpressionNode Expression { get; set; }
        public TypeReference TargetType { get; set; }
        
        public CastExpressionNode(int line, int column) : base(line, column) { }
        
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
}
