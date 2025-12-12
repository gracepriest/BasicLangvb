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
            if (Check(TokenType.Function))
                return ParseFunction();
            if (Check(TokenType.Sub))
                return ParseSubroutine();
            if (Check(TokenType.Dim))
                return ParseVariableDeclaration();
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

            if (Check(TokenType.Function))
            {
                var func = ParseFunction();
                func.Access = access;
                return func;
            }

            if (Check(TokenType.Sub))
            {
                var sub = ParseSubroutine();
                sub.Access = access;
                return sub;
            }

            if (Check(TokenType.Dim))
            {
                var var = ParseVariableDeclaration();
                var.Access = access;
                return var;
            }

            throw new ParseException($"Unexpected token in class: {Peek().Type}", Peek());
        }

        private InterfaceNode ParseInterface()
        {
            var token = Consume(TokenType.Interface, "Expected 'Interface'");
            var node = new InterfaceNode(token.Line, token.Column);

            node.Name = Consume(TokenType.Identifier, "Expected interface name").Lexeme;
            ConsumeNewlines();

            while (!Check(TokenType.EndInterface) && !IsAtEnd())
            {
                if (Check(TokenType.Function) || Check(TokenType.Sub))
                {
                    var method = ParseFunction();
                    method.IsAbstract = true;
                    node.Methods.Add(method);
                }
                SkipNewlines();
            }

            Consume(TokenType.EndInterface, "Expected 'End Interface'");
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

        private ExtensionMethodNode ParseExtensionMethod()
        {
            var token = Consume(TokenType.Extension, "Expected 'Extension'");
            var node = new ExtensionMethodNode(token.Line, token.Column);

            Consume(TokenType.Function, "Expected 'Function' after 'Extension'");

            // Parse extended type (e.g., String.Reverse())
            node.ExtendedType = Consume(TokenType.Identifier, "Expected type name").Lexeme;
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

            // Generic arguments
            if (Match(TokenType.LeftParen) && Check(TokenType.Of))
            {
                Consume(TokenType.Of, "Expected 'Of'");
                do
                {
                    type.GenericArguments.Add(ParseTypeReference());
                } while (Match(TokenType.Comma));

                Consume(TokenType.RightParen, "Expected ')' after generic arguments");
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
            if (Check(TokenType.Return))
                return ParseReturnStatement();
            if (Check(TokenType.Dim))
                return ParseVariableDeclaration();
            if (Check(TokenType.Auto))
                return ParseAutoDeclaration();
            if (Check(TokenType.Const))
                return ParseConstantDeclaration();

            // Assignment or expression statement
            return ParseAssignmentOrExpressionStatement();
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

            ConsumeNewlines();
            node.Body = ParseBlock(TokenType.Loop);
            Consume(TokenType.Loop, "Expected 'Loop'");

            if (Match(TokenType.While))
            {
                node.IsWhile = true;
                node.Condition = ParseExpression();
            }

            return node;
        }

        private TryStatementNode ParseTryStatement()
        {
            var token = Consume(TokenType.Try, "Expected 'Try'");
            var node = new TryStatementNode(token.Line, token.Column);

            ConsumeNewlines();
            node.TryBlock = ParseBlock(TokenType.Catch, TokenType.EndTry);

            while (Check(TokenType.Catch))
            {
                var catchClause = ParseCatchClause();
                node.CatchClauses.Add(catchClause);
            }

            Consume(TokenType.EndTry, "Expected 'End Try'");
            return node;
        }

        private CatchClauseNode ParseCatchClause()
        {
            var token = Consume(TokenType.Catch, "Expected 'Catch'");
            var node = new CatchClauseNode(token.Line, token.Column);

            node.ExceptionVariable = Consume(TokenType.Identifier, "Expected exception variable").Lexeme;
            Consume(TokenType.As, "Expected 'As'");
            node.ExceptionType = ParseTypeReference();

            ConsumeNewlines();
            node.Body = ParseBlock(TokenType.Catch, TokenType.EndTry);

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

        private StatementNode ParseAssignmentOrExpressionStatement()
        {
            var expr = ParseExpression();

            // Check for assignment operators
            if (Check(TokenType.Assignment) || Check(TokenType.PlusAssign) ||
                Check(TokenType.MinusAssign) || Check(TokenType.MultiplyAssign) ||
                Check(TokenType.DivideAssign))
            {
                var token = Advance();
                var assignment = new AssignmentStatementNode(token.Line, token.Column);
                assignment.Target = expr;
                assignment.Operator = token.Lexeme;
                assignment.Value = ParseExpression();
                return assignment;
            }

            // Expression statement
            var exprStmt = new ExpressionStatementNode(expr.Line, expr.Column);
            exprStmt.Expression = expr;
            return exprStmt;
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
                    var member = Consume(TokenType.Identifier, "Expected member name").Lexeme;
                    var memberAccess = new MemberAccessExpressionNode(expr.Line, expr.Column);
                    memberAccess.Object = expr;
                    memberAccess.MemberName = member;
                    expr = memberAccess;
                }
                else if (Match(TokenType.LeftParen))
                {
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

            // Me (this)
            if (Match(TokenType.Me))
            {
                var token = Previous();
                return new IdentifierExpressionNode(token.Line, token.Column) { Name = "Me" };
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

            // Array initializer
            if (Match(TokenType.LeftBrace))
            {
                var token = Previous();
                var call = new CallExpressionNode(token.Line, token.Column);
                call.Callee = new IdentifierExpressionNode(token.Line, token.Column) { Name = "ArrayInit" };

                if (!Check(TokenType.RightBrace))
                {
                    do
                    {
                        call.Arguments.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightBrace, "Expected '}' after array initializer");
                return call;
            }

            throw new ParseException($"Unexpected token in expression: {Peek().Type}", Peek());
        }

        // ====================================================================
        // Utility Methods
        // ====================================================================

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