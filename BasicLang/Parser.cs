using System;
using System.Collections.Generic;
using System.Linq;
using BasicLang.Compiler.AST;

namespace BasicLang.Compiler
{
    /// <summary>
    /// Recursive descent parser for BasicLang
    /// </summary>
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _current;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            _current = 0;

            // Remove comments and filter unnecessary tokens
            _tokens = _tokens.Where(t => t.Type != TokenType.Comment).ToList();
        }

        /// <summary>
        /// Parse the token stream into an AST
        /// </summary>
        public ProgramNode Parse()
        {
            var program = new ProgramNode(1, 1);

            while (!IsAtEnd())
            {
                SkipNewlines();

                if (IsAtEnd())
                    break;

                var declaration = ParseTopLevelDeclaration();
                if (declaration != null)
                {
                    program.Declarations.Add(declaration);
                }

                SkipNewlines();
            }

            return program;
        }

        // ====================================================================
        // Top-Level Declarations
        // ====================================================================

        private ASTNode ParseTopLevelDeclaration()
        {
            SkipNewlines();

            if (Check(TokenType.Namespace))
                return ParseNamespace();
            if (Check(TokenType.Module))
                return ParseModule();
            if (Check(TokenType.Using))
                return ParseUsing();
            if (Check(TokenType.Import))
                return ParseImport();
            if (Check(TokenType.Class))
                return ParseClass();
            if (Check(TokenType.Interface))
                return ParseInterface();
            if (Check(TokenType.Enum))
                return ParseEnum();
            if (Check(TokenType.Type))
                return ParseType();
            if (Check(TokenType.Structure))
                return ParseStructure();
            if (Check(TokenType.Template))
                return ParseTemplate();
            if (Check(TokenType.Delegate))
                return ParseDelegate();
            if (Check(TokenType.TypeDefine))
                return ParseTypeDefine();
            if (Check(TokenType.Extern))
                return ParseExtern();
            if (Check(TokenType.Extension))
                return ParseExtensionMethod();
            // Handle Async/Iterator modifiers for top-level functions/subs
            if (Check(TokenType.Async) || Check(TokenType.Iterator))
            {
                bool isAsync = false;
                bool isIterator = false;
                while (Check(TokenType.Async) || Check(TokenType.Iterator))
                {
                    if (Match(TokenType.Async)) isAsync = true;
                    if (Match(TokenType.Iterator)) isIterator = true;
                }
                if (Check(TokenType.Function))
                {
                    var func = ParseFunction();
                    func.IsAsync = isAsync;
                    func.IsIterator = isIterator;
                    return func;
                }
                if (Check(TokenType.Sub))
                {
                    var sub = ParseSubroutine();
                    sub.IsAsync = isAsync;
                    return sub;
                }
            }
            if (Check(TokenType.Function))
                return ParseFunction();
            if (Check(TokenType.Sub))
                return ParseSubroutine();
            if (Check(TokenType.Dim))
                return ParseVariableDeclaration();
            if (Check(TokenType.Auto))
                return ParseAutoDeclaration();
            if (Check(TokenType.Const))
                return ParseConstantDeclaration();

            throw new ParseException($"Unexpected token at top level: {Peek().Type}", Peek());
        }

        // ====================================================================
        // Namespaces and Modules
        // ====================================================================

        private NamespaceNode ParseNamespace()
        {
            var token = Consume(TokenType.Namespace, "Expected 'Namespace'");
            var node = new NamespaceNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected namespace name").Lexeme;
            ConsumeNewlines();

            while (!Check(TokenType.EndNamespace) && !IsAtEnd())
            {
                var member = ParseTopLevelDeclaration();
                if (member != null)
                {
                    node.Members.Add(member);
                }
                SkipNewlines();
            }

            Consume(TokenType.EndNamespace, "Expected 'End Namespace'");
            return node;
        }

        private ModuleNode ParseModule()
        {
            var token = Consume(TokenType.Module, "Expected 'Module'");
            var node = new ModuleNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected module name").Lexeme;
            ConsumeNewlines();

            while (!Check(TokenType.EndModule) && !IsAtEnd())
            {
                var member = ParseModuleMember();
                if (member != null)
                {
                    node.Members.Add(member);
                }
                SkipNewlines();
            }

            Consume(TokenType.EndModule, "Expected 'End Module'");
            return node;
        }

        private ASTNode ParseModuleMember()
        {
            // Handle attribute syntax <Extension>
            if (Check(TokenType.LessThan))
            {
                Advance(); // consume <
                if (Check(TokenType.Extension))
                {
                    Advance(); // consume Extension
                    Consume(TokenType.GreaterThan, "Expected '>' after attribute");
                    SkipNewlines();
                    return ParseExtensionMethodFromAttribute();
                }
                // Skip other unrecognized attributes
                while (!Check(TokenType.GreaterThan) && !IsAtEnd())
                    Advance();
                if (Check(TokenType.GreaterThan))
                    Advance();
                SkipNewlines();
            }

            // Handle Async/Iterator modifiers
            if (Check(TokenType.Async) || Check(TokenType.Iterator))
            {
                bool isAsync = false;
                bool isIterator = false;
                while (Check(TokenType.Async) || Check(TokenType.Iterator))
                {
                    if (Match(TokenType.Async)) isAsync = true;
                    if (Match(TokenType.Iterator)) isIterator = true;
                }
                if (Check(TokenType.Function))
                {
                    var func = ParseFunction();
                    func.IsAsync = isAsync;
                    func.IsIterator = isIterator;
                    return func;
                }
                if (Check(TokenType.Sub))
                {
                    var sub = ParseSubroutine();
                    sub.IsAsync = isAsync;
                    return sub;
                }
            }
            if (Check(TokenType.Function))
                return ParseFunction();
            if (Check(TokenType.Sub))
                return ParseSubroutine();
            if (Check(TokenType.Dim))
                return ParseVariableDeclaration();
            if (Check(TokenType.Const))
                return ParseConstantDeclaration();
            if (Check(TokenType.Extension))
                return ParseExtensionMethod();

            throw new ParseException($"Unexpected token in module: {Peek().Type}", Peek());
        }

        private UsingDirectiveNode ParseUsing()
        {
            var token = Consume(TokenType.Using, "Expected 'Using'");
            var node = new UsingDirectiveNode(token.Line, token.Column);

            node.Namespace = Consume(TokenType.Identifier, "Expected namespace name").Lexeme;
            ConsumeNewlines();

            return node;
        }

        private ImportDirectiveNode ParseImport()
        {
            var token = Consume(TokenType.Import, "Expected 'Import'");
            var node = new ImportDirectiveNode(token.Line, token.Column);

            node.Module = Consume(TokenType.Identifier, "Expected module name").Lexeme;
            ConsumeNewlines();

            return node;
        }

        // ====================================================================
        // Classes and Interfaces
        // ====================================================================

        private ClassNode ParseClass()
        {
            var token = Consume(TokenType.Class, "Expected 'Class'");
            var node = new ClassNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected class name").Lexeme;

            // Generic parameters
            if (Match(TokenType.LeftParen) && Check(TokenType.Of))
            {
                Consume(TokenType.Of, "Expected 'Of'");
                do
                {
                    node.GenericParameters.Add(Consume(TokenType.Identifier, "Expected type parameter").Lexeme);
                } while (Match(TokenType.Comma));

                Consume(TokenType.RightParen, "Expected ')' after generic parameters");
            }

            // Inheritance
            if (Match(TokenType.Inherits))
            {
                node.BaseClass = Consume(TokenType.Identifier, "Expected base class name").Lexeme;
            }

            // Interfaces
            if (Match(TokenType.Implements))
            {
                do
                {
                    node.Interfaces.Add(Consume(TokenType.Identifier, "Expected interface name").Lexeme);
                } while (Match(TokenType.Comma));
            }

            ConsumeNewlines();

            // Class members
            while (!Check(TokenType.EndClass) && !IsAtEnd())
            {
                var member = ParseClassMember();
                if (member != null)
                {
                    node.Members.Add(member);
                }
                SkipNewlines();
            }

            Consume(TokenType.EndClass, "Expected 'End Class'");
            return node;
        }

        private ASTNode ParseClassMember()
        {
            AccessModifier access = AccessModifier.Public;
            bool isStatic = false;
            bool isVirtual = false;
            bool isOverride = false;
            bool isAbstract = false;
            bool isSealed = false;
            bool isReadOnly = false;
            bool isWriteOnly = false;
            bool isAsync = false;
            bool isIterator = false;

            // Parse modifiers in any order
            while (true)
            {
                if (Check(TokenType.Public))
                {
                    Advance();
                    access = AccessModifier.Public;
                }
                else if (Check(TokenType.Private))
                {
                    Advance();
                    access = AccessModifier.Private;
                }
                else if (Check(TokenType.Protected))
                {
                    Advance();
                    access = AccessModifier.Protected;
                }
                else if (Check(TokenType.Shared))
                {
                    Advance();
                    isStatic = true;
                }
                else if (Check(TokenType.Overridable))
                {
                    Advance();
                    isVirtual = true;
                }
                else if (Check(TokenType.Overrides))
                {
                    Advance();
                    isOverride = true;
                }
                else if (Check(TokenType.MustOverride))
                {
                    Advance();
                    isAbstract = true;
                }
                else if (Check(TokenType.NotOverridable))
                {
                    Advance();
                    isSealed = true;
                }
                else if (Check(TokenType.ReadOnly))
                {
                    Advance();
                    isReadOnly = true;
                }
                else if (Check(TokenType.WriteOnly))
                {
                    Advance();
                    isWriteOnly = true;
                }
                else if (Check(TokenType.Async))
                {
                    Advance();
                    isAsync = true;
                }
                else if (Check(TokenType.Iterator))
                {
                    Advance();
                    isIterator = true;
                }
                else if (Check(TokenType.Widening))
                {
                    Advance();
                    isReadOnly = true;  // Reuse for Widening
                }
                else if (Check(TokenType.Narrowing))
                {
                    Advance();
                    isWriteOnly = true;  // Reuse for Narrowing
                }
                else
                {
                    break;
                }
            }

            // Property declaration
            if (Check(TokenType.Property))
            {
                var prop = ParseProperty();
                prop.Access = access;
                prop.IsStatic = isStatic;
                prop.IsReadOnly = isReadOnly;
                prop.IsWriteOnly = isWriteOnly;
                return prop;
            }

            // Event declaration
            if (Check(TokenType.Event))
            {
                var evt = ParseEventDeclaration();
                evt.Access = access;
                return evt;
            }

            // Operator overload declaration
            if (Check(TokenType.Operator))
            {
                var op = ParseOperatorDeclaration();
                op.Access = access;
                op.IsWidening = isReadOnly;   // Widening modifier reuses ReadOnly position
                op.IsNarrowing = isWriteOnly; // Narrowing modifier reuses WriteOnly position
                return op;
            }

            // Function declaration
            if (Check(TokenType.Function))
            {
                var func = ParseFunction();
                func.Access = access;
                func.IsStatic = isStatic;
                func.IsVirtual = isVirtual;
                func.IsOverride = isOverride;
                func.IsAbstract = isAbstract;
                func.IsSealed = isSealed;
                func.IsAsync = isAsync;
                func.IsIterator = isIterator;
                return func;
            }

            // Sub declaration - could be constructor (Sub New)
            if (Check(TokenType.Sub))
            {
                // Peek ahead to see if this is Sub New (constructor)
                if (PeekNext().Type == TokenType.New)
                {
                    var ctor = ParseConstructor();
                    ctor.Access = access;
                    return ctor;
                }

                var sub = ParseSubroutine();
                sub.Access = access;
                sub.IsStatic = isStatic;
                sub.IsVirtual = isVirtual;
                sub.IsOverride = isOverride;
                sub.IsSealed = isSealed;
                sub.IsAsync = isAsync;
                return sub;
            }

            // Field declaration with Dim
            if (Check(TokenType.Dim))
            {
                var field = ParseVariableDeclaration();
                field.Access = access;
                field.IsStatic = isStatic;
                return field;
            }

            // Field declaration without Dim (e.g., "Private _name As String" or "Private items(10) As Integer")
            // If we see an identifier followed by As or ( or [ (array), it's a field
            if (Check(TokenType.Identifier))
            {
                var nextType = PeekNext().Type;
                if (nextType == TokenType.As || nextType == TokenType.LeftBracket || nextType == TokenType.LeftParen)
                {
                    var token = Peek();
                    var name = Advance().Value.ToString();

                    // Parse array dimensions if present (support both [] and () syntax)
                    var arrayDimensions = new List<int>();
                    if (Match(TokenType.LeftParen))
                    {
                        // VB-style array: items(100)
                        if (Check(TokenType.IntegerLiteral))
                        {
                            arrayDimensions.Add(int.Parse(Advance().Value.ToString()));
                        }
                        Consume(TokenType.RightParen, "Expected ')' after array dimension");
                    }
                    while (Match(TokenType.LeftBracket))
                    {
                        if (Check(TokenType.IntegerLiteral))
                        {
                            arrayDimensions.Add(int.Parse(Advance().Value.ToString()));
                        }
                        Consume(TokenType.RightBracket, "Expected ']' after array dimension");
                    }

                    Consume(TokenType.As, "Expected 'As' in field declaration");

                    var field = new VariableDeclarationNode(token.Line, token.Column)
                    {
                        Name = name,
                        Type = ParseTypeReference(),
                        Access = access,
                        IsStatic = isStatic
                    };

                    // Add array dimensions to type
                    if (arrayDimensions.Count > 0)
                    {
                        field.Type.ArrayDimensions = arrayDimensions;
                    }

                    // Optional initializer
                    if (Match(TokenType.Equal))
                    {
                        field.Initializer = ParseExpression();
                    }

                    return field;
                }
            }

            throw new ParseException($"Unexpected token in class: {Peek().Type}", Peek());
        }

        private ConstructorNode ParseConstructor()
        {
            var token = Consume(TokenType.Sub, "Expected 'Sub'");
            Consume(TokenType.New, "Expected 'New'");
            var node = new ConstructorNode(token.Line, token.Column);

            // Parameters
            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        node.Parameters.Add(ParseParameter());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters");
            }

            ConsumeNewlines();

            // Check for MyBase.New() call as first statement
            if (Check(TokenType.MyBase))
            {
                Advance();  // consume MyBase
                Consume(TokenType.Dot, "Expected '.' after MyBase");
                Consume(TokenType.New, "Expected 'New' after MyBase.");
                Consume(TokenType.LeftParen, "Expected '(' after MyBase.New");

                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        node.BaseConstructorArgs.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightParen, "Expected ')' after arguments");
                ConsumeNewlines();
            }

            // Parse body
            node.Body = ParseBlock(TokenType.EndSub);
            Consume(TokenType.EndSub, "Expected 'End Sub'");

            return node;
        }

        private PropertyNode ParseProperty()
        {
            var token = Consume(TokenType.Property, "Expected 'Property'");
            var node = new PropertyNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected property name").Lexeme;

            // Property type
            if (Match(TokenType.As))
            {
                node.PropertyType = ParseTypeReference();
            }

            ConsumeNewlines();

            // Parse Get and Set blocks
            while (!Check(TokenType.EndProperty) && !IsAtEnd())
            {
                if (Check(TokenType.Get))
                {
                    Consume(TokenType.Get, "Expected 'Get'");
                    ConsumeNewlines();
                    node.Getter = ParseBlock(TokenType.EndGet);
                    Consume(TokenType.EndGet, "Expected 'End Get'");
                    ConsumeNewlines();
                }
                else if (Check(TokenType.Set))
                {
                    Consume(TokenType.Set, "Expected 'Set'");

                    // Optional setter parameter: Set(value As Type)
                    if (Match(TokenType.LeftParen))
                    {
                        node.SetterParameter = ParseParameter();
                        Consume(TokenType.RightParen, "Expected ')' after setter parameter");
                    }

                    ConsumeNewlines();
                    node.Setter = ParseBlock(TokenType.EndSet);
                    Consume(TokenType.EndSet, "Expected 'End Set'");
                    ConsumeNewlines();
                }
                else
                {
                    break;
                }
            }

            Consume(TokenType.EndProperty, "Expected 'End Property'");

            return node;
        }

        private EventDeclarationNode ParseEventDeclaration()
        {
            var token = Consume(TokenType.Event, "Expected 'Event'");
            var node = new EventDeclarationNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected event name").Lexeme;

            // Event can have delegate type: Event Click As EventHandler
            // Or inline signature: Event Click(sender As Object, args As String)
            if (Match(TokenType.As))
            {
                node.EventType = ParseTypeReference();
            }

            return node;
        }

        private OperatorDeclarationNode ParseOperatorDeclaration()
        {
            var token = Consume(TokenType.Operator, "Expected 'Operator'");
            var node = new OperatorDeclarationNode(token.Line, token.Column);
            node.IsShared = true;  // Operators are always Shared/static

            // Parse operator symbol: +, -, *, /, \, ^, &, Mod, Like, =, <>, <, >, <=, >=, And, Or, Xor, Not, IsTrue, IsFalse, CType
            if (Check(TokenType.Plus))
            {
                Advance();
                node.OperatorSymbol = "+";
            }
            else if (Check(TokenType.Minus))
            {
                Advance();
                node.OperatorSymbol = "-";
            }
            else if (Check(TokenType.Multiply))
            {
                Advance();
                node.OperatorSymbol = "*";
            }
            else if (Check(TokenType.Divide))
            {
                Advance();
                node.OperatorSymbol = "/";
            }
            else if (Check(TokenType.IntegerDivide))
            {
                Advance();
                node.OperatorSymbol = "\\";
            }
            else if (Check(TokenType.Caret))
            {
                Advance();
                node.OperatorSymbol = "^";
            }
            else if (Check(TokenType.Concatenate))
            {
                Advance();
                node.OperatorSymbol = "&";
            }
            else if (Check(TokenType.Modulo))
            {
                Advance();
                node.OperatorSymbol = "Mod";
            }
            else if (Check(TokenType.Equal))
            {
                Advance();
                node.OperatorSymbol = "=";
            }
            else if (Check(TokenType.NotEqual))
            {
                Advance();
                node.OperatorSymbol = "<>";
            }
            else if (Check(TokenType.LessThan))
            {
                Advance();
                node.OperatorSymbol = "<";
            }
            else if (Check(TokenType.GreaterThan))
            {
                Advance();
                node.OperatorSymbol = ">";
            }
            else if (Check(TokenType.LessThanOrEqual))
            {
                Advance();
                node.OperatorSymbol = "<=";
            }
            else if (Check(TokenType.GreaterThanOrEqual))
            {
                Advance();
                node.OperatorSymbol = ">=";
            }
            else if (Check(TokenType.And))
            {
                Advance();
                node.OperatorSymbol = "And";
            }
            else if (Check(TokenType.Or))
            {
                Advance();
                node.OperatorSymbol = "Or";
            }
            else if (Check(TokenType.BitwiseXor))
            {
                Advance();
                node.OperatorSymbol = "Xor";
            }
            else if (Check(TokenType.Not))
            {
                Advance();
                node.OperatorSymbol = "Not";
            }
            else if (Check(TokenType.Identifier))
            {
                // Handle Xor, Like, CType, IsTrue, IsFalse as identifiers
                var opName = Advance().Lexeme;
                if (opName == "Xor" || opName == "Like" || opName == "CType" || opName == "IsTrue" || opName == "IsFalse")
                {
                    node.OperatorSymbol = opName;
                }
                else
                {
                    throw new ParseException($"Unknown operator: {opName}", token);
                }
            }
            else
            {
                throw new ParseException($"Expected operator symbol after 'Operator'", Peek());
            }

            // Parse parameters
            Consume(TokenType.LeftParen, "Expected '(' after operator");
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var param = new ParameterNode(Peek().Line, Peek().Column);
                    param.Name = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
                    Consume(TokenType.As, "Expected 'As' after parameter name");
                    param.Type = ParseTypeReference();
                    node.Parameters.Add(param);
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after parameters");

            // Parse return type
            Consume(TokenType.As, "Expected 'As' after operator parameters");
            node.ReturnType = ParseTypeReference();
            ConsumeNewlines();

            // Parse body
            node.Body = ParseBlock(TokenType.EndOperator);
            Consume(TokenType.EndOperator, "Expected 'End Operator'");

            return node;
        }

        private InterfaceNode ParseInterface()
        {
            var token = Consume(TokenType.Interface, "Expected 'Interface'");
            var node = new InterfaceNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected interface name").Lexeme;
            ConsumeNewlines();

            while (!Check(TokenType.EndInterface) && !IsAtEnd())
            {
                if (Check(TokenType.Function))
                {
                    var method = ParseInterfaceFunction();
                    method.IsAbstract = true;
                    node.Methods.Add(method);
                }
                else if (Check(TokenType.Sub))
                {
                    var method = ParseInterfaceSub();
                    method.IsAbstract = true;
                    node.Methods.Add(method);
                }
                SkipNewlines();
            }

            Consume(TokenType.EndInterface, "Expected 'End Interface'");
            return node;
        }

        private FunctionNode ParseInterfaceFunction()
        {
            var token = Consume(TokenType.Function, "Expected 'Function'");
            var node = new FunctionNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected function name").Lexeme;

            Consume(TokenType.LeftParen, "Expected '(' after function name");
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    node.Parameters.Add(ParseParameter());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after parameters");

            if (Match(TokenType.As))
            {
                node.ReturnType = ParseTypeReference();
            }

            // Interface methods have no body
            ConsumeNewlines();
            return node;
        }

        private FunctionNode ParseInterfaceSub()
        {
            var token = Consume(TokenType.Sub, "Expected 'Sub'");
            var node = new FunctionNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected sub name").Lexeme;

            Consume(TokenType.LeftParen, "Expected '(' after sub name");
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    node.Parameters.Add(ParseParameter());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after parameters");

            node.ReturnType = new TypeReference("Void");

            // Interface methods have no body
            ConsumeNewlines();
            return node;
        }

        // ====================================================================
        // Enums
        // ====================================================================

        private EnumNode ParseEnum()
        {
            var token = Consume(TokenType.Enum, "Expected 'Enum'");
            var node = new EnumNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected enum name").Lexeme;

            // Optional underlying type: Enum Color As Integer
            if (Match(TokenType.As))
            {
                node.UnderlyingType = ParseTypeReference();
            }

            ConsumeNewlines();

            // Parse enum members
            long nextValue = 0;
            while (!Check(TokenType.EndEnum) && !IsAtEnd())
            {
                SkipNewlines();
                if (Check(TokenType.EndEnum)) break;

                var memberToken = Consume(TokenType.Identifier, "Expected enum member name");
                var member = new EnumMemberNode(memberToken.Line, memberToken.Column)
                {
                    Name = memberToken.Lexeme
                };

                // Optional explicit value: Red = 1
                if (Match(TokenType.Assignment))
                {
                    member.Value = ParseExpression();
                }

                node.Members.Add(member);
                SkipNewlines();
            }

            Consume(TokenType.EndEnum, "Expected 'End Enum'");
            return node;
        }

        // ====================================================================
        // Types and Structures
        // ====================================================================

        private TypeNode ParseType()
        {
            var token = Consume(TokenType.Type, "Expected 'Type'");
            var node = new TypeNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected type name").Lexeme;
            ConsumeNewlines();

            while (!Check(TokenType.EndType) && !IsAtEnd())
            {
                if (Check(TokenType.Identifier))
                {
                    var member = new VariableDeclarationNode(Peek().Line, Peek().Column);
                    member.Name = Consume(TokenType.Identifier, "Expected member name").Lexeme;
                    Consume(TokenType.As, "Expected 'As'");
                    member.Type = ParseTypeReference();
                    node.Members.Add(member);
                }
                SkipNewlines();
            }

            Consume(TokenType.EndType, "Expected 'End Type'");
            return node;
        }

        private StructureNode ParseStructure()
        {
            var token = Consume(TokenType.Structure, "Expected 'Structure'");
            var node = new StructureNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected structure name").Lexeme;
            ConsumeNewlines();

            while (!Check(TokenType.EndStructure) && !IsAtEnd())
            {
                if (Check(TokenType.Identifier))
                {
                    var member = new VariableDeclarationNode(Peek().Line, Peek().Column);
                    member.Name = Consume(TokenType.Identifier, "Expected member name").Lexeme;
                    Consume(TokenType.As, "Expected 'As'");
                    member.Type = ParseTypeReference();
                    node.Members.Add(member);
                }
                SkipNewlines();
            }

            Consume(TokenType.EndStructure, "Expected 'End Structure'");
            return node;
        }

        private TypeDefineNode ParseTypeDefine()
        {
            var token = Consume(TokenType.TypeDefine, "Expected 'TypeDefine'");
            var node = new TypeDefineNode(token.Line, token.Column);

            node.AliasName = Consume(TokenType.Identifier, "Expected alias name").Lexeme;
            Consume(TokenType.As, "Expected 'As'");
            node.BaseType = ParseTypeReference();

            return node;
        }

        // ====================================================================
        // Templates and Delegates
        // ====================================================================

        private TemplateDeclarationNode ParseTemplate()
        {
            var token = Consume(TokenType.Template, "Expected 'Template'");
            var node = new TemplateDeclarationNode(token.Line, token.Column);

            // Template Function or Template Class
            if (Check(TokenType.Function) || Check(TokenType.Class))
            {
                if (Check(TokenType.Function))
                {
                    Advance();
                    var func = new FunctionNode(token.Line, token.Column);
                    func.Name = Consume(TokenType.Identifier, "Expected function name").Lexeme;

                    // Generic parameters
                    if (Match(TokenType.LeftParen) && Check(TokenType.Of))
                    {
                        Consume(TokenType.Of, "Expected 'Of'");
                        do
                        {
                            node.TypeParameters.Add(Consume(TokenType.Identifier, "Expected type parameter").Lexeme);
                        } while (Match(TokenType.Comma));

                        Consume(TokenType.RightParen, "Expected ')' after generic parameters");
                    }

                    // Regular parameters
                    if (Match(TokenType.LeftParen))
                    {
                        if (!Check(TokenType.RightParen))
                        {
                            do
                            {
                                func.Parameters.Add(ParseParameter());
                            } while (Match(TokenType.Comma));
                        }
                        Consume(TokenType.RightParen, "Expected ')' after parameters");
                    }

                    // Return type
                    if (Match(TokenType.As))
                    {
                        func.ReturnType = ParseTypeReference();
                    }

                    ConsumeNewlines();
                    func.Body = ParseBlock(TokenType.EndFunction);
                    Consume(TokenType.EndFunction, "Expected 'End Function'");

                    node.Declaration = func;
                }
                else // Template Class
                {
                    node.Declaration = ParseClass();
                }
            }

            return node;
        }

        private DelegateDeclarationNode ParseDelegate()
        {
            var token = Consume(TokenType.Delegate, "Expected 'Delegate'");
            var node = new DelegateDeclarationNode(token.Line, token.Column);

            if (Match(TokenType.Function))
            {
                node.Name = Consume(TokenType.Identifier, "Expected delegate name").Lexeme;

                if (Match(TokenType.LeftParen))
                {
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            node.Parameters.Add(ParseParameter());
                        } while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RightParen, "Expected ')' after parameters");
                }

                if (Match(TokenType.As))
                {
                    node.ReturnType = ParseTypeReference();
                }
            }
            else if (Match(TokenType.Sub))
            {
                node.Name = Consume(TokenType.Identifier, "Expected delegate name").Lexeme;

                if (Match(TokenType.LeftParen))
                {
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            node.Parameters.Add(ParseParameter());
                        } while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RightParen, "Expected ')' after parameters");
                }
            }

            return node;
        }

        /// <summary>
        /// Parse an extern declaration with platform-specific implementations
        /// </summary>
        /// <remarks>
        /// Syntax:
        /// Extern Function Name(params) As ReturnType
        ///     CSharp: "implementation"
        ///     Cpp: "implementation"
        ///     LLVM: "implementation"
        ///     MSIL: "implementation"
        /// End Extern
        ///
        /// Or for Sub:
        /// Extern Sub Name(params)
        ///     CSharp: "implementation"
        /// End Extern
        /// </remarks>
        private ExternDeclarationNode ParseExtern()
        {
            var token = Consume(TokenType.Extern, "Expected 'Extern'");
            var node = new ExternDeclarationNode(token.Line, token.Column);

            if (Match(TokenType.Function))
            {
                node.IsFunction = true;
                node.Name = Consume(TokenType.Identifier, "Expected extern function name").Lexeme;

                // Parse parameters
                if (Match(TokenType.LeftParen))
                {
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            node.Parameters.Add(ParseParameter());
                        } while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RightParen, "Expected ')' after parameters");
                }

                // Parse return type
                if (Match(TokenType.As))
                {
                    node.ReturnType = ParseTypeReference();
                }
            }
            else if (Match(TokenType.Sub))
            {
                node.IsFunction = false;
                node.Name = Consume(TokenType.Identifier, "Expected extern sub name").Lexeme;

                // Parse parameters
                if (Match(TokenType.LeftParen))
                {
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            node.Parameters.Add(ParseParameter());
                        } while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RightParen, "Expected ')' after parameters");
                }
            }
            else
            {
                throw new ParseException("Expected 'Function' or 'Sub' after 'Extern'", Peek());
            }

            SkipNewlines();

            // Parse platform implementations
            // Format: PlatformName: "implementation string"
            while (!Check(TokenType.EndExtern) && !IsAtEnd())
            {
                SkipNewlines();

                if (Check(TokenType.EndExtern))
                    break;

                // Parse platform name (identifier followed by colon)
                var platformToken = Consume(TokenType.Identifier, "Expected platform name (CSharp, Cpp, LLVM, MSIL)");
                var platformName = platformToken.Lexeme;

                Consume(TokenType.Colon, $"Expected ':' after platform name '{platformName}'");

                // Parse implementation string
                var implToken = Consume(TokenType.StringLiteral, $"Expected string literal for {platformName} implementation");
                var implementation = implToken.Lexeme;

                // Remove quotes from the string literal
                if (implementation.StartsWith("\"") && implementation.EndsWith("\""))
                {
                    implementation = implementation.Substring(1, implementation.Length - 2);
                }

                node.PlatformImplementations[platformName] = implementation;

                SkipNewlines();
            }

            Consume(TokenType.EndExtern, "Expected 'End Extern'");

            return node;
        }

        private ExtensionMethodNode ParseExtensionMethod()
        {
            var token = Consume(TokenType.Extension, "Expected 'Extension'");
            var node = new ExtensionMethodNode(token.Line, token.Column);

            Consume(TokenType.Function, "Expected 'Function' after 'Extension'");

            var func = new FunctionNode(token.Line, token.Column);

            // Check for traditional syntax: Extension Function String.Reverse()
            // vs simpler syntax: Extension Function IsNullOrEmpty(s As String)
            var firstIdent = Consume(TokenType.Identifier, "Expected method name or type").Lexeme;

            if (Check(TokenType.Dot))
            {
                // Traditional syntax: TypeName.MethodName
                Advance(); // consume dot
                node.ExtendedType = firstIdent;
                func.Name = Consume(TokenType.Identifier, "Expected method name").Lexeme;
            }
            else
            {
                // Simpler syntax: MethodName(self As Type, ...)
                // The extended type comes from the first parameter
                func.Name = firstIdent;
            }

            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        func.Parameters.Add(ParseParameter());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters");
            }

            // For simpler syntax, the extended type is the first parameter's type
            if (string.IsNullOrEmpty(node.ExtendedType) && func.Parameters.Count > 0)
            {
                node.ExtendedType = func.Parameters[0].Type?.Name ?? "Object";
            }

            if (Match(TokenType.As))
            {
                func.ReturnType = ParseTypeReference();
            }

            ConsumeNewlines();
            func.Body = ParseBlock(TokenType.EndFunction);
            Consume(TokenType.EndFunction, "Expected 'End Function'");

            node.Method = func;
            return node;
        }

        private ExtensionMethodNode ParseExtensionMethodFromAttribute()
        {
            // Called after <Extension> attribute is consumed
            var token = Consume(TokenType.Function, "Expected 'Function' after <Extension>");
            var node = new ExtensionMethodNode(token.Line, token.Column);

            // Parse extended type (e.g., String.Reverse()) - can be identifier or type keyword
            if (Check(TokenType.Identifier))
            {
                node.ExtendedType = Advance().Lexeme;
            }
            else if (Check(TokenType.String) || Check(TokenType.Integer) || Check(TokenType.Double) ||
                     Check(TokenType.Boolean) || Check(TokenType.Long) || Check(TokenType.Single))
            {
                node.ExtendedType = Advance().Lexeme;
            }
            else
            {
                throw new ParseException($"Expected type name but found {Peek().Type}", Peek());
            }
            Consume(TokenType.Dot, "Expected '.' after type name");

            var func = new FunctionNode(token.Line, token.Column);
            func.Name = Consume(TokenType.Identifier, "Expected method name").Lexeme;

            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        func.Parameters.Add(ParseParameter());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters");
            }

            if (Match(TokenType.As))
            {
                func.ReturnType = ParseTypeReference();
            }

            ConsumeNewlines();
            func.Body = ParseBlock(TokenType.EndFunction);
            Consume(TokenType.EndFunction, "Expected 'End Function'");

            node.Method = func;
            return node;
        }

        // ====================================================================
        // Functions and Subroutines
        // ====================================================================

        private FunctionNode ParseFunction()
        {
            var token = Consume(TokenType.Function, "Expected 'Function'");
            var node = new FunctionNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected function name").Lexeme;

            // Generic type parameters: Function Foo(Of T, U)(...)
            if (Check(TokenType.LeftParen) && PeekNext().Type == TokenType.Of)
            {
                Advance(); // consume '('
                Consume(TokenType.Of, "Expected 'Of'");
                do
                {
                    node.GenericParameters.Add(Consume(TokenType.Identifier, "Expected type parameter").Lexeme);
                } while (Match(TokenType.Comma));
                Consume(TokenType.RightParen, "Expected ')' after generic parameters");
            }

            // Parameters
            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        node.Parameters.Add(ParseParameter());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters");
            }

            // Return type
            if (Match(TokenType.As))
            {
                node.ReturnType = ParseTypeReference();
            }

            // Implements clause
            if (Match(TokenType.Implements))
            {
                node.ImplementsInterface = Consume(TokenType.Identifier, "Expected interface name").Lexeme;
                Consume(TokenType.Dot, "Expected '.'");
                node.ImplementsInterface += "." + Consume(TokenType.Identifier, "Expected method name").Lexeme;
            }

            ConsumeNewlines();

            // Body (if not abstract)
            if (!node.IsAbstract)
            {
                node.Body = ParseBlock(TokenType.EndFunction);
                Consume(TokenType.EndFunction, "Expected 'End Function'");
            }

            return node;
        }

        private SubroutineNode ParseSubroutine()
        {
            var token = Consume(TokenType.Sub, "Expected 'Sub'");
            var node = new SubroutineNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected subroutine name").Lexeme;

            // Generic type parameters: Sub Foo(Of T, U)(...)
            if (Check(TokenType.LeftParen) && PeekNext().Type == TokenType.Of)
            {
                Advance(); // consume '('
                Consume(TokenType.Of, "Expected 'Of'");
                do
                {
                    node.GenericParameters.Add(Consume(TokenType.Identifier, "Expected type parameter").Lexeme);
                } while (Match(TokenType.Comma));
                Consume(TokenType.RightParen, "Expected ')' after generic parameters");
            }

            // Parameters
            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        node.Parameters.Add(ParseParameter());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after parameters");
            }

            // Implements clause
            if (Match(TokenType.Implements))
            {
                node.ImplementsInterface = Consume(TokenType.Identifier, "Expected interface name").Lexeme;
                Consume(TokenType.Dot, "Expected '.'");
                node.ImplementsInterface += "." + Consume(TokenType.Identifier, "Expected method name").Lexeme;
            }

            ConsumeNewlines();
            node.Body = ParseBlock(TokenType.EndSub);
            Consume(TokenType.EndSub, "Expected 'End Sub'");

            return node;
        }

        private ParameterNode ParseParameter()
        {
            var token = Peek();
            var node = new ParameterNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;

            // Check for array brackets before 'As'
            bool isArray = false;
            List<int> arrayDimensions = new List<int>();

            while (Match(TokenType.LeftBracket))
            {
                isArray = true;

                if (!Check(TokenType.RightBracket))
                {
                    var sizeExpr = ParseExpression();
                    if (sizeExpr is LiteralExpressionNode literal && literal.Value is int size)
                    {
                        arrayDimensions.Add(size);
                    }
                    else
                    {
                        arrayDimensions.Add(-1); // Dynamic size
                    }
                }
                else
                {
                    arrayDimensions.Add(-1); // No size specified
                }

                Consume(TokenType.RightBracket, "Expected ']'");
            }

            Consume(TokenType.As, "Expected 'As'");
            node.Type = ParseTypeReference();

            // If we had array brackets, mark the type as an array
            if (isArray)
            {
                node.Type.IsArray = true;
                node.Type.ArrayDimensions = arrayDimensions;
            }

            // Default value
            if (Match(TokenType.Assignment))
            {
                node.DefaultValue = ParseExpression();
            }

            return node;
        }

        // ====================================================================
        // Variable Declarations
        // ====================================================================

        private VariableDeclarationNode ParseVariableDeclaration()
        {
            var token = Consume(TokenType.Dim, "Expected 'Dim'");
            var node = new VariableDeclarationNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected variable name").Lexeme;

            // Array dimensions
            if (Match(TokenType.LeftBracket))
            {
                var arrayType = new TypeReference("Array");
                arrayType.IsArray = true;

                do
                {
                    if (!Check(TokenType.RightBracket))
                    {
                        var sizeExpr = ParseExpression();
                        if (sizeExpr is LiteralExpressionNode literal && literal.Value is int size)
                        {
                            arrayType.ArrayDimensions.Add(size);
                        }
                        else
                        {
                            arrayType.ArrayDimensions.Add(-1); // Dynamic size
                        }
                    }
                    else
                    {
                        arrayType.ArrayDimensions.Add(-1); // No size specified
                    }

                    Consume(TokenType.RightBracket, "Expected ']'");
                } while (Match(TokenType.LeftBracket));

                Consume(TokenType.As, "Expected 'As'");
                var elementType = ParseTypeReference();
                elementType.IsArray = true;
                elementType.ArrayDimensions = arrayType.ArrayDimensions;
                node.Type = elementType;
            }
            else
            {
                Consume(TokenType.As, "Expected 'As'");
                node.Type = ParseTypeReference();
            }

            // Initializer
            if (Match(TokenType.Assignment))
            {
                node.Initializer = ParseExpression();
            }

            return node;
        }

        private VariableDeclarationNode ParseAutoDeclaration()
        {
            var token = Consume(TokenType.Auto, "Expected 'Auto'");
            var node = new VariableDeclarationNode(token.Line, token.Column);
            node.IsAuto = true;

            node.Name = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
            Consume(TokenType.Assignment, "Expected '=' in auto declaration");
            node.Initializer = ParseExpression();

            return node;
        }

        private ConstantDeclarationNode ParseConstantDeclaration()
        {
            var token = Consume(TokenType.Const, "Expected 'Const'");
            var node = new ConstantDeclarationNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected constant name").Lexeme;
            Consume(TokenType.As, "Expected 'As'");
            node.Type = ParseTypeReference();
            Consume(TokenType.Assignment, "Expected '=' in constant declaration");
            node.Value = ParseExpression();

            return node;
        }

        // ====================================================================
        // Type References
        // ====================================================================

        private TypeReference ParseTypeReference()
        {
            TypeReference type;

            // Pointer type
            if (Match(TokenType.Pointer))
            {
                Consume(TokenType.To, "Expected 'To' after 'Pointer'");
                type = ParseTypeReference();
                type.IsPointer = true;
                return type;
            }

            // Base type - accept both type keywords and identifiers
            string typeName;
            if (Check(TokenType.Integer) || Check(TokenType.Long) || Check(TokenType.Single) ||
                Check(TokenType.Double) || Check(TokenType.String) || Check(TokenType.Boolean) ||
                Check(TokenType.Char))
            {
                typeName = Advance().Lexeme;
            }
            else if (Check(TokenType.Identifier))
            {
                typeName = Advance().Lexeme;
            }
            else
            {
                throw new ParseException($"Expected type name but found {Peek().Type}", Peek());
            }

            type = new TypeReference(typeName);

            // Generic arguments - only if we see '(' followed by 'Of'
            // Check both before consuming the paren
            if (Check(TokenType.LeftParen) && PeekNext().Type == TokenType.Of)
            {
                Advance();  // consume '('
                Consume(TokenType.Of, "Expected 'Of'");
                do
                {
                    type.GenericArguments.Add(ParseTypeReference());
                } while (Match(TokenType.Comma));

                Consume(TokenType.RightParen, "Expected ')' after generic arguments");
            }

            // Nullable type: Integer?
            if (Match(TokenType.QuestionMark))
            {
                type.IsNullable = true;
            }

            return type;
        }

        // ====================================================================
        // Statements
        // ====================================================================

        private BlockNode ParseBlock(TokenType endToken)
        {
            var block = new BlockNode(Peek().Line, Peek().Column);

            while (!Check(endToken) && !IsAtEnd())
            {
                SkipNewlines();

                if (Check(endToken) || IsAtEnd())
                    break;

                var statement = ParseStatement();
                if (statement != null)
                {
                    block.Statements.Add(statement);
                }

                SkipNewlines();
            }

            return block;
        }

        private StatementNode ParseStatement()
        {
            SkipNewlines();

            if (Check(TokenType.If))
                return ParseIfStatement();
            if (Check(TokenType.Select))
                return ParseSelectStatement();
            if (Check(TokenType.For))
                return ParseForLoop();
            if (Check(TokenType.While))
                return ParseWhileLoop();
            if (Check(TokenType.Do))
                return ParseDoLoop();
            if (Check(TokenType.Try))
                return ParseTryStatement();
            if (Check(TokenType.With))
                return ParseWithStatement();
            if (Check(TokenType.Throw))
                return ParseThrowStatement();
            if (Check(TokenType.Return))
                return ParseReturnStatement();
            if (Check(TokenType.Exit))
                return ParseExitStatement();
            if (Check(TokenType.Dim))
                return ParseVariableDeclaration();
            if (Check(TokenType.Auto))
                return ParseAutoDeclaration();
            if (Check(TokenType.Const))
                return ParseConstantDeclaration();
            if (Check(TokenType.Yield))
                return ParseYieldStatement();
            if (Check(TokenType.RaiseEvent))
                return ParseRaiseEventStatement();
            if (Check(TokenType.AddHandler))
                return ParseAddHandlerStatement();
            if (Check(TokenType.RemoveHandler))
                return ParseRemoveHandlerStatement();

            // Assignment or expression statement
            return ParseAssignmentOrExpressionStatement();
        }

        private YieldStatementNode ParseYieldStatement()
        {
            var token = Consume(TokenType.Yield, "Expected 'Yield'");
            var node = new YieldStatementNode(token.Line, token.Column);

            // Check for Yield Break (for exiting an iterator)
            // Note: VB.NET doesn't have Yield Break, but we support it as an extension
            if (Check(TokenType.Exit))
            {
                Advance();
                node.IsBreak = true;
            }
            else if (Check(TokenType.Return))
            {
                // Yield Return value
                Advance();
                node.Value = ParseExpression();
            }
            else if (!Check(TokenType.Newline) && !IsAtEnd())
            {
                // Yield value (without Return keyword)
                node.Value = ParseExpression();
            }

            return node;
        }

        private RaiseEventStatementNode ParseRaiseEventStatement()
        {
            var token = Consume(TokenType.RaiseEvent, "Expected 'RaiseEvent'");
            var node = new RaiseEventStatementNode(token.Line, token.Column);

            node.EventName = Consume(TokenType.Identifier, "Expected event name").Lexeme;

            // Optional arguments: RaiseEvent Click(sender, args)
            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        node.Arguments.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after event arguments");
            }

            return node;
        }

        private AddHandlerStatementNode ParseAddHandlerStatement()
        {
            var token = Consume(TokenType.AddHandler, "Expected 'AddHandler'");
            var node = new AddHandlerStatementNode(token.Line, token.Column);

            // Parse event expression: obj.Event
            node.EventExpression = ParseExpression();

            Consume(TokenType.Comma, "Expected ',' after event expression");

            // Parse handler expression: AddressOf Handler
            node.HandlerExpression = ParseExpression();

            return node;
        }

        private RemoveHandlerStatementNode ParseRemoveHandlerStatement()
        {
            var token = Consume(TokenType.RemoveHandler, "Expected 'RemoveHandler'");
            var node = new RemoveHandlerStatementNode(token.Line, token.Column);

            // Parse event expression: obj.Event
            node.EventExpression = ParseExpression();

            Consume(TokenType.Comma, "Expected ',' after event expression");

            // Parse handler expression: AddressOf Handler
            node.HandlerExpression = ParseExpression();

            return node;
        }

        private IfStatementNode ParseIfStatement()
        {
            var token = Consume(TokenType.If, "Expected 'If'");
            var node = new IfStatementNode(token.Line, token.Column);

            node.Condition = ParseExpression();

            // Check for single-line vs multi-line if
            if (Check(TokenType.Then))
            {
                Consume(TokenType.Then, "Expected 'Then'");

                // ✓ Check if this is a single-line or multi-line if BEFORE consuming newlines
                if (Check(TokenType.Newline))  // ✓ Now checks FIRST
                {
                    // Multi-line if: If condition Then [newline] ... End If
                    ConsumeNewlines();  // ✓ NOW safe to consume
                    node.ThenBlock = ParseBlock(TokenType.Else, TokenType.ElseIf, TokenType.EndIf);

                    // ✓ Handle Else and ElseIf clauses (fully implemented)
                    while (Check(TokenType.ElseIf))
                    {
                        Advance(); // consume ElseIf
                        var elseIfCondition = ParseExpression();
                        Consume(TokenType.Then, "Expected 'Then' after ElseIf condition");
                        ConsumeNewlines();
                        var elseIfBlock = ParseBlock(TokenType.Else, TokenType.ElseIf, TokenType.EndIf);

                        // Store ElseIf as nested If in the Else clause
                        var elseIfNode = new IfStatementNode(token.Line, token.Column);
                        elseIfNode.Condition = elseIfCondition;
                        elseIfNode.ThenBlock = elseIfBlock;

                        var elseBlock = new BlockNode(token.Line, token.Column);
                        elseBlock.Statements.Add(elseIfNode);
                        node.ElseBlock = elseBlock;
                        node = elseIfNode;
                    }

                    // ✓ Handle Else clause (fully implemented)
                    if (Check(TokenType.Else))
                    {
                        Advance(); // consume Else
                        ConsumeNewlines();
                        node.ElseBlock = ParseBlock(TokenType.EndIf);
                    }

                    // ✓ Consume EndIf (fully implemented)
                    Consume(TokenType.EndIf, "Expected 'End If'");
                }
                else
                {
                    // Single-line if: If condition Then statement
                    var statement = ParseStatement();
                    node.ThenBlock = new BlockNode(token.Line, token.Column);
                    node.ThenBlock.Statements.Add(statement);
                }
            }
            else
            {
                throw new ParseException("Expected 'Then' after if condition", Peek());
            }

            return node;
        }
        private BlockNode ParseBlock(params TokenType[] endTokens)
        {
            var block = new BlockNode(Peek().Line, Peek().Column);

            while (!endTokens.Any(t => Check(t)) && !IsAtEnd())
            {
                SkipNewlines();

                if (endTokens.Any(t => Check(t)) || IsAtEnd())
                    break;

                var statement = ParseStatement();
                if (statement != null)
                {
                    block.Statements.Add(statement);
                }

                SkipNewlines();
            }

            return block;
        }

        private SelectStatementNode ParseSelectStatement()
        {
            var token = Consume(TokenType.Select, "Expected 'Select'");
            Consume(TokenType.Case, "Expected 'Case' after 'Select'");
            var node = new SelectStatementNode(token.Line, token.Column);

            node.Expression = ParseExpression();
            ConsumeNewlines();

            while (Check(TokenType.Case) && !IsAtEnd())
            {
                var caseNode = ParseCaseClause();
                node.Cases.Add(caseNode);
                SkipNewlines();
            }

            Consume(TokenType.EndSelect, "Expected 'End Select'");
            return node;
        }

        private CaseClauseNode ParseCaseClause()
        {
            var token = Consume(TokenType.Case, "Expected 'Case'");
            var node = new CaseClauseNode(token.Line, token.Column);

            if (Match(TokenType.Else))
            {
                node.IsElse = true;
            }
            else
            {
                do
                {
                    node.Values.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }

            ConsumeNewlines();
            node.Body = ParseBlock(TokenType.Case, TokenType.EndSelect);

            return node;
        }

        private StatementNode ParseForLoop()
        {
            var token = Consume(TokenType.For, "Expected 'For'");

            // Check for For Each
            if (Match(TokenType.Each))
            {
                return ParseForEachLoop(token);
            }

            var node = new ForLoopNode(token.Line, token.Column);
            node.Variable = Consume(TokenType.Identifier, "Expected loop variable").Lexeme;
            Consume(TokenType.Assignment, "Expected '='");
            node.Start = ParseExpression();
            Consume(TokenType.To, "Expected 'To'");
            node.End = ParseExpression();

            if (Match(TokenType.Step))
            {
                node.Step = ParseExpression();
            }

            ConsumeNewlines();
            node.Body = ParseBlock(TokenType.Next);
            Consume(TokenType.Next, "Expected 'Next'");

            // Optional variable after Next
            if (Check(TokenType.Identifier))
            {
                Advance();
            }

            return node;
        }

        private StatementNode ParseForEachLoop(Token startToken)
        {
            var node = new ForEachLoopNode(startToken.Line, startToken.Column);

            node.Variable = Consume(TokenType.Identifier, "Expected loop variable").Lexeme;
            Consume(TokenType.As, "Expected 'As'");
            node.VariableType = ParseTypeReference();
            Consume(TokenType.In, "Expected 'In'");
            node.Collection = ParseExpression();

            ConsumeNewlines();
            node.Body = ParseBlock(TokenType.Next);
            Consume(TokenType.Next, "Expected 'Next'");

            // Optional variable after Next
            if (Check(TokenType.Identifier))
            {
                Advance();
            }

            return node;
        }

        private WhileLoopNode ParseWhileLoop()
        {
            var token = Consume(TokenType.While, "Expected 'While'");
            var node = new WhileLoopNode(token.Line, token.Column);

            node.Condition = ParseExpression();
            ConsumeNewlines();
            node.Body = ParseBlock(TokenType.Wend);
            Consume(TokenType.Wend, "Expected 'Wend'");

            return node;
        }

        private DoLoopNode ParseDoLoop()
        {
            var token = Consume(TokenType.Do, "Expected 'Do'");
            var node = new DoLoopNode(token.Line, token.Column);

            // Check for condition at start: Do While/Until <condition>
            if (Match(TokenType.While))
            {
                node.IsConditionAtStart = true;
                node.IsWhile = true;
                node.Condition = ParseExpression();
            }
            else if (Match(TokenType.Until))
            {
                node.IsConditionAtStart = true;
                node.IsWhile = false;
                node.Condition = ParseExpression();
            }

            ConsumeNewlines();
            node.Body = ParseBlock(TokenType.Loop);
            Consume(TokenType.Loop, "Expected 'Loop'");

            // Check for condition at end: Loop While/Until <condition>
            if (Match(TokenType.While))
            {
                node.IsConditionAtStart = false;
                node.IsWhile = true;
                node.Condition = ParseExpression();
            }
            else if (Match(TokenType.Until))
            {
                node.IsConditionAtStart = false;
                node.IsWhile = false;
                node.Condition = ParseExpression();
            }

            return node;
        }

        private TryStatementNode ParseTryStatement()
        {
            var token = Consume(TokenType.Try, "Expected 'Try'");
            var node = new TryStatementNode(token.Line, token.Column);

            ConsumeNewlines();
            node.TryBlock = ParseBlock(TokenType.Catch, TokenType.Finally, TokenType.EndTry);

            while (Check(TokenType.Catch))
            {
                var catchClause = ParseCatchClause();
                node.CatchClauses.Add(catchClause);
            }

            if (Check(TokenType.Finally))
            {
                Advance(); // consume Finally
                ConsumeNewlines();
                node.FinallyBlock = ParseBlock(TokenType.EndTry);
            }

            Consume(TokenType.EndTry, "Expected 'End Try'");
            return node;
        }

        private WithStatementNode ParseWithStatement()
        {
            var token = Consume(TokenType.With, "Expected 'With'");
            var node = new WithStatementNode(token.Line, token.Column);

            node.Object = ParseExpression();
            ConsumeNewlines();

            node.Body = ParseBlock(TokenType.EndWith);
            Consume(TokenType.EndWith, "Expected 'End With'");

            return node;
        }

        private CatchClauseNode ParseCatchClause()
        {
            var token = Consume(TokenType.Catch, "Expected 'Catch'");
            var node = new CatchClauseNode(token.Line, token.Column);

            // Optional exception variable
            if (Check(TokenType.Identifier))
            {
                node.ExceptionVariable = Advance().Lexeme;
                if (Check(TokenType.As))
                {
                    Advance();
                    node.ExceptionType = ParseTypeReference();
                }
            }

            ConsumeNewlines();
            node.Body = ParseBlock(TokenType.Catch, TokenType.Finally, TokenType.EndTry);

            return node;
        }

        private ThrowStatementNode ParseThrowStatement()
        {
            var token = Consume(TokenType.Throw, "Expected 'Throw'");
            var node = new ThrowStatementNode(token.Line, token.Column);

            if (!Check(TokenType.Newline) && !IsAtEnd())
            {
                node.Exception = ParseExpression();
            }

            return node;
        }

        private ReturnStatementNode ParseReturnStatement()
        {
            var token = Consume(TokenType.Return, "Expected 'Return'");
            var node = new ReturnStatementNode(token.Line, token.Column);

            if (!Check(TokenType.Newline) && !IsAtEnd())
            {
                node.Value = ParseExpression();
            }

            return node;
        }

        private ExitStatementNode ParseExitStatement()
        {
            var token = Consume(TokenType.Exit, "Expected 'Exit'");
            var node = new ExitStatementNode(token.Line, token.Column);

            // Expect the kind of exit: For, Do, While, Sub, Function
            if (Check(TokenType.For))
            {
                Advance();
                node.Kind = ExitKind.For;
            }
            else if (Check(TokenType.Do))
            {
                Advance();
                node.Kind = ExitKind.Do;
            }
            else if (Check(TokenType.While))
            {
                Advance();
                node.Kind = ExitKind.While;
            }
            else if (Check(TokenType.Sub))
            {
                Advance();
                node.Kind = ExitKind.Sub;
            }
            else if (Check(TokenType.Function))
            {
                Advance();
                node.Kind = ExitKind.Function;
            }
            else
            {
                throw new ParseException("Expected 'For', 'Do', 'While', 'Sub', or 'Function' after 'Exit'", Peek());
            }

            return node;
        }

        private StatementNode ParseAssignmentOrExpressionStatement()
        {
            // Parse the left-hand side WITHOUT treating '=' as equality
            // This is the assignment target or the start of an expression
            var target = ParseAssignmentTarget();

            // Check for assignment operators
            if (Check(TokenType.Assignment) || Check(TokenType.PlusAssign) ||
                Check(TokenType.MinusAssign) || Check(TokenType.MultiplyAssign) ||
                Check(TokenType.DivideAssign))
            {
                var token = Advance();
                var assignment = new AssignmentStatementNode(token.Line, token.Column);
                assignment.Target = target;
                assignment.Operator = token.Lexeme;
                assignment.Value = ParseExpression();
                return assignment;
            }

            // Not an assignment - continue parsing as a full expression
            // The target we parsed is just the start of the expression
            var expr = ParseExpressionContinuation(target);

            // Expression statement
            var exprStmt = new ExpressionStatementNode(expr.Line, expr.Column);
            exprStmt.Expression = expr;
            return exprStmt;
        }

        /// <summary>
        /// Parse a potential assignment target: identifier, member access, array access,
        /// or prefix increment/decrement.
        /// Does NOT consume '=' as equality.
        /// </summary>
        private ExpressionNode ParseAssignmentTarget()
        {
            // Handle prefix increment/decrement as expression statements
            if (Check(TokenType.Increment) || Check(TokenType.Decrement))
            {
                var op = Advance();
                var operand = ParsePostfix();
                var unary = new UnaryExpressionNode(op.Line, op.Column);
                unary.Operator = op.Lexeme;
                unary.Operand = operand;
                unary.IsPostfix = false;
                return unary;
            }

            return ParsePostfix();
        }

        /// <summary>
        /// Continue parsing an expression given an already-parsed left side.
        /// Used when we determined something is NOT an assignment.
        /// </summary>
        private ExpressionNode ParseExpressionContinuation(ExpressionNode left)
        {
            // Continue with binary operators if present
            return ParseBinaryExpressionContinuation(left, 0);
        }

        private ExpressionNode ParseBinaryExpressionContinuation(ExpressionNode left, int minPrecedence)
        {
            while (IsBinaryOperator(Peek()) && GetPrecedence(Peek()) >= minPrecedence)
            {
                var op = Advance();
                int prec = GetPrecedence(op);
                var right = ParseUnary();

                while (IsBinaryOperator(Peek()) && GetPrecedence(Peek()) > prec)
                {
                    right = ParseBinaryExpressionContinuation(right, GetPrecedence(Peek()));
                }

                var binary = new BinaryExpressionNode(op.Line, op.Column);
                binary.Left = left;
                binary.Operator = op.Lexeme;
                binary.Right = right;
                left = binary;
            }
            return left;
        }

        private bool IsBinaryOperator(Token token)
        {
            return token.Type switch
            {
                TokenType.Plus or TokenType.Minus or TokenType.Multiply or TokenType.Divide or
                TokenType.Modulo or TokenType.And or TokenType.Or or TokenType.BitwiseXor or
                TokenType.AndAnd or TokenType.OrOr or TokenType.Assignment or
                TokenType.Equal or TokenType.NotEqual or TokenType.LessThan or
                TokenType.LessThanOrEqual or TokenType.GreaterThan or TokenType.GreaterThanOrEqual => true,
                _ => false
            };
        }

        private int GetPrecedence(Token token)
        {
            return token.Type switch
            {
                TokenType.OrOr or TokenType.Or => 1,
                TokenType.AndAnd or TokenType.And => 2,
                TokenType.Assignment or TokenType.Equal or TokenType.NotEqual => 3,
                TokenType.LessThan or TokenType.LessThanOrEqual or
                TokenType.GreaterThan or TokenType.GreaterThanOrEqual => 4,
                TokenType.Plus or TokenType.Minus => 5,
                TokenType.Multiply or TokenType.Divide or TokenType.Modulo => 6,
                TokenType.BitwiseXor => 7,
                _ => 0
            };
        }

        // ====================================================================
        // Expressions
        // ====================================================================

        private ExpressionNode ParseExpression()
        {
            return ParseLogicalOr();
        }

        private ExpressionNode ParseLogicalOr()
        {
            var expr = ParseLogicalAnd();

            while (Check(TokenType.Or) || Check(TokenType.OrOr))
            {
                var op = Advance();
                var right = ParseLogicalAnd();
                var binary = new BinaryExpressionNode(op.Line, op.Column);
                binary.Left = expr;
                binary.Operator = op.Lexeme;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        private ExpressionNode ParseLogicalAnd()
        {
            var expr = ParseEquality();

            while (Check(TokenType.And) || Check(TokenType.AndAnd))
            {
                var op = Advance();
                var right = ParseEquality();
                var binary = new BinaryExpressionNode(op.Line, op.Column);
                binary.Left = expr;
                binary.Operator = op.Lexeme;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        private ExpressionNode ParseEquality()
        {
            var expr = ParseComparison();

            while (Check(TokenType.Equal) || Check(TokenType.NotEqual) ||
                   Check(TokenType.IsEqual) || Check(TokenType.Assignment)) // Include Assignment here
            {
                var op = Advance();
                // Normalize = to == in expression context
                if (op.Type == TokenType.Assignment)
                    op = new Token(TokenType.Equal, "=", op.Value, op.Line, op.Column);

                var right = ParseComparison();
                var binary = new BinaryExpressionNode(op.Line, op.Column);
                binary.Left = expr;
                binary.Operator = op.Lexeme;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }
        private ExpressionNode ParseComparison()
        {
            var expr = ParseAdditive();

            while (Check(TokenType.LessThan) || Check(TokenType.LessThanOrEqual) ||
                   Check(TokenType.GreaterThan) || Check(TokenType.GreaterThanOrEqual))
            {
                var op = Advance();
                var right = ParseAdditive();
                var binary = new BinaryExpressionNode(op.Line, op.Column);
                binary.Left = expr;
                binary.Operator = op.Lexeme;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        private ExpressionNode ParseAdditive()
        {
            var expr = ParseMultiplicative();

            while (Check(TokenType.Plus) || Check(TokenType.Minus) || Check(TokenType.Concatenate))
            {
                var op = Advance();
                var right = ParseMultiplicative();
                var binary = new BinaryExpressionNode(op.Line, op.Column);
                binary.Left = expr;
                binary.Operator = op.Lexeme;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        private ExpressionNode ParseMultiplicative()
        {
            var expr = ParseUnary();

            while (Check(TokenType.Multiply) || Check(TokenType.Divide) ||
                   Check(TokenType.IntegerDivide) || Check(TokenType.Modulo))
            {
                var op = Advance();
                var right = ParseUnary();
                var binary = new BinaryExpressionNode(op.Line, op.Column);
                binary.Left = expr;
                binary.Operator = op.Lexeme;
                binary.Right = right;
                expr = binary;
            }

            return expr;
        }

        private ExpressionNode ParseUnary()
        {
            // Prefix operators: Not, !, -, +
            if (Check(TokenType.Not) || Check(TokenType.Bang) ||
                Check(TokenType.Minus) || Check(TokenType.Plus))
            {
                var op = Advance();
                var operand = ParseUnary();
                var unary = new UnaryExpressionNode(op.Line, op.Column);
                unary.Operator = op.Lexeme;
                unary.Operand = operand;
                unary.IsPostfix = false;
                return unary;
            }

            // Prefix increment/decrement: ++x, --y
            if (Check(TokenType.Increment) || Check(TokenType.Decrement))
            {
                var op = Advance();
                var operand = ParseUnary();
                var unary = new UnaryExpressionNode(op.Line, op.Column);
                unary.Operator = op.Lexeme;
                unary.Operand = operand;
                unary.IsPostfix = false;
                return unary;
            }

            return ParsePostfix();
        }

        private ExpressionNode ParsePostfix()
        {
            var expr = ParsePrimary();

            while (true)
            {
                if (Match(TokenType.Increment) || Match(TokenType.Decrement))
                {
                    var op = Previous();
                    var unary = new UnaryExpressionNode(op.Line, op.Column);
                    unary.Operator = op.Lexeme;
                    unary.Operand = expr;
                    unary.IsPostfix = true;
                    expr = unary;
                }
                else if (Match(TokenType.Dot))
                {
                    // Allow identifiers and keywords as member names (e.g., obj.Property, obj.Value)
                    var memberToken = Peek();
                    string member;
                    if (Check(TokenType.Identifier))
                    {
                        member = Advance().Lexeme;
                    }
                    else if (IsKeywordUsableAsMemberName(memberToken.Type))
                    {
                        member = Advance().Lexeme;
                    }
                    else
                    {
                        throw new ParseException($"Expected member name but found {memberToken.Type}", memberToken);
                    }
                    var memberAccess = new MemberAccessExpressionNode(expr.Line, expr.Column);
                    memberAccess.Object = expr;
                    memberAccess.MemberName = member;
                    expr = memberAccess;
                }
                else if (Match(TokenType.LeftParen))
                {
                    // Check if this is generic type arguments: func(Of T)(args)
                    if (Check(TokenType.Of))
                    {
                        // Parse generic type arguments
                        Advance(); // consume 'Of'
                        var genericArgs = new List<TypeReference>();
                        do
                        {
                            genericArgs.Add(ParseTypeReference());
                        } while (Match(TokenType.Comma));
                        Consume(TokenType.RightParen, "Expected ')' after generic type arguments");

                        // Now expect the actual function arguments
                        var call = new CallExpressionNode(expr.Line, expr.Column);
                        call.Callee = expr;
                        call.GenericArguments = genericArgs;

                        if (Match(TokenType.LeftParen))
                        {
                            if (!Check(TokenType.RightParen))
                            {
                                do
                                {
                                    call.Arguments.Add(ParseExpression());
                                } while (Match(TokenType.Comma));
                            }
                            Consume(TokenType.RightParen, "Expected ')' after arguments");
                        }
                        expr = call;
                    }
                    else
                    {
                        // Regular function call
                        var call = new CallExpressionNode(expr.Line, expr.Column);
                        call.Callee = expr;

                        if (!Check(TokenType.RightParen))
                        {
                            do
                            {
                                call.Arguments.Add(ParseExpression());
                            } while (Match(TokenType.Comma));
                        }

                        Consume(TokenType.RightParen, "Expected ')' after arguments");
                        expr = call;
                    }
                }
                else if (Match(TokenType.LeftBracket))
                {
                    var arrayAccess = new ArrayAccessExpressionNode(expr.Line, expr.Column);
                    arrayAccess.Array = expr;

                    do
                    {
                        arrayAccess.Indices.Add(ParseExpression());
                        Consume(TokenType.RightBracket, "Expected ']'");
                    } while (Match(TokenType.LeftBracket));

                    expr = arrayAccess;
                }
                else if (Match(TokenType.Caret))
                {
                    // Pointer dereference
                    var unary = new UnaryExpressionNode(expr.Line, expr.Column);
                    unary.Operator = "^";
                    unary.Operand = expr;
                    unary.IsPostfix = true;
                    expr = unary;
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private ExpressionNode ParsePrimary()
        {
            // Literals
            if (Check(TokenType.IntegerLiteral) || Check(TokenType.LongLiteral) ||
                Check(TokenType.SingleLiteral) || Check(TokenType.DoubleLiteral) ||
                Check(TokenType.StringLiteral) || Check(TokenType.CharLiteral) ||
                Check(TokenType.BooleanLiteral))
            {
                var token = Advance();
                var literal = new LiteralExpressionNode(token.Line, token.Column);
                literal.Value = token.Value;
                literal.LiteralType = token.Type;
                return literal;
            }

            // Interpolated string: $"Hello {name}"
            if (Check(TokenType.InterpolatedStringLiteral))
            {
                return ParseInterpolatedString();
            }

            // New expression
            if (Match(TokenType.New))
            {
                var token = Previous();
                var newExpr = new NewExpressionNode(token.Line, token.Column);
                newExpr.Type = ParseTypeReference();

                if (Match(TokenType.LeftParen))
                {
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            newExpr.Arguments.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.RightParen, "Expected ')' after arguments");
                }

                return newExpr;
            }

            // AddressOf
            if (Match(TokenType.AddressOf))
            {
                var token = Previous();
                var unary = new UnaryExpressionNode(token.Line, token.Column);
                unary.Operator = "AddressOf";
                unary.Operand = ParsePrimary();
                unary.IsPostfix = false;
                return unary;
            }

            // Await expression: Await SomeAsyncMethod()
            if (Match(TokenType.Await))
            {
                var token = Previous();
                var awaitExpr = new AwaitExpressionNode(token.Line, token.Column);
                awaitExpr.Expression = ParseUnary();  // Parse the expression to await
                return awaitExpr;
            }

            // Lambda expression: Function(x) expr or Sub(x) statement
            if (Check(TokenType.Function) || Check(TokenType.Sub))
            {
                bool isFunction = Check(TokenType.Function);
                var token = Advance();

                // Check if this is a lambda (followed by open paren)
                if (Check(TokenType.LeftParen))
                {
                    return ParseLambdaExpression(token, isFunction);
                }
                else
                {
                    // Put the token back and let normal parsing handle it
                    _current--;
                }
            }

            // Me (this)
            if (Match(TokenType.Me))
            {
                var token = Previous();
                return new IdentifierExpressionNode(token.Line, token.Column) { Name = "Me" };
            }

            // MyBase (base class reference)
            if (Match(TokenType.MyBase))
            {
                var token = Previous();
                return new MyBaseExpressionNode(token.Line, token.Column);
            }

            // Identifier
            if (Check(TokenType.Identifier))
            {
                var token = Advance();
                return new IdentifierExpressionNode(token.Line, token.Column) { Name = token.Lexeme };
            }

            // Parenthesized expression
            if (Match(TokenType.LeftParen))
            {
                var expr = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return expr;
            }

            // Collection initializer: { 1, 2, 3 }
            if (Match(TokenType.LeftBrace))
            {
                var token = Previous();
                var node = new CollectionInitializerNode(token.Line, token.Column);

                if (!Check(TokenType.RightBrace))
                {
                    do
                    {
                        node.Elements.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightBrace, "Expected '}' after collection initializer");
                return node;
            }

            throw new ParseException($"Unexpected token in expression: {Peek().Type}", Peek());
        }

        private InterpolatedStringNode ParseInterpolatedString()
        {
            var token = Advance(); // consume the InterpolatedStringLiteral token
            var node = new InterpolatedStringNode(token.Line, token.Column);

            // The token.Value contains the raw string content like "Hello {name}, age {age}"
            string content = token.Value?.ToString() ?? "";

            // Parse the content into parts
            int i = 0;
            var currentText = new System.Text.StringBuilder();

            while (i < content.Length)
            {
                if (content[i] == '{')
                {
                    // Save any accumulated text
                    if (currentText.Length > 0)
                    {
                        node.Parts.Add(currentText.ToString());
                        currentText.Clear();
                    }

                    // Find matching closing brace
                    int braceDepth = 1;
                    int start = i + 1;
                    i++;
                    while (i < content.Length && braceDepth > 0)
                    {
                        if (content[i] == '{') braceDepth++;
                        else if (content[i] == '}') braceDepth--;
                        if (braceDepth > 0) i++;
                    }

                    // Extract the expression text
                    string exprText = content.Substring(start, i - start);
                    i++; // skip the closing brace

                    // Parse the expression
                    var exprLexer = new Lexer(exprText);
                    var exprTokens = exprLexer.Tokenize();
                    var exprParser = new Parser(exprTokens);
                    var expr = exprParser.ParseExpression();
                    node.Parts.Add(expr);
                }
                else
                {
                    currentText.Append(content[i]);
                    i++;
                }
            }

            // Add any remaining text
            if (currentText.Length > 0)
            {
                node.Parts.Add(currentText.ToString());
            }

            return node;
        }

        private LambdaExpressionNode ParseLambdaExpression(Token token, bool isFunction)
        {
            var lambda = new LambdaExpressionNode(token.Line, token.Column);
            lambda.IsFunction = isFunction;

            // Parse parameters
            Consume(TokenType.LeftParen, "Expected '(' after Function/Sub in lambda");

            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var param = new ParameterNode(Peek().Line, Peek().Column);
                    param.Name = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;

                    if (Match(TokenType.As))
                    {
                        param.Type = ParseTypeReference();
                    }

                    lambda.Parameters.Add(param);
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expected ')' after lambda parameters");

            // Optional return type for Function lambdas
            if (isFunction && Match(TokenType.As))
            {
                lambda.ReturnType = ParseTypeReference();
            }

            // Parse body - expression lambda (single expression on same line)
            lambda.Body = ParseExpression();

            return lambda;
        }

        // ====================================================================
        // Utility Methods
        // ====================================================================

        private bool IsKeywordUsableAsMemberName(TokenType type)
        {
            // Keywords that can be used as member names after a dot
            // Only include tokens that actually exist in TokenType
            return type == TokenType.Property ||
                   type == TokenType.Type ||
                   type == TokenType.New ||
                   type == TokenType.Get ||
                   type == TokenType.Set ||
                   type == TokenType.Me ||
                   type == TokenType.Len ||
                   type == TokenType.String ||
                   type == TokenType.Integer ||
                   type == TokenType.Double ||
                   type == TokenType.Boolean;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        private Token Peek()
        {
            return _tokens[_current];
        }

        private Token PeekNext()
        {
            if (_current + 1 >= _tokens.Count) return _tokens[_current];
            return _tokens[_current + 1];
        }

        private Token Previous()
        {
            return _tokens[_current - 1];
        }

        private bool IsAtEnd()
        {
            return _current >= _tokens.Count || Peek().Type == TokenType.EOF;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw new ParseException(message + $" but found {Peek().Type}", Peek());
        }

        private void SkipNewlines()
        {
            while (Check(TokenType.Newline) && !IsAtEnd())
            {
                Advance();
            }
        }

        private void ConsumeNewlines()
        {
            if (!Check(TokenType.Newline) && !IsAtEnd())
            {
                // Optional newline consumption
                return;
            }

            while (Check(TokenType.Newline))
            {
                Advance();
            }
        }
    }

    /// <summary>
    /// Exception thrown during parsing
    /// </summary>
    public class ParseException : Exception
    {
        public Token Token { get; }

        public ParseException(string message, Token token) : base(message)
        {
            Token = token;
        }

        public override string ToString()
        {
            return $"Parse error at line {Token.Line}, column {Token.Column}: {Message}";
        }
    }
}