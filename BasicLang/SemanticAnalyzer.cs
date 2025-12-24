using System;
using System.Collections.Generic;
using System.Linq;
using BasicLang.Compiler.AST;

namespace BasicLang.Compiler.SemanticAnalysis
{
    /// <summary>
    /// Semantic analyzer for BasicLang - performs type checking and scope resolution
    /// </summary>
    public class SemanticAnalyzer : IASTVisitor
    {
        private readonly TypeManager _typeManager;
        private Scope _currentScope;
        private readonly List<SemanticError> _errors;
        private readonly Dictionary<ASTNode, TypeInfo> _nodeTypes;
        private readonly Dictionary<ASTNode, Symbol> _nodeSymbols;

        public List<SemanticError> Errors => _errors;
        public Scope GlobalScope { get; private set; }

        public SemanticAnalyzer()
        {
            _typeManager = new TypeManager();
            _errors = new List<SemanticError>();
            _nodeTypes = new Dictionary<ASTNode, TypeInfo>();
            _nodeSymbols = new Dictionary<ASTNode, Symbol>();
        }

        public bool Analyze(ProgramNode program)
        {
            _errors.Clear();
            _nodeTypes.Clear();
            _nodeSymbols.Clear();

            GlobalScope = new Scope("Global", ScopeKind.Global, null);
            _currentScope = GlobalScope;

            // Register standard library functions
            RegisterStdLibFunctions();

            try
            {
                program.Accept(this);
                return _errors.Count == 0;
            }
            catch (Exception ex)
            {
                _errors.Add(new SemanticError($"Internal error: {ex.Message}", 0, 0));
                return false;
            }
        }

        public TypeInfo GetNodeType(ASTNode node)
        {
            _nodeTypes.TryGetValue(node, out var type);
            return type;
        }

        public Symbol GetNodeSymbol(ASTNode node)
        {
            _nodeSymbols.TryGetValue(node, out var symbol);
            return symbol;
        }

        private void SetNodeType(ASTNode node, TypeInfo type)
        {
            _nodeTypes[node] = type;
        }

        private void SetNodeSymbol(ASTNode node, Symbol symbol)
        {
            _nodeSymbols[node] = symbol;
        }

