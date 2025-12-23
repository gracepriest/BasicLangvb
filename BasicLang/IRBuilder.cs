using System;
using System.Collections.Generic;
using System.Linq;
using BasicLang.Compiler.AST;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Compiler.IR
{
    /// <summary>
    /// Builds IR from AST with SSA transformation
    /// </summary>
    public class IRBuilder : IASTVisitor
    {
        private readonly SemanticAnalyzer _semanticAnalyzer;
        private IRModule _module;
        private IRFunction _currentFunction;
        private BasicBlock _currentBlock;
        private readonly Stack<LoopContext> _loopStack;
        private readonly Dictionary<string, Stack<IRVariable>> _variableVersions;
        private readonly Dictionary<string, IRVariable> _globalVariables;

        // For SSA construction
        private int _nextVersion = 0;

        public IRModule Module => _module;

        public IRBuilder(SemanticAnalyzer semanticAnalyzer)
        {
            _semanticAnalyzer = semanticAnalyzer;
            _loopStack = new Stack<LoopContext>();
            _variableVersions = new Dictionary<string, Stack<IRVariable>>();
            _globalVariables = new Dictionary<string, IRVariable>();
        }

        /// <summary>
        /// Build IR from program AST
        /// </summary>
        public IRModule Build(ProgramNode program, string moduleName = "main")
        {
            _module = new IRModule(moduleName);
            _currentFunction = null;
            _currentBlock = null;

            program.Accept(this);

            return _module;
        }

        private IRVariable CreateVariable(string name, TypeInfo type, int version = 0)
        {
            return new IRVariable(name, type, version);
        }

        private IRVariable GetOrCreateVariable(string name, TypeInfo type)
        {
            // Check for existing version
            if (_variableVersions.ContainsKey(name) && _variableVersions[name].Count > 0)
            {
                return _variableVersions[name].Peek();
            }

            // Check global
            if (_globalVariables.ContainsKey(name))
            {
                return _globalVariables[name];
            }

            // Create new version
            var variable = CreateVariable(name, type, _nextVersion++);

            if (!_variableVersions.ContainsKey(name))
            {
                _variableVersions[name] = new Stack<IRVariable>();
            }

            _variableVersions[name].Push(variable);

            return variable;
        }

        private void PushVariableVersion(string name, IRVariable variable)
        {
            if (!_variableVersions.ContainsKey(name))
            {
                _variableVersions[name] = new Stack<IRVariable>();
            }
            _variableVersions[name].Push(variable);
        }

        private void PopVariableVersion(string name)
        {
            if (_variableVersions.ContainsKey(name) && _variableVersions[name].Count > 0)
            {
                _variableVersions[name].Pop();
            }
        }

        private void EmitInstruction(IRInstruction instruction)
        {
            if (_currentBlock != null)
            {
                _currentBlock.AddInstruction(instruction);
            }
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

        public void Visit(NamespaceNode node)
        {
            // Namespaces are organizational - process members
            foreach (var member in node.Members)
            {
                member.Accept(this);
            }
        }

        public void Visit(ModuleNode node)
        {
            // Modules are organizational - process members
            foreach (var member in node.Members)
            {
                member.Accept(this);
            }
        }

        public void Visit(UsingDirectiveNode node)
        {
            // No IR generation needed
        }

        public void Visit(ImportDirectiveNode node)
        {
            // No IR generation needed
        }

        // ====================================================================
        // Functions and Subroutines
        // ====================================================================

        public void Visit(FunctionNode node)
        {
            var returnType = _semanticAnalyzer.GetNodeType(node) ?? new TypeInfo("Void", TypeKind.Void);

            _currentFunction = _module.CreateFunction(node.Name, returnType);

            // Create parameters
            foreach (var param in node.Parameters)
            {
                var paramType = _semanticAnalyzer.GetNodeType(param);
                var irParam = new IRVariable(param.Name, paramType) { IsParameter = true };
                _currentFunction.Parameters.Add(irParam);
                PushVariableVersion(param.Name, irParam);
            }

            // Create entry block
            _currentBlock = _currentFunction.CreateBlock("entry");

            // Process body
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            // Ensure function ends with return
            if (!_currentBlock.IsTerminated())
            {
                if (returnType.Name == "Void")
                {
                    EmitInstruction(new IRReturn());
                }
                else
                {
                    // Return default value
                    var defaultValue = CreateDefaultValue(returnType);
                    EmitInstruction(new IRReturn(defaultValue));
                }
            }

            // Clean up variable versions
            foreach (var param in node.Parameters)
            {
                PopVariableVersion(param.Name);
            }

            _currentFunction = null;
            _currentBlock = null;
        }

        public void Visit(SubroutineNode node)
        {
            var voidType = new TypeInfo("Void", TypeKind.Void);

            _currentFunction = _module.CreateFunction(node.Name, voidType);

            // Create parameters
            foreach (var param in node.Parameters)
            {
                var paramType = _semanticAnalyzer.GetNodeType(param);
                var irParam = new IRVariable(param.Name, paramType) { IsParameter = true };
                _currentFunction.Parameters.Add(irParam);
                PushVariableVersion(param.Name, irParam);
            }

            // Create entry block
            _currentBlock = _currentFunction.CreateBlock("entry");

            // Process body
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }

            // Ensure function ends with return
            if (!_currentBlock.IsTerminated())
            {
                EmitInstruction(new IRReturn());
            }

            // Clean up variable versions
            foreach (var param in node.Parameters)
            {
                PopVariableVersion(param.Name);
            }

            _currentFunction = null;
            _currentBlock = null;
        }

        public void Visit(ParameterNode node)
        {
            // Handled in function visit
        }

        // ====================================================================
        // Declarations
        // ====================================================================

        public void Visit(VariableDeclarationNode node)
        {
            var varType = _semanticAnalyzer.GetNodeType(node);

            if (_currentFunction == null)
            {
                // Global variable
                var globalVar = _module.CreateGlobalVariable(node.Name, varType);
                _globalVariables[node.Name] = globalVar;

                if (node.Initializer != null)
                {
                    // Global initializers need special handling
                    // For now, we'll create an initialization function
                }
            }
            else
            {
                // Local variable - register it for declaration
                var localVar = CreateVariable(node.Name, varType, _nextVersion++);
                PushVariableVersion(node.Name, localVar);
                _currentFunction.LocalVariables.Add(localVar);

                // Only emit IRAlloca for variables that need memory semantics
                // (arrays, ByRef parameters, address-of operations)
                bool needsMemory = varType.Kind == TypeKind.Array;

                if (needsMemory)
                {
                    var alloca = new IRAlloca($"{node.Name}_addr", varType);
                    EmitInstruction(alloca);
                }

                // Initialize if there's an initializer
                if (node.Initializer != null)
                {
                    node.Initializer.Accept(this);
                    var initValue = _expressionResult;

                    // For memory-backed variables, emit a store
                    if (needsMemory)
                    {
                        var alloca = _currentBlock.Instructions
                            .OfType<IRAlloca>()
                            .LastOrDefault(a => a.Name == $"{node.Name}_addr");
                        if (alloca != null)
                        {
                            EmitInstruction(new IRStore(initValue, alloca));
                        }
                    }

                    // Optimization: If the value is a direct call or op result,
                    // rename it to the variable instead of creating a separate assignment
                    if (initValue is IRCall call)
                    {
                        call.Name = localVar.Name;
                    }
                    else if (initValue is IRBinaryOp binOp)
                    {
                        binOp.Name = localVar.Name;
                    }
                    else if (initValue is IRUnaryOp unaryOp)
                    {
                        unaryOp.Name = localVar.Name;
                    }
                    else if (initValue is IRCompare compare)
                    {
                        compare.Name = localVar.Name;
                    }
                    else
                    {
                        // For constants, variables, or other values, emit an assignment
                        EmitInstruction(new IRAssignment(localVar, initValue));
                    }
                }
                // No initializer - C# backend will use default(T), no IR needed
            }
        }

        public void Visit(ConstantDeclarationNode node)
        {
            // Constants are typically inlined during expression evaluation
            // But we can store them for reference
            if (node.Value != null)
            {
                node.Value.Accept(this);
            }
        }

        public void Visit(TypeDefineNode node)
        {
            // Type aliases don't generate IR
        }

        // ====================================================================
        // Classes and Types
        // ====================================================================

        public void Visit(ClassNode node)
        {
            // For now, we'll skip class generation
            // Full class support requires more complex IR
            foreach (var member in node.Members)
            {
                member.Accept(this);
            }
        }

        public void Visit(InterfaceNode node)
        {
            // Interfaces don't generate IR directly
        }

        public void Visit(TypeNode node)
        {
            // User-defined types don't generate IR
        }

        public void Visit(StructureNode node)
        {
            // Structures don't generate IR directly
        }

        public void Visit(TemplateDeclarationNode node)
        {
            // Templates are expanded before IR generation
            if (node.Declaration != null)
            {
                node.Declaration.Accept(this);
            }
        }

        public void Visit(DelegateDeclarationNode node)
        {
            // Delegates are types, not executable code
        }

        public void Visit(ExtensionMethodNode node)
        {
            // Extension methods are regular functions
            if (node.Method != null)
            {
                node.Method.Accept(this);
            }
        }

        // ====================================================================
        // Statements
        // ====================================================================

        public void Visit(BlockNode node)
        {
            foreach (var statement in node.Statements)
            {
                statement.Accept(this);

                // Stop if we hit a terminator
                if (_currentBlock.IsTerminated())
                    break;
            }
        }

        public void Visit(IfStatementNode node)
        {
            // Evaluate condition
            node.Condition.Accept(this);
            var condition = _expressionResult;

            // Create blocks
            var thenBlock = _currentFunction.CreateBlock("if.then");
            var elseBlock = node.ElseBlock != null || node.ElseIfClauses.Count > 0
                ? _currentFunction.CreateBlock("if.else")
                : null;
            var mergeBlock = _currentFunction.CreateBlock("if.end");

            // Emit conditional branch
            var branchTarget = elseBlock ?? mergeBlock;
            EmitInstruction(new IRConditionalBranch(condition, thenBlock, branchTarget));

            // Generate then block
            _currentBlock = thenBlock;
            node.ThenBlock.Accept(this);
            if (!_currentBlock.IsTerminated())
            {
                EmitInstruction(new IRBranch(mergeBlock));
            }

            // Generate else/elseif blocks
            if (node.ElseIfClauses.Count > 0 || node.ElseBlock != null)
            {
                _currentBlock = elseBlock;

                // Handle elseif chain
                foreach (var (elseIfCond, elseIfBlock) in node.ElseIfClauses)
                {
                    elseIfCond.Accept(this);
                    var elseIfCondition = _expressionResult;

                    var elseIfThen = _currentFunction.CreateBlock("elseif.then");
                    var elseIfNext = _currentFunction.CreateBlock("elseif.else");

                    EmitInstruction(new IRConditionalBranch(elseIfCondition, elseIfThen, elseIfNext));

                    _currentBlock = elseIfThen;
                    elseIfBlock.Accept(this);
                    if (!_currentBlock.IsTerminated())
                    {
                        EmitInstruction(new IRBranch(mergeBlock));
                    }

                    _currentBlock = elseIfNext;
                }

                // Final else block
                if (node.ElseBlock != null)
                {
                    node.ElseBlock.Accept(this);
                }

                if (!_currentBlock.IsTerminated())
                {
                    EmitInstruction(new IRBranch(mergeBlock));
                }
            }

            // Continue with merge block
            _currentBlock = mergeBlock;
        }

        public void Visit(SelectStatementNode node)
        {
            // Evaluate switch expression
            node.Expression.Accept(this);
            var switchValue = _expressionResult;

            // Create blocks
            var defaultBlock = _currentFunction.CreateBlock("switch.default");
            var endBlock = _currentFunction.CreateBlock("switch.end");

            var switchInst = new IRSwitch(switchValue, defaultBlock);

            // Generate case blocks
            var caseBlocks = new List<BasicBlock>();
            int caseIndex = 0;
            foreach (var caseClause in node.Cases)
            {
                if (caseClause.IsElse)
                {
                    // Default case
                    continue;
                }

                var caseBlock = _currentFunction.CreateBlock($"switch_case_{caseIndex++}");
                caseBlocks.Add(caseBlock);

                // Add case values
                foreach (var caseValue in caseClause.Values)
                {
                    caseValue.Accept(this);
                    var value = _expressionResult;
                    switchInst.Cases.Add((value, caseBlock));
                }
            }

            EmitInstruction(switchInst);

            // Generate case bodies
            for (int i = 0; i < node.Cases.Count; i++)
            {
                var caseClause = node.Cases[i];

                if (caseClause.IsElse)
                {
                    _currentBlock = defaultBlock;
                }
                else
                {
                    _currentBlock = caseBlocks[i];
                }

                caseClause.Body.Accept(this);

                if (!_currentBlock.IsTerminated())
                {
                    EmitInstruction(new IRBranch(endBlock));
                }
            }

            // Default block (if no else case was provided)
            _currentBlock = defaultBlock;
            if (!_currentBlock.IsTerminated())
            {
                EmitInstruction(new IRBranch(endBlock));
            }

            _currentBlock = endBlock;
        }

        public void Visit(CaseClauseNode node)
        {
            // Handled in SelectStatementNode
        }

        public void Visit(ForLoopNode node)
        {
            // Create loop blocks
            var condBlock = _currentFunction.CreateBlock("for.cond");
            var bodyBlock = _currentFunction.CreateBlock("for.body");
            var incBlock = _currentFunction.CreateBlock("for.inc");
            var endBlock = _currentFunction.CreateBlock("for.end");

            // Initialize loop variable
            node.Start.Accept(this);
            var startValue = _expressionResult;

            var loopVar = GetOrCreateVariable(node.Variable, startValue.Type);
            EmitInstruction(new IRAssignment(loopVar, startValue));

            // Jump to condition
            EmitInstruction(new IRBranch(condBlock));

            // Condition block
            _currentBlock = condBlock;
            node.End.Accept(this);
            var endValue = _expressionResult;

            var tempName = _currentFunction.GetNextTempName();
            var cond = new IRCompare(tempName, CompareKind.Le, loopVar, endValue,
                new TypeInfo("Boolean", TypeKind.Primitive));
            EmitInstruction(cond);

            EmitInstruction(new IRConditionalBranch(cond, bodyBlock, endBlock));

            // Push loop context
            _loopStack.Push(new LoopContext(condBlock, endBlock));

            // Body block
            _currentBlock = bodyBlock;
            node.Body.Accept(this);

            if (!_currentBlock.IsTerminated())
            {
                EmitInstruction(new IRBranch(incBlock));
            }

            // Increment block
            _currentBlock = incBlock;
            IRValue stepValue;
            if (node.Step != null)
            {
                node.Step.Accept(this);
                stepValue = _expressionResult;
            }
            else
            {
                stepValue = new IRConstant(1, new TypeInfo("Integer", TypeKind.Primitive));
            }

            var incTemp = _currentFunction.GetNextTempName();
            var inc = new IRBinaryOp(incTemp, BinaryOpKind.Add, loopVar, stepValue, loopVar.Type);
            EmitInstruction(inc);

            var newLoopVar = CreateVariable(node.Variable, loopVar.Type, _nextVersion++);
            EmitInstruction(new IRAssignment(newLoopVar, inc));
            PushVariableVersion(node.Variable, newLoopVar);

            EmitInstruction(new IRBranch(condBlock));

            // Pop loop context
            _loopStack.Pop();
            PopVariableVersion(node.Variable);

            // Continue with end block
            _currentBlock = endBlock;
        }

        public void Visit(ForEachLoopNode node)
        {
            // Simplified for-each - assumes array iteration
            node.Collection.Accept(this);
            var collection = _expressionResult;

            // Create loop blocks
            var condBlock = _currentFunction.CreateBlock("foreach.cond");
            var bodyBlock = _currentFunction.CreateBlock("foreach.body");
            var incBlock = _currentFunction.CreateBlock("foreach.inc");
            var endBlock = _currentFunction.CreateBlock("foreach.end");

            // Initialize index variable
            var indexVar = CreateVariable("__index", new TypeInfo("Integer", TypeKind.Primitive), _nextVersion++);
            EmitInstruction(new IRAssignment(indexVar, new IRConstant(0, indexVar.Type)));

            // Get array length (simplified - would need runtime support)
            var lengthTemp = _currentFunction.GetNextTempName();
            var length = new IRVariable(lengthTemp, new TypeInfo("Integer", TypeKind.Primitive));
            EmitInstruction(new IRComment("Get array length"));

            EmitInstruction(new IRBranch(condBlock));

            // Condition block
            _currentBlock = condBlock;
            var condTemp = _currentFunction.GetNextTempName();
            var cond = new IRCompare(condTemp, CompareKind.Lt, indexVar, length,
                new TypeInfo("Boolean", TypeKind.Primitive));
            EmitInstruction(cond);
            EmitInstruction(new IRConditionalBranch(cond, bodyBlock, endBlock));

            // Body block
            _currentBlock = bodyBlock;

            // Get element at index
            var gepTemp = _currentFunction.GetNextTempName();
            var elemType = _semanticAnalyzer.GetNodeType(node) ?? new TypeInfo("Object", TypeKind.Class);
            var gep = new IRGetElementPtr(gepTemp, collection, elemType);
            gep.Indices.Add(indexVar);
            EmitInstruction(gep);

            var loadTemp = _currentFunction.GetNextTempName();
            var element = new IRLoad(loadTemp, gep, elemType);
            EmitInstruction(element);

            // Assign to loop variable
            var loopVar = GetOrCreateVariable(node.Variable, elemType);
            EmitInstruction(new IRAssignment(loopVar, element));

            _loopStack.Push(new LoopContext(incBlock, endBlock));
            node.Body.Accept(this);
            _loopStack.Pop();

            if (!_currentBlock.IsTerminated())
            {
                EmitInstruction(new IRBranch(incBlock));
            }

            // Increment block
            _currentBlock = incBlock;
            var incTemp = _currentFunction.GetNextTempName();
            var inc = new IRBinaryOp(incTemp, BinaryOpKind.Add, indexVar,
                new IRConstant(1, indexVar.Type), indexVar.Type);
            EmitInstruction(inc);

            var newIndex = CreateVariable("__index", indexVar.Type, _nextVersion++);
            EmitInstruction(new IRAssignment(newIndex, inc));

            EmitInstruction(new IRBranch(condBlock));

            // Continue with end block
            _currentBlock = endBlock;
        }

        public void Visit(WhileLoopNode node)
        {
            var condBlock = _currentFunction.CreateBlock("while.cond");
            var bodyBlock = _currentFunction.CreateBlock("while.body");
            var endBlock = _currentFunction.CreateBlock("while.end");

            EmitInstruction(new IRBranch(condBlock));

            // Condition block
            _currentBlock = condBlock;
            node.Condition.Accept(this);
            var condition = _expressionResult;
            EmitInstruction(new IRConditionalBranch(condition, bodyBlock, endBlock));

            // Body block
            _currentBlock = bodyBlock;
            _loopStack.Push(new LoopContext(condBlock, endBlock));
            node.Body.Accept(this);
            _loopStack.Pop();

            if (!_currentBlock.IsTerminated())
            {
                EmitInstruction(new IRBranch(condBlock));
            }

            // Continue with end block
            _currentBlock = endBlock;
        }

        public void Visit(DoLoopNode node)
        {
            var condBlock = _currentFunction.CreateBlock("do.cond");
            var bodyBlock = _currentFunction.CreateBlock("do.body");
            var endBlock = _currentFunction.CreateBlock("do.end");

            if (node.IsConditionAtStart && node.Condition != null)
            {
                // Do While/Until ... Loop - condition at start (like a while loop)
                EmitInstruction(new IRBranch(condBlock));

                // Condition block
                _currentBlock = condBlock;
                node.Condition.Accept(this);
                var condition = _expressionResult;

                // For Until, swap true/false branches
                if (node.IsWhile)
                    EmitInstruction(new IRConditionalBranch(condition, bodyBlock, endBlock));
                else
                    EmitInstruction(new IRConditionalBranch(condition, endBlock, bodyBlock));

                // Body block
                _currentBlock = bodyBlock;
                _loopStack.Push(new LoopContext(condBlock, endBlock));
                node.Body.Accept(this);
                _loopStack.Pop();

                if (!_currentBlock.IsTerminated())
                {
                    EmitInstruction(new IRBranch(condBlock));
                }
            }
            else
            {
                // Do ... Loop While/Until - condition at end (or infinite loop)
                EmitInstruction(new IRBranch(bodyBlock));

                // Body block
                _currentBlock = bodyBlock;
                _loopStack.Push(new LoopContext(condBlock, endBlock));
                node.Body.Accept(this);
                _loopStack.Pop();

                if (!_currentBlock.IsTerminated())
                {
                    EmitInstruction(new IRBranch(condBlock));
                }

                // Condition block
                _currentBlock = condBlock;
                if (node.Condition != null)
                {
                    node.Condition.Accept(this);
                    var condition = _expressionResult;

                    // For Until, swap true/false branches
                    if (node.IsWhile)
                        EmitInstruction(new IRConditionalBranch(condition, bodyBlock, endBlock));
                    else
                        EmitInstruction(new IRConditionalBranch(condition, endBlock, bodyBlock));
                }
                else
                {
                    // Infinite loop
                    EmitInstruction(new IRBranch(bodyBlock));
                }
            }

            // Continue with end block
            _currentBlock = endBlock;
        }

        public void Visit(TryStatementNode node)
        {
            // Simplified exception handling
            // Full support would require runtime support

            var tryBlock = _currentFunction.CreateBlock("try.body");
            var endBlock = _currentFunction.CreateBlock("try.end");

            EmitInstruction(new IRComment("Begin try block"));
            EmitInstruction(new IRBranch(tryBlock));

            _currentBlock = tryBlock;
            node.TryBlock.Accept(this);

            if (!_currentBlock.IsTerminated())
            {
                EmitInstruction(new IRBranch(endBlock));
            }

            // Catch blocks
            foreach (var catchClause in node.CatchClauses)
            {
                var catchBlock = _currentFunction.CreateBlock("catch.body");
                _currentBlock = catchBlock;

                EmitInstruction(new IRComment($"Catch {catchClause.ExceptionType}"));
                catchClause.Body.Accept(this);

                if (!_currentBlock.IsTerminated())
                {
                    EmitInstruction(new IRBranch(endBlock));
                }
            }

            _currentBlock = endBlock;
        }

        public void Visit(CatchClauseNode node)
        {
            // Handled in TryStatementNode
        }

        public void Visit(ReturnStatementNode node)
        {
            if (node.Value != null)
            {
                node.Value.Accept(this);
                EmitInstruction(new IRReturn(_expressionResult));
            }
            else
            {
                EmitInstruction(new IRReturn());
            }
        }

        public void Visit(ExitStatementNode node)
        {
            switch (node.Kind)
            {
                case ExitKind.For:
                case ExitKind.Do:
                case ExitKind.While:
                    // Jump to the break target of the current loop
                    if (_loopStack.Count == 0)
                    {
                        throw new Exception($"Exit {node.Kind} outside of loop");
                    }
                    var loopContext = _loopStack.Peek();
                    EmitInstruction(new IRBranch(loopContext.BreakTarget));
                    break;

                case ExitKind.Sub:
                case ExitKind.Function:
                    // Exit Sub/Function is like Return (without value for Sub)
                    EmitInstruction(new IRReturn());
                    break;
            }
        }

        public void Visit(AssignmentStatementNode node)
        {
            // Evaluate right-hand side
            node.Value.Accept(this);
            var value = _expressionResult;

            // Handle compound assignments
            if (node.Operator != "=")
            {
                // Load current value
                node.Target.Accept(this);
                var currentValue = _expressionResult;

                // Determine operation
                BinaryOpKind op = node.Operator switch
                {
                    "+=" or "=+" => BinaryOpKind.Add,
                    "-=" or "=-" => BinaryOpKind.Sub,
                    "*=" => BinaryOpKind.Mul,
                    "/=" => BinaryOpKind.Div,
                    _ => throw new Exception($"Unknown assignment operator: {node.Operator}")
                };

                var tempName = _currentFunction.GetNextTempName();
                var result = new IRBinaryOp(tempName, op, currentValue, value, currentValue.Type);
                EmitInstruction(result);
                value = result;
            }

            // Store to target
            if (node.Target is IdentifierExpressionNode idExpr)
            {
                var targetVar = GetOrCreateVariable(idExpr.Name, value.Type);

                // Optimization: If the value is a direct call or binary op result,
                // rename it to the target variable instead of creating a separate assignment
                if (value is IRCall call)
                {
                    // Rename the call's result to the target variable
                    call.Name = targetVar.Name;
                    // No need to emit IRAssignment - the call now directly assigns to target
                }
                else if (value is IRBinaryOp binOp)
                {
                    // Rename the binary op's result to the target variable
                    binOp.Name = targetVar.Name;
                    // No need to emit IRAssignment
                }
                else if (value is IRUnaryOp unaryOp)
                {
                    // Rename the unary op's result to the target variable
                    unaryOp.Name = targetVar.Name;
                    // No need to emit IRAssignment
                }
                else if (value is IRCompare compare)
                {
                    // Rename the compare result to the target variable
                    compare.Name = targetVar.Name;
                    // No need to emit IRAssignment
                }
                else
                {
                    // For constants, variables, or other values, emit an assignment
                    EmitInstruction(new IRAssignment(targetVar, value));
                }
            }
            else if (node.Target is MemberAccessExpressionNode memberExpr)
            {
                // Handle member assignment
                EmitInstruction(new IRComment($"Store to member: {memberExpr.MemberName}"));
            }
            else if (node.Target is ArrayAccessExpressionNode arrayExpr)
            {
                // Handle array element assignment
                arrayExpr.Array.Accept(this);
                var array = _expressionResult;

                // Get element pointer
                var gepTemp = _currentFunction.GetNextTempName();
                var gep = new IRGetElementPtr(gepTemp, array, value.Type);
                foreach (var index in arrayExpr.Indices)
                {
                    index.Accept(this);
                    gep.Indices.Add(_expressionResult);
                }
                EmitInstruction(gep);

                // Store to pointer
                EmitInstruction(new IRStore(value, gep));
            }
        }

        public void Visit(ExpressionStatementNode node)
        {
            node.Expression.Accept(this);
        }

        // ====================================================================
        // Expressions
        // ====================================================================

        private IRValue _expressionResult;

        public void Visit(BinaryExpressionNode node)
        {
            node.Left.Accept(this);
            var left = _expressionResult;

            node.Right.Accept(this);
            var right = _expressionResult;

            var resultType = _semanticAnalyzer.GetNodeType(node);
            var tempName = _currentFunction.GetNextTempName();

            // Map operator
            IRValue result;

            if (IsComparisonOperator(node.Operator))
            {
                var cmpKind = MapComparisonOperator(node.Operator);
                result = new IRCompare(tempName, cmpKind, left, right, resultType);
            }
            else
            {
                var opKind = MapBinaryOperator(node.Operator);
                result = new IRBinaryOp(tempName, opKind, left, right, resultType);
            }

            EmitInstruction(result);
            _expressionResult = result;
        }

        public void Visit(UnaryExpressionNode node)
        {
            node.Operand.Accept(this);
            var operand = _expressionResult;

            var resultType = _semanticAnalyzer.GetNodeType(node);
            var tempName = _currentFunction.GetNextTempName();

            var opKind = MapUnaryOperator(node.Operator);
            var result = new IRUnaryOp(tempName, opKind, operand, resultType);

            EmitInstruction(result);
            _expressionResult = result;
        }

        public void Visit(LiteralExpressionNode node)
        {
            var type = _semanticAnalyzer.GetNodeType(node);
            _expressionResult = new IRConstant(node.Value, type);
        }

        public void Visit(IdentifierExpressionNode node)
        {
            // Look up variable
            var variable = GetOrCreateVariable(node.Name, _semanticAnalyzer.GetNodeType(node));
            _expressionResult = variable;
        }

        public void Visit(MemberAccessExpressionNode node)
        {
            node.Object.Accept(this);
            var obj = _expressionResult;

            // Generate GEP for member access
            var memberType = _semanticAnalyzer.GetNodeType(node);
            var tempName = _currentFunction.GetNextTempName();

            var gep = new IRGetElementPtr(tempName, obj, memberType);
            EmitInstruction(new IRComment($"Access member: {node.MemberName}"));
            EmitInstruction(gep);

            // Load from pointer
            var loadTemp = _currentFunction.GetNextTempName();
            var load = new IRLoad(loadTemp, gep, memberType);
            EmitInstruction(load);

            _expressionResult = load;
        }

        public void Visit(CallExpressionNode node)
        {
            string functionName = "";

            if (node.Callee is IdentifierExpressionNode idExpr)
            {
                functionName = idExpr.Name;
            }
            else if (node.Callee is MemberAccessExpressionNode memberExpr)
            {
                functionName = $"{memberExpr.Object}.{memberExpr.MemberName}";
            }

            var returnType = _semanticAnalyzer.GetNodeType(node);

            // Create temp name for now - if this call is directly assigned to a variable,
            // the assignment handler will rename it to the target variable
            var tempName = returnType != null && returnType.Name != "Void"
                ? _currentFunction.GetNextTempName()
                : null;

            var call = new IRCall(tempName, functionName, returnType);

            // Evaluate arguments
            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
                call.Arguments.Add(_expressionResult);
            }

            EmitInstruction(call);
            _expressionResult = call;
        }

        public void Visit(ArrayAccessExpressionNode node)
        {
            node.Array.Accept(this);
            var array = _expressionResult;

            var elementType = _semanticAnalyzer.GetNodeType(node);
            var gepTemp = _currentFunction.GetNextTempName();
            var gep = new IRGetElementPtr(gepTemp, array, elementType);

            foreach (var index in node.Indices)
            {
                index.Accept(this);
                gep.Indices.Add(_expressionResult);
            }

            EmitInstruction(gep);

            // Load from array element
            var loadTemp = _currentFunction.GetNextTempName();
            var load = new IRLoad(loadTemp, gep, elementType);
            EmitInstruction(load);

            _expressionResult = load;
        }

        public void Visit(NewExpressionNode node)
        {
            var type = _semanticAnalyzer.GetNodeType(node);
            var tempName = _currentFunction.GetNextTempName();

            // Allocate memory
            var alloca = new IRAlloca(tempName, type);
            EmitInstruction(alloca);

            // Call constructor if there are arguments
            if (node.Arguments.Count > 0)
            {
                EmitInstruction(new IRComment("Constructor call"));
                // Would need to generate constructor call here
            }

            _expressionResult = alloca;
        }

        public void Visit(CastExpressionNode node)
        {
            node.Expression.Accept(this);
            var value = _expressionResult;

            var sourceType = _semanticAnalyzer.GetNodeType(node.Expression);
            var targetType = _semanticAnalyzer.GetNodeType(node);

            var tempName = _currentFunction.GetNextTempName();
            var castKind = DetermineCastKind(sourceType, targetType);

            var cast = new IRCast(tempName, value, sourceType, targetType, castKind);
            EmitInstruction(cast);

            _expressionResult = cast;
        }

        // ====================================================================
        // Helper Methods
        // ====================================================================

        private IRConstant CreateDefaultValue(TypeInfo type)
        {
            if (type.Name == "Integer" || type.Name == "Long")
                return new IRConstant(0, type);
            if (type.Name == "Single" || type.Name == "Double")
                return new IRConstant(0.0, type);
            if (type.Name == "Boolean")
                return new IRConstant(false, type);
            if (type.Name == "String")
                return new IRConstant("", type);

            return new IRConstant(null, type);
        }

        private bool IsComparisonOperator(string op)
        {
            return op == "<" || op == "<=" || op == ">" || op == ">=" ||
                   op == "=" || op == "<>" || op == "==" || op == "!=" || op == "IsEqual";
        }

        private CompareKind MapComparisonOperator(string op)
        {
            return op switch
            {
                "=" or "==" or "IsEqual" => CompareKind.Eq,
                "<>" or "!=" => CompareKind.Ne,
                "<" => CompareKind.Lt,
                "<=" => CompareKind.Le,
                ">" => CompareKind.Gt,
                ">=" => CompareKind.Ge,
                _ => throw new Exception($"Unknown comparison operator: {op}")
            };
        }

        private BinaryOpKind MapBinaryOperator(string op)
        {
            return op switch
            {
                "+" => BinaryOpKind.Add,
                "-" => BinaryOpKind.Sub,
                "*" => BinaryOpKind.Mul,
                "/" => BinaryOpKind.Div,
                "\\" => BinaryOpKind.IntDiv,
                "%" => BinaryOpKind.Mod,
                "&" => BinaryOpKind.Concat,
                "And" or "&&" => BinaryOpKind.And,
                "Or" or "||" => BinaryOpKind.Or,
                _ => throw new Exception($"Unknown binary operator: {op}")
            };
        }

        private UnaryOpKind MapUnaryOperator(string op)
        {
            return op switch
            {
                "-" => UnaryOpKind.Neg,
                "Not" or "!" => UnaryOpKind.Not,
                "++" => UnaryOpKind.Inc,
                "--" => UnaryOpKind.Dec,
                _ => throw new Exception($"Unknown unary operator: {op}")
            };
        }

        private CastKind DetermineCastKind(TypeInfo source, TypeInfo target)
        {
            if (source.IsFloatingPoint() && target.IsIntegral())
                return CastKind.FPToSI;
            if (source.IsIntegral() && target.IsFloatingPoint())
                return CastKind.SIToFP;
            if (source.IsIntegral() && target.IsIntegral())
                return source.Name == "Long" ? CastKind.Trunc : CastKind.SExt;
            if (source.IsFloatingPoint() && target.IsFloatingPoint())
                return source.Name == "Double" ? CastKind.FPTrunc : CastKind.FPExt;

            return CastKind.Bitcast;
        }

        private class LoopContext
        {
            public BasicBlock ContinueTarget { get; }
            public BasicBlock BreakTarget { get; }

            public LoopContext(BasicBlock continueTarget, BasicBlock breakTarget)
            {
                ContinueTarget = continueTarget;
                BreakTarget = breakTarget;
            }
        }
    }
}