        /// <summary>
        /// Register standard library functions in the global scope
        /// </summary>
        private void RegisterStdLibFunctions()
        {
            // I/O Functions
            RegisterStdLibFunction("Print", SymbolKind.Subroutine, _typeManager.GetType("Void"),
                new[] { ("value", _typeManager.GetType("Object")) });

            RegisterStdLibFunction("PrintLine", SymbolKind.Subroutine, _typeManager.GetType("Void"),
                new[] { ("value", _typeManager.GetType("Object")) });

            RegisterStdLibFunction("Input", SymbolKind.Function, _typeManager.GetType("String"),
                new[] { ("prompt", _typeManager.GetType("String")) });

            RegisterStdLibFunction("ReadLine", SymbolKind.Function, _typeManager.GetType("String"),
                Array.Empty<(string, TypeInfo)>());

            // String Functions
            RegisterStdLibFunction("Len", SymbolKind.Function, _typeManager.GetType("Integer"),
                new[] { ("str", _typeManager.GetType("String")) });

            RegisterStdLibFunction("Mid", SymbolKind.Function, _typeManager.GetType("String"),
                new[] { ("str", _typeManager.GetType("String")), ("start", _typeManager.GetType("Integer")), ("length", _typeManager.GetType("Integer")) });

            RegisterStdLibFunction("Left", SymbolKind.Function, _typeManager.GetType("String"),
                new[] { ("str", _typeManager.GetType("String")), ("length", _typeManager.GetType("Integer")) });

            RegisterStdLibFunction("Right", SymbolKind.Function, _typeManager.GetType("String"),
                new[] { ("str", _typeManager.GetType("String")), ("length", _typeManager.GetType("Integer")) });

            RegisterStdLibFunction("UCase", SymbolKind.Function, _typeManager.GetType("String"),
                new[] { ("str", _typeManager.GetType("String")) });

            RegisterStdLibFunction("LCase", SymbolKind.Function, _typeManager.GetType("String"),
                new[] { ("str", _typeManager.GetType("String")) });

            RegisterStdLibFunction("Trim", SymbolKind.Function, _typeManager.GetType("String"),
                new[] { ("str", _typeManager.GetType("String")) });

            RegisterStdLibFunction("InStr", SymbolKind.Function, _typeManager.GetType("Integer"),
                new[] { ("str", _typeManager.GetType("String")), ("search", _typeManager.GetType("String")) });

            RegisterStdLibFunction("Replace", SymbolKind.Function, _typeManager.GetType("String"),
                new[] { ("str", _typeManager.GetType("String")), ("find", _typeManager.GetType("String")), ("replaceWith", _typeManager.GetType("String")) });

            // Math Functions
            RegisterStdLibFunction("Abs", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Sqrt", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Pow", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("baseVal", _typeManager.GetType("Double")), ("exponent", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Sin", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Cos", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Tan", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Log", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Exp", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Floor", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Ceiling", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Round", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Min", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("a", _typeManager.GetType("Double")), ("b", _typeManager.GetType("Double")) });

            RegisterStdLibFunction("Max", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("a", _typeManager.GetType("Double")), ("b", _typeManager.GetType("Double")) });

            // Random Functions
            RegisterStdLibFunction("Rnd", SymbolKind.Function, _typeManager.GetType("Double"),
                Array.Empty<(string, TypeInfo)>());

            RegisterStdLibFunction("Randomize", SymbolKind.Subroutine, _typeManager.VoidType,
                Array.Empty<(string, TypeInfo)>());

            // Conversion Functions
            RegisterStdLibFunction("CInt", SymbolKind.Function, _typeManager.GetType("Integer"),
                new[] { ("value", _typeManager.GetType("Object")) });

            RegisterStdLibFunction("CLng", SymbolKind.Function, _typeManager.GetType("Long"),
                new[] { ("value", _typeManager.GetType("Object")) });

            RegisterStdLibFunction("CDbl", SymbolKind.Function, _typeManager.GetType("Double"),
                new[] { ("value", _typeManager.GetType("Object")) });

            RegisterStdLibFunction("CSng", SymbolKind.Function, _typeManager.GetType("Single"),
                new[] { ("value", _typeManager.GetType("Object")) });

            RegisterStdLibFunction("CStr", SymbolKind.Function, _typeManager.GetType("String"),
                new[] { ("value", _typeManager.GetType("Object")) });

            RegisterStdLibFunction("CBool", SymbolKind.Function, _typeManager.GetType("Boolean"),
                new[] { ("value", _typeManager.GetType("Object")) });

            // Array Functions
            RegisterStdLibFunction("UBound", SymbolKind.Function, _typeManager.GetType("Integer"),
                new[] { ("array", _typeManager.GetType("Object")) });

            RegisterStdLibFunction("LBound", SymbolKind.Function, _typeManager.GetType("Integer"),
                new[] { ("array", _typeManager.GetType("Object")) });
        }

        private void RegisterStdLibFunction(string name, SymbolKind kind, TypeInfo returnType, (string name, TypeInfo type)[] parameters)
        {
            var symbol = new Symbol(name, kind, returnType, 0, 0)
            {
                ReturnType = returnType,
                IsDefined = true
            };

            foreach (var param in parameters)
            {
                symbol.Parameters.Add(new Symbol(param.name, SymbolKind.Parameter, param.type, 0, 0));
            }

            GlobalScope.Define(symbol);
        }

        private void Error(string message, int line, int column)
        {
            _errors.Add(new SemanticError(message, line, column));
        }

        private void Warning(string message, int line, int column)
        {
            _errors.Add(new SemanticError(message, line, column, ErrorSeverity.Warning));
        }

        private Scope EnterScope(string name, ScopeKind kind)
        {
            var newScope = new Scope(name, kind, _currentScope);
            _currentScope = newScope;
            return newScope;
        }

        private void ExitScope()
        {
            if (_currentScope.Parent != null)
            {
                _currentScope = _currentScope.Parent;
            }
        }

        private TypeInfo ResolveTypeReference(TypeReference typeRef)
        {
            if (typeRef == null)
                return _typeManager.VoidType;

            // Handle pointer types
            if (typeRef.IsPointer)
            {
                var baseType = ResolveTypeReference(new TypeReference(typeRef.Name));
                return _typeManager.CreatePointerType(baseType);
            }

            // Handle array types
            if (typeRef.IsArray)
            {
                var elementType = _typeManager.GetType(typeRef.Name);
                if (elementType == null)
                {
                    Error($"Unknown type '{typeRef.Name}'", 0, 0);
                    return _typeManager.ObjectType;
                }
                // Get the array size from the first dimension (for 1D arrays)
                int arraySize = typeRef.ArrayDimensions.Count > 0 ? typeRef.ArrayDimensions[0] : 0;
                return _typeManager.CreateArrayType(elementType, typeRef.ArrayDimensions.Count, arraySize);
            }

            // Handle generic types
            if (typeRef.GenericArguments.Count > 0)
            {
                var typeArgs = typeRef.GenericArguments
                    .Select(t => ResolveTypeReference(t))
                    .ToList();
                return _typeManager.CreateGenericType(typeRef.Name, typeArgs);
            }

            // Regular type lookup
            var type = _typeManager.GetType(typeRef.Name);
            if (type == null)
            {
                Error($"Unknown type '{typeRef.Name}'", 0, 0);
                return _typeManager.ObjectType;
            }

            // Handle nullable types
            if (typeRef.IsNullable)
            {
                var nullableType = new TypeInfo($"{type.Name}?", TypeKind.Nullable);
                nullableType.IsNullable = true;
                nullableType.UnderlyingType = type;
                return nullableType;
            }

            return type;
        }

        // ====================================================================
        // Program Structure
        // ====================================================================

        public void Visit(ProgramNode node)
        {
            foreach (var declaration in node.Declarations)
            {
                declaration.Accept(this);
            }
        }

        // ====================================================================
        // Declarations
        // ====================================================================

        public void Visit(NamespaceNode node)
        {
            EnterScope(node.Name, ScopeKind.Namespace);

            var symbol = new Symbol(node.Name, SymbolKind.Namespace, null, node.Line, node.Column);
            _currentScope.Parent.Define(symbol);

            foreach (var member in node.Members)
            {
                member.Accept(this);
            }

            ExitScope();
        }

        public void Visit(ModuleNode node)
        {
            EnterScope(node.Name, ScopeKind.Module);

            var symbol = new Symbol(node.Name, SymbolKind.Module, null, node.Line, node.Column);
            _currentScope.Parent.Define(symbol);

            foreach (var member in node.Members)
            {
                member.Accept(this);
            }

            ExitScope();
        }

        public void Visit(ClassNode node)
        {
            // Define the class type
            var classType = _typeManager.DefineType(node.Name, TypeKind.Class);
            if (classType == null)
            {
                Error($"Class '{node.Name}' is already defined", node.Line, node.Column);
                return;
            }

            // Set base class
            if (node.BaseClass != null)
            {
                var baseType = _typeManager.GetType(node.BaseClass);
                if (baseType == null)
                {
                    Error($"Unknown base class '{node.BaseClass}'", node.Line, node.Column);
                }
                else if (baseType.Kind != TypeKind.Class)
                {
                    Error($"'{node.BaseClass}' is not a class", node.Line, node.Column);
                }
                else
                {
                    classType.BaseType = baseType;
                }
            }

            // Add interfaces
            foreach (var interfaceName in node.Interfaces)
            {
                var interfaceType = _typeManager.GetType(interfaceName);
                if (interfaceType == null)
                {
                    Error($"Unknown interface '{interfaceName}'", node.Line, node.Column);
                }
                else if (interfaceType.Kind != TypeKind.Interface)
                {
                    Error($"'{interfaceName}' is not an interface", node.Line, node.Column);
                }
                else
                {
                    classType.Interfaces.Add(interfaceType);
                }
            }

            // Define class symbol
            var symbol = new Symbol(node.Name, SymbolKind.Class, classType, node.Line, node.Column);
            if (!_currentScope.Define(symbol))
            {
                Error($"Symbol '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }

            // Enter class scope
            var classScope = EnterScope(node.Name, ScopeKind.Class);
            classScope.ClassType = classType;

            // Process members
            foreach (var member in node.Members)
            {
                member.Accept(this);

                // Add member to class type
                if (member is FunctionNode func && _nodeSymbols.TryGetValue(func, out var funcSymbol))
                {
                    classType.Members[func.Name] = funcSymbol;
                }
                else if (member is SubroutineNode sub && _nodeSymbols.TryGetValue(sub, out var subSymbol))
                {
                    classType.Members[sub.Name] = subSymbol;
                }
                else if (member is VariableDeclarationNode varDecl && _nodeSymbols.TryGetValue(varDecl, out var varSymbol))
                {
                    classType.Members[varDecl.Name] = varSymbol;
                }
            }

            ExitScope();
        }

        public void Visit(InterfaceNode node)
        {
            var interfaceType = _typeManager.DefineType(node.Name, TypeKind.Interface);
            if (interfaceType == null)
            {
                Error($"Interface '{node.Name}' is already defined", node.Line, node.Column);
                return;
            }

            var symbol = new Symbol(node.Name, SymbolKind.Interface, interfaceType, node.Line, node.Column);
            if (!_currentScope.Define(symbol))
            {
                Error($"Symbol '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }

            EnterScope(node.Name, ScopeKind.Interface);

            foreach (var method in node.Methods)
            {
                method.Accept(this);
            }

            ExitScope();
        }

        public void Visit(TypeNode node)
        {
            var type = _typeManager.DefineType(node.Name, TypeKind.UserDefinedType);
            if (type == null)
            {
                Error($"Type '{node.Name}' is already defined", node.Line, node.Column);
                return;
            }

            var symbol = new Symbol(node.Name, SymbolKind.Type, type, node.Line, node.Column);
            if (!_currentScope.Define(symbol))
            {
                Error($"Symbol '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }

            // Process members
            foreach (var member in node.Members)
            {
                var memberType = ResolveTypeReference(member.Type);
                var memberSymbol = new Symbol(member.Name, SymbolKind.Variable, memberType, member.Line, member.Column);
                type.Members[member.Name] = memberSymbol;
            }
        }

        public void Visit(StructureNode node)
        {
            var type = _typeManager.DefineType(node.Name, TypeKind.Structure);
            if (type == null)
            {
                Error($"Structure '{node.Name}' is already defined", node.Line, node.Column);
                return;
            }

            var symbol = new Symbol(node.Name, SymbolKind.Structure, type, node.Line, node.Column);
            if (!_currentScope.Define(symbol))
            {
                Error($"Symbol '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }

            // Process members
            foreach (var member in node.Members)
            {
                var memberType = ResolveTypeReference(member.Type);
                var memberSymbol = new Symbol(member.Name, SymbolKind.Variable, memberType, member.Line, member.Column);
                type.Members[member.Name] = memberSymbol;
            }
        }

        public void Visit(FunctionNode node)
        {
            var returnType = ResolveTypeReference(node.ReturnType);

            var symbol = new Symbol(node.Name, SymbolKind.Function, returnType, node.Line, node.Column);
            symbol.ReturnType = returnType;
            symbol.Access = node.Access;

            if (!_currentScope.Define(symbol))
            {
                Error($"Function '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }

            SetNodeSymbol(node, symbol);
            SetNodeType(node, returnType);

            // Enter function scope
            var functionScope = EnterScope(node.Name, ScopeKind.Function);
            functionScope.ReturnType = returnType;

            // Process parameters
            foreach (var param in node.Parameters)
            {
                param.Accept(this);
                if (_nodeSymbols.TryGetValue(param, out var paramSymbol))
                {
                    symbol.Parameters.Add(paramSymbol);
                }
            }

            // Process body
            if (node.Body != null && !node.IsAbstract)
            {
                node.Body.Accept(this);
            }

            ExitScope();
        }

        public void Visit(SubroutineNode node)
        {
            var symbol = new Symbol(node.Name, SymbolKind.Subroutine, _typeManager.VoidType, node.Line, node.Column);
            symbol.ReturnType = _typeManager.VoidType;
            symbol.Access = node.Access;

            if (!_currentScope.Define(symbol))
            {
                Error($"Subroutine '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }

            SetNodeSymbol(node, symbol);

            // Enter subroutine scope
            var subScope = EnterScope(node.Name, ScopeKind.Subroutine);
            subScope.ReturnType = _typeManager.VoidType;

            // Process parameters
            foreach (var param in node.Parameters)
            {
                param.Accept(this);
                if (_nodeSymbols.TryGetValue(param, out var paramSymbol))
                {
                    symbol.Parameters.Add(paramSymbol);
                }
            }

            // Process body
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            ExitScope();
        }

        public void Visit(ParameterNode node)
        {
            var paramType = ResolveTypeReference(node.Type);
            var symbol = new Symbol(node.Name, SymbolKind.Parameter, paramType, node.Line, node.Column);

            if (!_currentScope.Define(symbol))
            {
                Error($"Parameter '{node.Name}' is already defined", node.Line, node.Column);
            }

            SetNodeSymbol(node, symbol);
            SetNodeType(node, paramType);

            // Check default value type
            if (node.DefaultValue != null)
            {
                node.DefaultValue.Accept(this);
                var defaultType = GetNodeType(node.DefaultValue);

                if (!paramType.IsAssignableFrom(defaultType))
                {
                    Error($"Default value type '{defaultType}' is not compatible with parameter type '{paramType}'",
                          node.Line, node.Column);
                }
            }
        }

        public void Visit(VariableDeclarationNode node)
        {
            TypeInfo varType;

            if (node.IsAuto)
            {
                // Type inference from initializer
                if (node.Initializer == null)
                {
                    Error($"Auto variable '{node.Name}' must have an initializer", node.Line, node.Column);
                    varType = _typeManager.ObjectType;
                }
                else
                {
                    node.Initializer.Accept(this);
                    varType = GetNodeType(node.Initializer);
                    if (varType == null)
                    {
                        Error($"Cannot infer type for variable '{node.Name}'", node.Line, node.Column);
                        varType = _typeManager.ObjectType;
                    }
                }
            }
            else
            {
                varType = ResolveTypeReference(node.Type);
            }

            var symbol = new Symbol(node.Name, SymbolKind.Variable, varType, node.Line, node.Column);
            symbol.Access = node.Access;

            if (!_currentScope.Define(symbol))
            {
                Error($"Variable '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }

            SetNodeSymbol(node, symbol);
            SetNodeType(node, varType);

            // Check initializer type
            if (node.Initializer != null && !node.IsAuto)
            {
                node.Initializer.Accept(this);
                var initType = GetNodeType(node.Initializer);

                if (!varType.IsAssignableFrom(initType))
                {
                    Error($"Cannot assign value of type '{initType}' to variable of type '{varType}'",
                          node.Line, node.Column);
                }
            }
        }

        public void Visit(ConstantDeclarationNode node)
        {
            var constType = ResolveTypeReference(node.Type);

            if (node.Value == null)
            {
                Error($"Constant '{node.Name}' must have a value", node.Line, node.Column);
            }
            else
            {
                node.Value.Accept(this);
                var valueType = GetNodeType(node.Value);

                if (!constType.IsAssignableFrom(valueType))
                {
                    Error($"Constant value type '{valueType}' is not compatible with declared type '{constType}'",
                          node.Line, node.Column);
                }
            }

            var symbol = new Symbol(node.Name, SymbolKind.Constant, constType, node.Line, node.Column);
            symbol.IsConstant = true;

            if (!_currentScope.Define(symbol))
            {
                Error($"Constant '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }

            SetNodeSymbol(node, symbol);
            SetNodeType(node, constType);
        }

        public void Visit(TypeDefineNode node)
        {
            var baseType = ResolveTypeReference(node.BaseType);

            // Create alias
            var aliasType = new TypeInfo(node.AliasName, baseType.Kind)
            {
                BaseType = baseType
            };

            if (_typeManager.DefineType(node.AliasName, baseType.Kind) == null)
            {
                Error($"Type alias '{node.AliasName}' is already defined", node.Line, node.Column);
            }
        }

        public void Visit(TemplateDeclarationNode node)
        {
            // TODO: Implement generic type parameters
            if (node.Declaration != null)
            {
                node.Declaration.Accept(this);
            }
        }

        public void Visit(DelegateDeclarationNode node)
        {
            var delegateType = _typeManager.DefineType(node.Name, TypeKind.Delegate);
            if (delegateType == null)
            {
                Error($"Delegate '{node.Name}' is already defined", node.Line, node.Column);
                return;
            }

            var returnType = ResolveTypeReference(node.ReturnType);
            var symbol = new Symbol(node.Name, SymbolKind.Class, delegateType, node.Line, node.Column);
            symbol.ReturnType = returnType;

            foreach (var param in node.Parameters)
            {
                param.Accept(this);
                if (_nodeSymbols.TryGetValue(param, out var paramSymbol))
                {
                    symbol.Parameters.Add(paramSymbol);
                }
            }

            if (!_currentScope.Define(symbol))
            {
                Error($"Delegate '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }
        }

        public void Visit(ExtensionMethodNode node)
        {
            // Verify extended type exists
            var extendedType = _typeManager.GetType(node.ExtendedType);
            if (extendedType == null)
            {
                Error($"Cannot extend unknown type '{node.ExtendedType}'", node.Line, node.Column);
            }

            if (node.Method != null)
            {
                node.Method.Accept(this);
            }
        }

        public void Visit(ExternDeclarationNode node)
        {
            // Validate extern declaration
            if (string.IsNullOrEmpty(node.Name))
            {
                Error("Extern declaration must have a name", node.Line, node.Column);
                return;
            }

            // Validate parameters
            foreach (var param in node.Parameters)
            {
                param.Accept(this);
            }

            // Determine return type
            var returnType = _typeManager.GetType("Void");
            if (node.IsFunction && node.ReturnType != null)
            {
                returnType = _typeManager.GetType(node.ReturnType.Name);
                if (returnType == null)
                {
                    Error($"Unknown return type '{node.ReturnType.Name}' for extern '{node.Name}'", node.Line, node.Column);
                    returnType = _typeManager.GetType("Void");
                }
            }

            // Register extern as a function/sub in the symbol table
            var symbolKind = node.IsFunction ? SymbolKind.Function : SymbolKind.Subroutine;
            var externSymbol = new Symbol(node.Name, symbolKind, returnType, node.Line, node.Column)
            {
                ReturnType = returnType,
                IsExtern = true,
                ExternImplementations = new Dictionary<string, string>(node.PlatformImplementations)
            };

            // Add parameters to the symbol
            foreach (var param in node.Parameters)
            {
                var paramType = _typeManager.GetType(param.Type?.Name ?? "Object");
                var paramSymbol = new Symbol(param.Name, SymbolKind.Parameter, paramType, param.Line, param.Column);
                externSymbol.Parameters.Add(paramSymbol);
            }

            if (!_currentScope.Define(externSymbol))
            {
                Error($"Extern '{node.Name}' is already defined in this scope", node.Line, node.Column);
            }
        }

        public void Visit(UsingDirectiveNode node)
        {
            // Nothing to do - just for reference
        }

        public void Visit(ImportDirectiveNode node)
        {
            // Nothing to do - just for reference
        }

        public void Visit(ConstructorNode node)
        {
            // Enter constructor scope
            EnterScope("Constructor", ScopeKind.Function);

            // Register parameters
            foreach (var param in node.Parameters)
            {
                param.Accept(this);
            }

            // Validate base constructor call if present
            if (node.BaseConstructorArgs.Count > 0)
            {
                foreach (var arg in node.BaseConstructorArgs)
                {
                    arg.Accept(this);
                }
                // TODO: Validate base constructor exists and arguments match
            }

            // Analyze body
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            ExitScope();
        }

        public void Visit(PropertyNode node)
        {
            // Validate property type
            TypeInfo propertyType = null;
            if (node.PropertyType != null)
            {
                propertyType = _typeManager.GetType(node.PropertyType.Name);
                if (propertyType == null)
                {
                    Error($"Unknown property type '{node.PropertyType.Name}'", node.Line, node.Column);
                }
            }

            // Analyze getter
            if (node.Getter != null)
            {
                EnterScope($"get_{node.Name}", ScopeKind.Function);
                node.Getter.Accept(this);
                ExitScope();
            }

            // Analyze setter
            if (node.Setter != null)
            {
                EnterScope($"set_{node.Name}", ScopeKind.Function);

                // Register setter parameter (value)
                if (node.SetterParameter != null)
                {
                    node.SetterParameter.Accept(this);
                }
                else
                {
                    // Create implicit 'value' parameter
                    var valueSymbol = new Symbol("value", SymbolKind.Parameter, propertyType, node.Line, node.Column);
                    _currentScope.Define(valueSymbol);
                }

                node.Setter.Accept(this);
                ExitScope();
            }
        }

        public void Visit(MyBaseExpressionNode node)
        {
            // MyBase refers to the base class
            // Validate we're inside a class that has a base class
            var classScope = _currentScope.GetClassScope();
            if (classScope == null)
            {
                Error("MyBase can only be used within a class", node.Line, node.Column);
                return;
            }

            var classType = classScope.ClassType;
            if (classType == null || classType.BaseType == null)
            {
                Error("MyBase can only be used in a class that inherits from another class", node.Line, node.Column);
            }

            // Set node type to base class type
            if (classType?.BaseType != null)
            {
                _nodeTypes[node] = classType.BaseType;
            }
        }

        public void Visit(LambdaExpressionNode node)
        {
            // Create a scope for the lambda parameters
            EnterScope("Lambda", ScopeKind.Function);

            // Register parameters in the lambda scope
            foreach (var param in node.Parameters)
            {
                var paramType = _typeManager.GetType(param.Type?.Name ?? "Object");
                var paramSymbol = new Symbol(param.Name, SymbolKind.Parameter, paramType, param.Line, param.Column);
                _currentScope.Define(paramSymbol);
            }

            // Analyze the body
            if (node.Body != null)
            {
                node.Body.Accept(this);
                // For function lambdas, the return type is the type of the expression
                if (node.IsFunction)
                {
                    var bodyType = GetNodeType(node.Body) ?? _typeManager.GetType("Object");
                    // Create a delegate type for the lambda
                    var delegateType = new TypeInfo($"Func<{bodyType.Name}>", TypeKind.Delegate);
                    SetNodeType(node, delegateType);
                }
                else
                {
                    // Sub lambdas return void
                    var delegateType = new TypeInfo("Action", TypeKind.Delegate);
                    SetNodeType(node, delegateType);
                }
            }
            else if (node.StatementBody != null)
            {
                node.StatementBody.Accept(this);
                // Statement body lambdas with explicit return type or void
                var returnType = node.ReturnType != null
                    ? _typeManager.GetType(node.ReturnType.Name)
                    : _typeManager.GetType("Void");
                var delegateType = new TypeInfo($"Func<{returnType.Name}>", TypeKind.Delegate);
                SetNodeType(node, delegateType);
            }

            ExitScope();
        }

        public void Visit(CollectionInitializerNode node)
        {
            TypeInfo commonType = null;

            // Analyze each element
            foreach (var element in node.Elements)
            {
                element.Accept(this);
                var elementType = GetNodeType(element);

                if (elementType != null)
                {
                    if (commonType == null)
                    {
                        commonType = elementType;
                    }
                    else if (!commonType.Equals(elementType))
                    {
                        // For mixed types, use Object
                        Warning($"Collection contains mixed types: {commonType.Name} and {elementType.Name}", node.Line, node.Column);
                        commonType = _typeManager.GetType("Object");
                    }
                }
            }

            // Set the type to array of the common element type
            var arrayType = new TypeInfo($"{(commonType?.Name ?? "Object")}[]", TypeKind.Array);
            arrayType.ElementType = commonType ?? _typeManager.GetType("Object");
            SetNodeType(node, arrayType);
        }

        public void Visit(TupleLiteralNode node)
        {
            var elementTypes = new List<TypeInfo>();

            // Analyze each element
            foreach (var element in node.Elements)
            {
                element.Accept(this);
                var elementType = GetNodeType(element) ?? _typeManager.GetType("Object");
                elementTypes.Add(elementType);
            }

            // Create tuple type
            var typeNames = string.Join(", ", elementTypes.Select(t => t.Name));
            var tupleType = new TypeInfo($"({typeNames})", TypeKind.Tuple);
            tupleType.TupleElementTypes = elementTypes;
            tupleType.TupleElementNames = node.ElementNames.ToList();
            SetNodeType(node, tupleType);
        }

        public void Visit(OperatorDeclarationNode node)
        {
            // Operators must be Shared/Static
            if (!node.IsShared)
            {
                Warning("Operators must be Shared (static)", node.Line, node.Column);
            }

            // Create a scope for the operator body
            EnterScope($"Operator {node.OperatorSymbol}", ScopeKind.Function);

            // Register parameters
            foreach (var param in node.Parameters)
            {
                var paramType = ResolveTypeReference(param.Type);
                var paramSymbol = new Symbol(param.Name, SymbolKind.Parameter, paramType, param.Line, param.Column);
                _currentScope.Define(paramSymbol);
            }

            // Analyze the body
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            ExitScope();
        }

        public void Visit(EventDeclarationNode node)
        {
            // Resolve the event type (delegate type)
            var eventType = node.EventType != null ? ResolveTypeReference(node.EventType) : _typeManager.GetType("EventHandler");

            // Register the event as a symbol
            var eventSymbol = new Symbol(node.Name, SymbolKind.Event, eventType, node.Line, node.Column);
            _currentScope.Define(eventSymbol);
        }

        public void Visit(RaiseEventStatementNode node)
        {
            // Look up the event
            var eventSymbol = _currentScope.Resolve(node.EventName);
            if (eventSymbol == null)
            {
                Error($"Event '{node.EventName}' is not defined", node.Line, node.Column);
            }

            // Analyze arguments
            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }
        }

        public void Visit(AddHandlerStatementNode node)
        {
            // Analyze the event expression
            node.EventExpression?.Accept(this);

            // Analyze the handler expression
            node.HandlerExpression?.Accept(this);
        }

        public void Visit(RemoveHandlerStatementNode node)
        {
            // Analyze the event expression
            node.EventExpression?.Accept(this);

            // Analyze the handler expression
            node.HandlerExpression?.Accept(this);
        }

        public void Visit(TypePatternNode node)
        {
            // Resolve the match type
            if (node.MatchType != null)
            {
                var resolvedType = ResolveTypeReference(node.MatchType);
                if (resolvedType == null)
                {
                    Error($"Unknown type '{node.MatchType.Name}' in type pattern", node.Line, node.Column);
                }
            }

            // If there's a variable binding, add it to scope
            if (!string.IsNullOrEmpty(node.VariableName) && node.MatchType != null)
            {
                var resolvedType = ResolveTypeReference(node.MatchType);
                if (resolvedType != null)
                {
                    var symbol = new Symbol(node.VariableName, SymbolKind.Variable, resolvedType, node.Line, node.Column);
                    _currentScope.Define(symbol);
                }
            }

            // Analyze When guard if present
            node.WhenGuard?.Accept(this);
        }

        public void Visit(ConstantPatternNode node)
        {
            // Analyze the constant value
            node.Value?.Accept(this);

            // Analyze When guard if present
            node.WhenGuard?.Accept(this);
        }

        public void Visit(RangePatternNode node)
        {
            // Analyze bounds
            node.LowerBound?.Accept(this);
            node.UpperBound?.Accept(this);

            // Verify bounds are comparable
            var lowerType = GetNodeType(node.LowerBound);
            var upperType = GetNodeType(node.UpperBound);

            if (lowerType != null && upperType != null)
            {
                if (!lowerType.IsNumeric() || !upperType.IsNumeric())
                {
                    Warning("Range pattern bounds should be numeric types", node.Line, node.Column);
                }
            }

            // Analyze When guard if present
            node.WhenGuard?.Accept(this);
        }

        public void Visit(ComparisonPatternNode node)
        {
            // Analyze the comparison value
            node.Value?.Accept(this);

            // Validate operator
            var validOperators = new[] { ">", "<", ">=", "<=", "=", "<>" };
            if (!validOperators.Contains(node.Operator))
            {
                Error($"Invalid comparison operator '{node.Operator}' in pattern", node.Line, node.Column);
            }

            // Analyze When guard if present
            node.WhenGuard?.Accept(this);
        }

        public void Visit(AwaitExpressionNode node)
        {
            // Check that we're inside an async function
            var funcScope = _currentScope.GetFunctionScope();
            // Note: In a full implementation, we would check if the function has IsAsync set
            // For now, just analyze the expression

            node.Expression?.Accept(this);

            // The expression should return a Task-like type
            // For now, we'll trust the programmer
        }

        public void Visit(YieldStatementNode node)
        {
            // Check that we're inside an iterator function
            var funcScope = _currentScope.GetFunctionScope();
            if (funcScope == null)
            {
                Error("Yield statement must be inside a function", node.Line, node.Column);
            }

            // Analyze the value if not a yield break
            if (!node.IsBreak && node.Value != null)
            {
                node.Value.Accept(this);
            }
        }

        // ====================================================================
        // Statements
        // ====================================================================

        public void Visit(BlockNode node)
        {
            EnterScope("Block", ScopeKind.Block);

            foreach (var statement in node.Statements)
            {
                statement.Accept(this);
            }

            ExitScope();
        }

        public void Visit(IfStatementNode node)
        {
            // Check condition
            node.Condition.Accept(this);
            var condType = GetNodeType(node.Condition);

            if (condType != null && !condType.Equals(_typeManager.BooleanType))
            {
                Warning($"Condition should be Boolean, got '{condType}'", node.Line, node.Column);
            }

            // Check branches
            node.ThenBlock.Accept(this);

            foreach (var (condition, block) in node.ElseIfClauses)
            {
                condition.Accept(this);
                var elseIfCondType = GetNodeType(condition);
                if (elseIfCondType != null && !elseIfCondType.Equals(_typeManager.BooleanType))
                {
                    Warning($"ElseIf condition should be Boolean, got '{elseIfCondType}'", node.Line, node.Column);
                }
                block.Accept(this);
            }

            if (node.ElseBlock != null)
            {
                node.ElseBlock.Accept(this);
            }
        }

        public void Visit(SelectStatementNode node)
        {
            node.Expression.Accept(this);
            var exprType = GetNodeType(node.Expression);

            foreach (var caseClause in node.Cases)
            {
                caseClause.Accept(this);

                // Check case values are compatible with expression
                foreach (var value in caseClause.Values)
                {
                    var valueType = GetNodeType(value);
                    if (valueType != null && exprType != null &&
                        !_typeManager.AreCompatible(exprType, valueType))
                    {
                        Error($"Case value type '{valueType}' is not compatible with select expression type '{exprType}'",
                              value.Line, value.Column);
                    }
                }
            }
        }

        public void Visit(CaseClauseNode node)
        {
            foreach (var value in node.Values)
            {
                value.Accept(this);
            }

            node.Body.Accept(this);
        }

        public void Visit(ForLoopNode node)
        {
            // Define loop variable
            var loopVarSymbol = new Symbol(node.Variable, SymbolKind.Variable,
                                          _typeManager.IntegerType, node.Line, node.Column);

            EnterScope("ForLoop", ScopeKind.Loop);

            if (!_currentScope.Define(loopVarSymbol))
            {
                Error($"Loop variable '{node.Variable}' conflicts with existing symbol", node.Line, node.Column);
            }

            // Check start, end, and step expressions
            node.Start.Accept(this);
            var startType = GetNodeType(node.Start);
            if (startType != null && !startType.IsNumeric())
            {
                Error($"For loop start value must be numeric, got '{startType}'", node.Line, node.Column);
            }

            node.End.Accept(this);
            var endType = GetNodeType(node.End);
            if (endType != null && !endType.IsNumeric())
            {
                Error($"For loop end value must be numeric, got '{endType}'", node.Line, node.Column);
            }

            if (node.Step != null)
            {
                node.Step.Accept(this);
                var stepType = GetNodeType(node.Step);
                if (stepType != null && !stepType.IsNumeric())
                {
                    Error($"For loop step value must be numeric, got '{stepType}'", node.Line, node.Column);
                }
            }

            node.Body.Accept(this);

            ExitScope();
        }

        public void Visit(ForEachLoopNode node)
        {
            // Check collection
            node.Collection.Accept(this);
            var collectionType = GetNodeType(node.Collection);

            TypeInfo elementType = ResolveTypeReference(node.VariableType);

            if (collectionType != null)
            {
                if (collectionType.Kind == TypeKind.Array)
                {
                    if (!elementType.IsAssignableFrom(collectionType.ElementType))
                    {
                        Error($"Cannot assign array element type '{collectionType.ElementType}' to loop variable of type '{elementType}'",
                              node.Line, node.Column);
                    }
                }
                else
                {
                    // Check if type is enumerable (has appropriate members)
                    // For now, just check if it's an array
                    Warning($"For Each requires an array or enumerable collection", node.Line, node.Column);
                }
            }

            EnterScope("ForEachLoop", ScopeKind.Loop);

            var loopVarSymbol = new Symbol(node.Variable, SymbolKind.Variable, elementType, node.Line, node.Column);
            if (!_currentScope.Define(loopVarSymbol))
            {
                Error($"Loop variable '{node.Variable}' conflicts with existing symbol", node.Line, node.Column);
            }

            node.Body.Accept(this);

            ExitScope();
        }

        public void Visit(WithStatementNode node)
        {
            // Analyze the object expression
            node.Object.Accept(this);
            var objectType = GetNodeType(node.Object);

            // Create a scope for the With block where member access can be implicit
            EnterScope("WithBlock", ScopeKind.Block);

            // Store the With object type for implicit member access resolution
            // (In a full implementation, we'd need to track this for .Property syntax)

            node.Body.Accept(this);

            ExitScope();
        }

        public void Visit(WhileLoopNode node)
        {
            node.Condition.Accept(this);
            var condType = GetNodeType(node.Condition);

            if (condType != null && !condType.Equals(_typeManager.BooleanType))
            {
                Warning($"While condition should be Boolean, got '{condType}'", node.Line, node.Column);
            }

            EnterScope("WhileLoop", ScopeKind.Loop);
            node.Body.Accept(this);
            ExitScope();
        }

        public void Visit(DoLoopNode node)
        {
            EnterScope("DoLoop", ScopeKind.Loop);
            node.Body.Accept(this);
            ExitScope();

            if (node.Condition != null)
            {
                node.Condition.Accept(this);
                var condType = GetNodeType(node.Condition);

                if (condType != null && !condType.Equals(_typeManager.BooleanType))
                {
                    Warning($"Loop condition should be Boolean, got '{condType}'", node.Line, node.Column);
                }
            }
        }

        public void Visit(TryStatementNode node)
        {
            node.TryBlock.Accept(this);

            foreach (var catchClause in node.CatchClauses)
            {
                catchClause.Accept(this);
            }
        }

        public void Visit(CatchClauseNode node)
        {
            var exceptionType = ResolveTypeReference(node.ExceptionType);

            EnterScope("Catch", ScopeKind.Block);

            if (!string.IsNullOrEmpty(node.ExceptionVariable))
            {
                var exSymbol = new Symbol(node.ExceptionVariable, SymbolKind.Variable,
                                         exceptionType, node.Line, node.Column);
                _currentScope.Define(exSymbol);
            }

            node.Body.Accept(this);

            ExitScope();
        }

        public void Visit(ThrowStatementNode node)
        {
            if (node.Exception != null)
            {
                node.Exception.Accept(this);
            }
        }

        public void Visit(ReturnStatementNode node)
        {
            var functionScope = _currentScope.GetFunctionScope();
            if (functionScope == null)
            {
                Error("Return statement outside of function", node.Line, node.Column);
                return;
            }

            if (node.Value != null)
            {
                node.Value.Accept(this);
                var returnType = GetNodeType(node.Value);

                if (functionScope.ReturnType.Equals(_typeManager.VoidType))
                {
                    Error("Cannot return a value from a subroutine", node.Line, node.Column);
                }
                else if (!functionScope.ReturnType.IsAssignableFrom(returnType))
                {
                    Error($"Cannot return type '{returnType}' from function expecting '{functionScope.ReturnType}'",
                          node.Line, node.Column);
                }
            }
            else
            {
                if (!functionScope.ReturnType.Equals(_typeManager.VoidType))
                {
                    Error($"Function must return a value of type '{functionScope.ReturnType}'",
                          node.Line, node.Column);
                }
            }
        }

        public void Visit(ExitStatementNode node)
        {
            // Exit Sub/Function should be inside a sub/function
            if (node.Kind == ExitKind.Sub || node.Kind == ExitKind.Function)
            {
                var functionScope = _currentScope.GetFunctionScope();
                if (functionScope == null)
                {
                    Error($"Exit {node.Kind} outside of {node.Kind.ToString().ToLower()}", node.Line, node.Column);
                }
            }
            // Exit For/Do/While should be inside a loop - we'll validate at IR generation or let C# handle it
        }

        public void Visit(AssignmentStatementNode node)
        {
            node.Target.Accept(this);
            node.Value.Accept(this);

            var targetType = GetNodeType(node.Target);
            var valueType = GetNodeType(node.Value);

            if (targetType == null || valueType == null)
                return;

            // Check if target is assignable
            if (node.Target is IdentifierExpressionNode idExpr)
            {
                var symbol = _currentScope.Resolve(idExpr.Name);
                if (symbol != null && symbol.IsConstant)
                {
                    Error($"Cannot assign to constant '{idExpr.Name}'", node.Line, node.Column);
                }
            }

            // Check type compatibility
            if (node.Operator == "=")
            {
                if (!targetType.IsAssignableFrom(valueType))
                {
                    Error($"Cannot assign value of type '{valueType}' to '{targetType}'",
                          node.Line, node.Column);
                }
            }
            else // Compound assignment
            {
                // Operators like +=, -= require numeric types
                if (!targetType.IsNumeric() || !valueType.IsNumeric())
                {
                    Error($"Operator '{node.Operator}' requires numeric operands", node.Line, node.Column);
                }
            }
        }

        public void Visit(ExpressionStatementNode node)
        {
            node.Expression.Accept(this);
        }

        // ====================================================================
        // Expressions
        // ====================================================================

        public void Visit(BinaryExpressionNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);

            var leftType = GetNodeType(node.Left);
            var rightType = GetNodeType(node.Right);

            if (leftType == null || rightType == null)
            {
                SetNodeType(node, _typeManager.ObjectType);
                return;
            }

            TypeInfo resultType;

            switch (node.Operator)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                case "%":
                    // Arithmetic operators
                    if (!leftType.IsNumeric() || !rightType.IsNumeric())
                    {
                        Error($"Arithmetic operator '{node.Operator}' requires numeric operands",
                              node.Line, node.Column);
                        resultType = _typeManager.IntegerType;
                    }
                    else
                    {
                        resultType = _typeManager.GetCommonType(leftType, rightType);
                    }
                    break;

                case "\\":
                    // Integer division
                    if (!leftType.IsIntegral() || !rightType.IsIntegral())
                    {
                        Error($"Integer division requires integral operands", node.Line, node.Column);
                    }
                    resultType = _typeManager.IntegerType;
                    break;

                case "&":
                    // String concatenation
                    if (leftType.Name == "String" || rightType.Name == "String")
                    {
                        resultType = _typeManager.StringType;
                    }
                    else
                    {
                        Error($"Operator '&' requires at least one string operand", node.Line, node.Column);
                        resultType = _typeManager.StringType;
                    }
                    break;

                case "<":
                case "<=":
                case ">":
                case ">=":
                    // Comparison operators
                    if (!leftType.IsNumeric() || !rightType.IsNumeric())
                    {
                        Error($"Comparison operator '{node.Operator}' requires numeric operands",
                              node.Line, node.Column);
                    }
                    resultType = _typeManager.BooleanType;
                    break;

                case "=":
                case "<>":
                case "!=":
                case "==":
                case "IsEqual":
                    // Equality operators
                    if (!_typeManager.AreCompatible(leftType, rightType) &&
                        !_typeManager.AreCompatible(rightType, leftType))
                    {
                        Warning($"Comparing incompatible types '{leftType}' and '{rightType}'",
                               node.Line, node.Column);
                    }
                    resultType = _typeManager.BooleanType;
                    break;

                case "And":
                case "&&":
                case "Or":
                case "||":
                    // Logical operators
                    if (!leftType.Equals(_typeManager.BooleanType) ||
                        !rightType.Equals(_typeManager.BooleanType))
                    {
                        Error($"Logical operator '{node.Operator}' requires Boolean operands",
                              node.Line, node.Column);
                    }
                    resultType = _typeManager.BooleanType;
                    break;

                default:
                    Error($"Unknown binary operator '{node.Operator}'", node.Line, node.Column);
                    resultType = _typeManager.ObjectType;
                    break;
            }

            SetNodeType(node, resultType);
        }

        public void Visit(UnaryExpressionNode node)
        {
            node.Operand.Accept(this);
            var operandType = GetNodeType(node.Operand);

            if (operandType == null)
            {
                SetNodeType(node, _typeManager.ObjectType);
                return;
            }

            TypeInfo resultType;

            switch (node.Operator)
            {
                case "-":
                case "+":
                    if (!operandType.IsNumeric())
                    {
                        Error($"Unary '{node.Operator}' requires numeric operand", node.Line, node.Column);
                    }
                    resultType = operandType;
                    break;

                case "Not":
                case "!":
                    if (!operandType.Equals(_typeManager.BooleanType))
                    {
                        Error($"Logical NOT requires Boolean operand", node.Line, node.Column);
                    }
                    resultType = _typeManager.BooleanType;
                    break;

                case "++":
                case "--":
                    if (!operandType.IsNumeric())
                    {
                        Error($"Operator '{node.Operator}' requires numeric operand", node.Line, node.Column);
                    }
                    resultType = operandType;
                    break;

                case "^":
                    // Pointer dereference
                    if (!operandType.IsPointer)
                    {
                        Error($"Cannot dereference non-pointer type '{operandType}'", node.Line, node.Column);
                        resultType = _typeManager.ObjectType;
                    }
                    else
                    {
                        resultType = operandType; // Should be base type, but simplified here
                    }
                    break;

                case "AddressOf":
                    resultType = _typeManager.CreatePointerType(operandType);
                    break;

                default:
                    Error($"Unknown unary operator '{node.Operator}'", node.Line, node.Column);
                    resultType = _typeManager.ObjectType;
                    break;
            }

            SetNodeType(node, resultType);
        }

        public void Visit(LiteralExpressionNode node)
        {
            TypeInfo type;

            switch (node.LiteralType)
            {
                case TokenType.IntegerLiteral:
                    type = _typeManager.IntegerType;
                    break;
                case TokenType.LongLiteral:
                    type = _typeManager.LongType;
                    break;
                case TokenType.SingleLiteral:
                    type = _typeManager.SingleType;
                    break;
                case TokenType.DoubleLiteral:
                    type = _typeManager.DoubleType;
                    break;
                case TokenType.StringLiteral:
                    type = _typeManager.StringType;
                    break;
                case TokenType.CharLiteral:
                    type = _typeManager.CharType;
                    break;
                case TokenType.BooleanLiteral:
                    type = _typeManager.BooleanType;
                    break;
                default:
                    type = _typeManager.ObjectType;
                    break;
            }

            SetNodeType(node, type);
        }

        public void Visit(InterpolatedStringNode node)
        {
            // Analyze each expression part
            foreach (var part in node.Parts)
            {
                if (part is ExpressionNode expr)
                {
                    expr.Accept(this);
                }
            }

            // Interpolated strings always result in String type
            SetNodeType(node, _typeManager.StringType);
        }

        public void Visit(IdentifierExpressionNode node)
        {
            var symbol = _currentScope.Resolve(node.Name);

            if (symbol == null)
            {
                Error($"Undefined identifier '{node.Name}'", node.Line, node.Column);
                SetNodeType(node, _typeManager.ObjectType);
            }
            else
            {
                SetNodeSymbol(node, symbol);
                SetNodeType(node, symbol.Type);
            }
        }

        public void Visit(MemberAccessExpressionNode node)
        {
            node.Object.Accept(this);
            var objectType = GetNodeType(node.Object);

            if (objectType == null)
            {
                SetNodeType(node, _typeManager.ObjectType);
                return;
            }

            // Look up member in type
            if (objectType.Members.TryGetValue(node.MemberName, out var memberSymbol))
            {
                SetNodeSymbol(node, memberSymbol);
                SetNodeType(node, memberSymbol.Type);
            }
            else
            {
                Error($"Type '{objectType.Name}' does not have a member '{node.MemberName}'",
                      node.Line, node.Column);
                SetNodeType(node, _typeManager.ObjectType);
            }
        }

        public void Visit(CallExpressionNode node)
        {
            node.Callee.Accept(this);

            var calleeType = GetNodeType(node.Callee);
            Symbol calleeSymbol = null;

            if (node.Callee is IdentifierExpressionNode idExpr)
            {
                calleeSymbol = _currentScope.Resolve(idExpr.Name);
            }
            else if (node.Callee is MemberAccessExpressionNode memberExpr)
            {
                calleeSymbol = GetNodeSymbol(memberExpr);
            }

            if (calleeSymbol != null &&
                (calleeSymbol.Kind == SymbolKind.Function || calleeSymbol.Kind == SymbolKind.Subroutine))
            {
                // Check argument count
                if (node.Arguments.Count != calleeSymbol.Parameters.Count)
                {
                    Error($"Function '{calleeSymbol.Name}' expects {calleeSymbol.Parameters.Count} arguments, got {node.Arguments.Count}",
                          node.Line, node.Column);
                }
                else
                {
                    // Check argument types
                    for (int i = 0; i < node.Arguments.Count; i++)
                    {
                        node.Arguments[i].Accept(this);
                        var argType = GetNodeType(node.Arguments[i]);
                        var paramType = calleeSymbol.Parameters[i].Type;

                        if (argType != null && paramType != null && !paramType.IsAssignableFrom(argType))
                        {
                            Error($"Argument {i + 1}: cannot convert from '{argType}' to '{paramType}'",
                                  node.Arguments[i].Line, node.Arguments[i].Column);
                        }
                    }
                }

                SetNodeType(node, calleeSymbol.ReturnType ?? _typeManager.VoidType);
            }
            else
            {
                // Process arguments anyway
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                SetNodeType(node, _typeManager.ObjectType);
            }
        }

        public void Visit(ArrayAccessExpressionNode node)
        {
            node.Array.Accept(this);
            var arrayType = GetNodeType(node.Array);

            if (arrayType == null)
            {
                SetNodeType(node, _typeManager.ObjectType);
                return;
            }

            if (arrayType.Kind != TypeKind.Array)
            {
                Error($"Cannot index non-array type '{arrayType}'", node.Line, node.Column);
                SetNodeType(node, _typeManager.ObjectType);
                return;
            }

            // Check indices
            foreach (var index in node.Indices)
            {
                index.Accept(this);
                var indexType = GetNodeType(index);

                if (indexType != null && !indexType.IsIntegral())
                {
                    Error($"Array index must be an integer type, got '{indexType}'",
                          index.Line, index.Column);
                }
            }

            if (node.Indices.Count != arrayType.ArrayRank)
            {
                Error($"Array expects {arrayType.ArrayRank} indices, got {node.Indices.Count}",
                      node.Line, node.Column);
            }

            SetNodeType(node, arrayType.ElementType ?? _typeManager.ObjectType);
        }

        public void Visit(NewExpressionNode node)
        {
            var type = ResolveTypeReference(node.Type);

            // TODO: Check constructor arguments
            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }

            SetNodeType(node, type);
        }

        public void Visit(CastExpressionNode node)
        {
            node.Expression.Accept(this);
            var targetType = ResolveTypeReference(node.TargetType);

            SetNodeType(node, targetType);
        }
    }
}