using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.SemanticAnalysis;
using BasicLang.Compiler.StdLib;
using BasicLang.Compiler.StdLib.CSharp;

namespace BasicLang.Compiler.CodeGen.CSharp
{
    /// <summary>
    /// Improved C# code generator.
    ///
    /// IMPORTANT: This generator intentionally avoids emitting compiler-temporary locals (t0, t1, ...)
    /// by inlining SSA IR values into C# expressions whenever it is safe/possible.
    ///
    /// Rule of thumb:
    /// - If an IR value has a Name that matches a real declared variable (local/param/global), we emit a statement assignment.
    /// - Otherwise, we treat it as an expression-only value and inline it where referenced (return, if-condition, RHS, etc.).
    /// - Calls are emitted as statements if their result is assigned to a declared variable or if the result is otherwise unused.
    /// </summary>
    public class ImprovedCSharpCodeGenerator : IIRVisitor
    {
        private readonly StringBuilder _output;
        private readonly CodeGenOptions _options;
        private int _indentLevel;

        private readonly Dictionary<string, string> _typeMap;
        private readonly HashSet<string> _usings;

        private IRModule _currentModule;
        private IRFunction _currentFunction;

        // Name mapping and declared identifier tracking
        private readonly Dictionary<IRValue, string> _valueNames;
        private readonly Dictionary<string, string> _variableNameMap; // logical name -> sanitized C# name
        private readonly HashSet<string> _declaredIdentifiers;         // logical names (locals/params/globals)
        private readonly Dictionary<string, IRValue> _tempDefsByName;  // tempName -> defining IRValue (only for non-declared names)

        // Use counts help decide whether to emit calls as statements or inline them into expressions
        private readonly Dictionary<IRValue, int> _useCounts;

        // For structured control flow generation
        private HashSet<BasicBlock> _processedBlocks;

        // Stack of loop end blocks for break detection
        private Stack<BasicBlock> _loopEndBlocks;

        // Standard library provider for built-in functions
        private readonly CSharpStdLibProvider _stdLib;

        public string GeneratedCode => _output.ToString();

        public ImprovedCSharpCodeGenerator(CodeGenOptions options = null)
        {
            _output = new StringBuilder();
            _options = options ?? new CodeGenOptions();
            _indentLevel = 0;

            _usings = new HashSet<string>();
            _valueNames = new Dictionary<IRValue, string>();
            _variableNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _declaredIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _tempDefsByName = new Dictionary<string, IRValue>(StringComparer.OrdinalIgnoreCase);
            _useCounts = new Dictionary<IRValue, int>();

            // Initialize standard library provider
            _stdLib = new CSharpStdLibProvider();

            // Initialize type mapping
            _typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Integer", "int" },
                { "Long", "long" },
                { "Single", "float" },
                { "Double", "double" },
                { "String", "string" },
                { "Boolean", "bool" },
                { "Char", "char" },
                { "Void", "void" },
                { "Object", "object" }
            };

            // Default using
            _usings.Add("System");
        }

        /// <summary>
        /// Generate C# code from IR module
        /// </summary>
        public string Generate(IRModule module)
        {
            _currentModule = module;

            _output.Clear();
            _indentLevel = 0;
            _usings.Clear();

            // Add default usings
            _usings.Add("System");
            _usings.Add("System.Collections.Generic");

            // Emit using directives
            foreach (var usingDirective in _usings.OrderBy(u => u))
                WriteLine($"using {usingDirective};");

            WriteLine();

            // Namespace
            WriteLine($"namespace {_options.Namespace}");
            WriteLine("{");
            Indent();

            // Class
            WriteLine($"{_options.ClassAccessModifier} class {_options.ClassName}");
            WriteLine("{");
            Indent();

            // Globals
            if (module.GlobalVariables.Count > 0)
            {
                WriteLine("// Global variables");
                foreach (var globalVar in module.GlobalVariables.Values)
                {
                    var type = MapType(globalVar.Type);
                    var name = SanitizeName(globalVar.Name);
                    WriteLine($"private static {type} {name};");
                }
                WriteLine();
            }

            // Functions
            bool hasUserMain = false;
            foreach (var function in module.Functions)
            {
                if (function.IsExternal) continue;

                GenerateFunction(function);
                WriteLine();

                if (function.Name.Equals("Main", StringComparison.OrdinalIgnoreCase))
                    hasUserMain = true;
            }

            // Optional default Main
            if (_options.GenerateMainMethod && !hasUserMain)
                GenerateMainMethod();

            Unindent();
            WriteLine("}");

            Unindent();
            WriteLine("}");

            _currentModule = null;
            return _output.ToString();
        }

        private void GenerateMainMethod()
        {
            WriteLine("static void Main(string[] args)");
            WriteLine("{");
            Indent();
            WriteLine("Console.WriteLine(\"No Main function found\");");
            Unindent();
            WriteLine("}");
        }

        private void GenerateFunction(IRFunction function)
        {
            _currentFunction = function;

            _valueNames.Clear();
            _variableNameMap.Clear();
            _declaredIdentifiers.Clear();
            _tempDefsByName.Clear();
            _useCounts.Clear();

            // Track declared identifiers (params, locals, globals)
            foreach (var param in function.Parameters)
                _declaredIdentifiers.Add(param.Name);

            foreach (var local in function.LocalVariables)
                _declaredIdentifiers.Add(local.Name);

            if (_currentModule != null)
            {
                foreach (var g in _currentModule.GlobalVariables.Values)
                    _declaredIdentifiers.Add(g.Name);
            }

            // Map parameters and locals to sanitized names
            foreach (var param in function.Parameters)
            {
                var sanitized = SanitizeName(param.Name);
                _valueNames[param] = sanitized;
                _variableNameMap[param.Name] = sanitized;
            }

            foreach (var localVar in function.LocalVariables)
            {
                var sanitized = SanitizeName(localVar.Name);
                _valueNames[localVar] = sanitized;
                _variableNameMap[localVar.Name] = sanitized;
            }

            AnalyzeUseCounts(function);
            BuildTempDefinitions(function);

            // Signature
            var returnType = MapType(function.ReturnType);
            var functionName = SanitizeName(function.Name);

            var parameters = string.Join(", ", function.Parameters.Select(p =>
                $"{MapType(p.Type)} {GetValueName(p)}"));

            WriteLine($"{_options.MethodAccessModifier} static {returnType} {functionName}({parameters})");

            WriteLine("{");
            Indent();

            // Declare locals (ONLY real locals; no compiler temps)
            var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var localVar in function.LocalVariables)
            {
                var varName = GetValueName(localVar);
                if (declared.Add(varName))
                {
                    var csharpType = MapType(localVar.Type);
                    string defaultValue;

                    // Check if this is an array with a specific size
                    if (localVar.Type?.Kind == TypeKind.Array && localVar.Type.ArraySize > 0)
                    {
                        var elementType = MapType(localVar.Type.ElementType);
                        defaultValue = $"new {elementType}[{localVar.Type.ArraySize}]";
                    }
                    else
                    {
                        defaultValue = GetDefaultValue(localVar.Type);
                    }

                    WriteLine($"{csharpType} {varName} = {defaultValue};");
                }
            }

            if (function.LocalVariables.Count > 0)
                WriteLine();

            // Body - use structured control flow generation
            _processedBlocks = new HashSet<BasicBlock>();
            _loopEndBlocks = new Stack<BasicBlock>();
            if (function.EntryBlock != null)
                GenerateStructuredBlock(function.EntryBlock);

            Unindent();
            WriteLine("}");

            _currentFunction = null;
        }

        private void AnalyzeUseCounts(IRFunction function)
        {
            foreach (var block in function.Blocks)
            {
                foreach (var instr in block.Instructions)
                {
                    foreach (var op in GetOperands(instr))
                    {
                        if (op == null) continue;
                        _useCounts.TryGetValue(op, out var c);
                        _useCounts[op] = c + 1;
                    }
                }
            }
        }

        private void BuildTempDefinitions(IRFunction function)
        {
            // Only map "temp-like" names (i.e., not declared locals/params/globals).
            foreach (var block in function.Blocks)
            {
                foreach (var instr in block.Instructions)
                {
                    if (instr is not IRValue v) continue;
                    if (string.IsNullOrEmpty(v.Name)) continue;

                    if (_declaredIdentifiers.Contains(v.Name))
                        continue;

                    // first definition wins (good enough for simple SSA-style temp regs)
                    if (!_tempDefsByName.ContainsKey(v.Name))
                        _tempDefsByName[v.Name] = v;
                }
            }
        }

        private void GenerateBlock(BasicBlock block, HashSet<BasicBlock> visited)
        {
            GenerateStructuredBlock(block);
        }

        /// <summary>
        /// Generate structured C# code from a basic block, recognizing control flow patterns.
        /// </summary>
        private void GenerateStructuredBlock(BasicBlock block)
        {
            if (block == null || _processedBlocks.Contains(block))
                return;

            _processedBlocks.Add(block);

            // Emit non-control-flow instructions
            EmitBlockInstructions(block);

            // Handle the terminator instruction with structured control flow
            var terminator = block.Instructions.LastOrDefault();

            if (terminator is IRConditionalBranch condBranch)
            {
                HandleConditionalBranch(condBranch);
            }
            else if (terminator is IRBranch branch)
            {
                HandleUnconditionalBranch(branch);
            }
            else if (terminator is IRReturn ret)
            {
                // Return is handled by Visit(IRReturn)
            }
            else if (terminator is IRSwitch switchInst)
            {
                HandleSwitchStatement(switchInst);
            }
            // For other terminators, the Visit method handles them
        }

        private void EmitBlockInstructions(BasicBlock block)
        {
            foreach (var instruction in block.Instructions)
            {
                // Skip control flow - we handle it structurally
                if (instruction is IRBranch or IRConditionalBranch or IRSwitch)
                    continue;

                if (!ShouldEmitInstruction(instruction))
                    continue;

                instruction.Accept(this);
            }
        }

        private void HandleConditionalBranch(IRConditionalBranch condBranch)
        {
            var condition = EmitExpression(condBranch.Condition);
            var trueBlock = condBranch.TrueTarget;
            var falseBlock = condBranch.FalseTarget;

            // Detect loop patterns
            if (IsLoopHeader(trueBlock, falseBlock, out var loopBody, out var loopEnd, out var loopInc, out var loopType, out var negateCondition))
            {
                // For Until loops, negate the condition
                var loopCondition = negateCondition ? $"!({condition})" : condition;
                GenerateLoop(loopCondition, loopBody, loopEnd, loopInc, loopType);
                return;
            }

            // Detect if-then-else pattern
            if (IsIfThenElse(trueBlock, falseBlock, out var thenBlock, out var elseBlock, out var mergeBlock))
            {
                GenerateIfThenElse(condition, thenBlock, elseBlock, mergeBlock);
                return;
            }

            // Detect simple if-then pattern (no else)
            if (IsIfThen(trueBlock, falseBlock, out thenBlock, out mergeBlock))
            {
                GenerateIfThen(condition, thenBlock, mergeBlock);
                return;
            }

            // Fallback: emit goto-style code
            WriteLine($"if ({condition})");
            WriteLine("{");
            Indent();
            GenerateStructuredBlock(trueBlock);
            Unindent();
            WriteLine("}");
            WriteLine("else");
            WriteLine("{");
            Indent();
            GenerateStructuredBlock(falseBlock);
            Unindent();
            WriteLine("}");
        }

        private void HandleUnconditionalBranch(IRBranch branch)
        {
            var target = branch.Target;

            // If the target is already processed or is a loop back-edge, skip
            // (the loop structure handles continuation)
            if (_processedBlocks.Contains(target))
                return;

            // If target is a merge block or loop end, we've already handled it
            if (target.Name.EndsWith(".end"))
                return;

            // Continue with the next block
            GenerateStructuredBlock(target);
        }

        private void HandleSwitchStatement(IRSwitch switchInst)
        {
            var value = EmitExpression(switchInst.Value);

            WriteLine($"switch ({value})");
            WriteLine("{");
            Indent();

            // Group cases by their target block
            var casesByBlock = new Dictionary<BasicBlock, List<IRValue>>();
            foreach (var (caseValue, target) in switchInst.Cases)
            {
                if (!casesByBlock.ContainsKey(target))
                    casesByBlock[target] = new List<IRValue>();
                casesByBlock[target].Add(caseValue);
            }

            // Emit each case group with inline body
            foreach (var (block, caseValues) in casesByBlock)
            {
                // Emit case labels
                foreach (var caseValue in caseValues)
                {
                    var caseExpr = EmitExpression(caseValue);
                    WriteLine($"case {caseExpr}:");
                }

                // Mark block as processed so it's not emitted again
                _processedBlocks.Add(block);

                // Emit the case body with indentation
                Indent();
                EmitBlockInstructions(block);

                // Check if block ends with a return (no break needed)
                var terminator = block.Instructions.LastOrDefault();
                if (terminator is IRReturn)
                {
                    // Return already emitted
                }
                else if (terminator is IRBranch br && br.Target.Name.Contains("switch.end"))
                {
                    // Jump to switch end - emit break
                    WriteLine("break;");
                }
                else if (terminator is IRBranch branch)
                {
                    // Process the branch target (might have more code)
                    HandleUnconditionalBranch(branch);
                    WriteLine("break;");
                }
                else
                {
                    // Default: add break
                    WriteLine("break;");
                }

                Unindent();
            }

            // Emit default case
            var defaultBlock = switchInst.DefaultTarget;
            WriteLine("default:");
            _processedBlocks.Add(defaultBlock);
            Indent();
            EmitBlockInstructions(defaultBlock);

            var defaultTerminator = defaultBlock.Instructions.LastOrDefault();
            if (defaultTerminator is IRReturn)
            {
                // Return already emitted
            }
            else
            {
                WriteLine("break;");
            }
            Unindent();

            Unindent();
            WriteLine("}");

            // Find and process switch.end block
            var endBlock = _currentFunction.Blocks.FirstOrDefault(b => b.Name == "switch.end");
            if (endBlock != null && !_processedBlocks.Contains(endBlock))
            {
                GenerateStructuredBlock(endBlock);
            }
        }

        private bool IsLoopHeader(BasicBlock trueBlock, BasicBlock falseBlock,
            out BasicBlock loopBody, out BasicBlock loopEnd, out BasicBlock loopInc, out string loopType, out bool negateCondition)
        {
            loopBody = null;
            loopEnd = null;
            loopInc = null;
            loopType = null;
            negateCondition = false;

            // Standard pattern: condition block branches to body (true) and end (false)
            if (trueBlock.Name.Contains(".body") && falseBlock.Name.Contains(".end"))
            {
                loopBody = trueBlock;
                loopEnd = falseBlock;
                negateCondition = false;

                // Find increment block through body block's terminator (more reliable than name matching)
                loopInc = FindIncrementBlock(trueBlock);

                if (trueBlock.Name.StartsWith("for.") || trueBlock.Name.StartsWith("foreach."))
                    loopType = "for";
                else if (trueBlock.Name.StartsWith("while."))
                    loopType = "while";
                else if (trueBlock.Name.StartsWith("do."))
                    loopType = "do";
                else
                    loopType = "while";

                return true;
            }

            // Until pattern: branches are swapped (end on true, body on false)
            if (trueBlock.Name.Contains(".end") && falseBlock.Name.Contains(".body"))
            {
                loopBody = falseBlock;
                loopEnd = trueBlock;
                negateCondition = true;  // Need to negate condition for Until loops

                // Find increment block through body block's terminator
                loopInc = FindIncrementBlock(falseBlock);

                if (falseBlock.Name.StartsWith("for.") || falseBlock.Name.StartsWith("foreach."))
                    loopType = "for";
                else if (falseBlock.Name.StartsWith("while."))
                    loopType = "while";
                else if (falseBlock.Name.StartsWith("do."))
                    loopType = "do";
                else
                    loopType = "while";

                return true;
            }

            return false;
        }

        /// <summary>
        /// Find the increment block by following the body block's branch target.
        /// This is more reliable than name matching when there are multiple loops.
        /// </summary>
        private BasicBlock FindIncrementBlock(BasicBlock bodyBlock)
        {
            // The body block should end with a branch to the increment block
            var terminator = bodyBlock.Instructions.LastOrDefault();
            if (terminator is IRBranch branch && branch.Target.Name.Contains(".inc"))
            {
                return branch.Target;
            }

            // If body has nested control flow, we need to trace through to find the inc block
            // Check all blocks that the body might branch to
            foreach (var instruction in bodyBlock.Instructions)
            {
                if (instruction is IRBranch br && br.Target.Name.Contains(".inc"))
                {
                    return br.Target;
                }
            }

            return null;
        }

        private void GenerateLoop(string condition, BasicBlock bodyBlock, BasicBlock endBlock, BasicBlock incBlock, string loopType)
        {
            // Push the loop end block so inner code can emit 'break' when targeting it
            if (endBlock != null)
                _loopEndBlocks.Push(endBlock);

            WriteLine($"while ({condition})");
            WriteLine("{");
            Indent();

            // Generate body
            _processedBlocks.Add(bodyBlock);
            EmitBlockInstructions(bodyBlock);

            // Handle body's terminator
            var bodyTerminator = bodyBlock.Instructions.LastOrDefault();
            if (bodyTerminator is IRConditionalBranch innerCond)
            {
                // Nested control flow in loop body
                HandleConditionalBranch(innerCond);
            }

            // Always generate increment if it exists
            if (incBlock != null && !_processedBlocks.Contains(incBlock))
            {
                _processedBlocks.Add(incBlock);
                EmitBlockInstructions(incBlock);
            }

            Unindent();
            WriteLine("}");

            // Pop the loop end block
            if (endBlock != null)
                _loopEndBlocks.Pop();

            // Continue after the loop
            if (endBlock != null && !_processedBlocks.Contains(endBlock))
            {
                _processedBlocks.Add(endBlock);
                EmitBlockInstructions(endBlock);

                // Handle end block's terminator
                var endTerminator = endBlock.Instructions.LastOrDefault();
                if (endTerminator is IRConditionalBranch endCond)
                    HandleConditionalBranch(endCond);
                else if (endTerminator is IRBranch endBranch)
                    HandleUnconditionalBranch(endBranch);
            }
        }

        private bool IsIfThenElse(BasicBlock trueBlock, BasicBlock falseBlock,
            out BasicBlock thenBlock, out BasicBlock elseBlock, out BasicBlock mergeBlock)
        {
            thenBlock = null;
            elseBlock = null;
            mergeBlock = null;

            // Pattern: true -> if.then, false -> if.else, both merge at if.end
            if (trueBlock.Name.Contains(".then") && falseBlock.Name.Contains(".else"))
            {
                thenBlock = trueBlock;
                elseBlock = falseBlock;

                // Find merge block
                mergeBlock = _currentFunction.Blocks.FirstOrDefault(b =>
                    b.Name.EndsWith(".end") &&
                    b.Name.StartsWith(trueBlock.Name.Split('.')[0]));

                return mergeBlock != null;
            }

            return false;
        }

        private bool IsIfThen(BasicBlock trueBlock, BasicBlock falseBlock,
            out BasicBlock thenBlock, out BasicBlock mergeBlock)
        {
            thenBlock = null;
            mergeBlock = null;

            // Pattern: true -> if.then, false -> if.end (no else)
            if (trueBlock.Name.Contains(".then") && falseBlock.Name.Contains(".end"))
            {
                thenBlock = trueBlock;
                mergeBlock = falseBlock;
                return true;
            }

            return false;
        }

        private void GenerateIfThenElse(string condition, BasicBlock thenBlock, BasicBlock elseBlock, BasicBlock mergeBlock)
        {
            WriteLine($"if ({condition})");
            WriteLine("{");
            Indent();

            _processedBlocks.Add(thenBlock);
            EmitBlockInstructions(thenBlock);

            // Handle then block's terminator (might have nested control flow, return, or break)
            var thenTerminator = thenBlock.Instructions.LastOrDefault();
            if (thenTerminator is IRConditionalBranch thenCond)
                HandleConditionalBranch(thenCond);
            else if (thenTerminator is IRBranch thenBranch)
            {
                if (IsLoopEndBlock(thenBranch.Target))
                    WriteLine("break;");
                else if (!_processedBlocks.Contains(thenBranch.Target))
                    HandleUnconditionalBranch(thenBranch);
            }

            Unindent();
            WriteLine("}");
            WriteLine("else");
            WriteLine("{");
            Indent();

            _processedBlocks.Add(elseBlock);
            EmitBlockInstructions(elseBlock);

            // Handle else block's terminator
            var elseTerminator = elseBlock.Instructions.LastOrDefault();
            if (elseTerminator is IRConditionalBranch elseCond)
                HandleConditionalBranch(elseCond);
            else if (elseTerminator is IRBranch elseBranch)
            {
                if (IsLoopEndBlock(elseBranch.Target))
                    WriteLine("break;");
                else if (!_processedBlocks.Contains(elseBranch.Target))
                    HandleUnconditionalBranch(elseBranch);
            }

            Unindent();
            WriteLine("}");

            // Continue after merge
            if (mergeBlock != null && !_processedBlocks.Contains(mergeBlock))
            {
                _processedBlocks.Add(mergeBlock);
                EmitBlockInstructions(mergeBlock);

                var mergeTerminator = mergeBlock.Instructions.LastOrDefault();
                if (mergeTerminator is IRConditionalBranch mergeCond)
                    HandleConditionalBranch(mergeCond);
                else if (mergeTerminator is IRBranch mergeBranch)
                    HandleUnconditionalBranch(mergeBranch);
            }
        }

        private void GenerateIfThen(string condition, BasicBlock thenBlock, BasicBlock mergeBlock)
        {
            WriteLine($"if ({condition})");
            WriteLine("{");
            Indent();

            _processedBlocks.Add(thenBlock);
            EmitBlockInstructions(thenBlock);

            // Handle then block's terminator
            var thenTerminator = thenBlock.Instructions.LastOrDefault();
            if (thenTerminator is IRConditionalBranch thenCond)
                HandleConditionalBranch(thenCond);
            else if (thenTerminator is IRBranch thenBranch)
            {
                // Check if this is a break (branch to loop end)
                if (IsLoopEndBlock(thenBranch.Target))
                    WriteLine("break;");
                else if (!_processedBlocks.Contains(thenBranch.Target))
                    HandleUnconditionalBranch(thenBranch);
            }

            Unindent();
            WriteLine("}");

            // Continue after merge
            if (mergeBlock != null && !_processedBlocks.Contains(mergeBlock))
            {
                _processedBlocks.Add(mergeBlock);
                EmitBlockInstructions(mergeBlock);

                var mergeTerminator = mergeBlock.Instructions.LastOrDefault();
                if (mergeTerminator is IRConditionalBranch mergeCond)
                    HandleConditionalBranch(mergeCond);
                else if (mergeTerminator is IRBranch mergeBranch)
                    HandleUnconditionalBranch(mergeBranch);
            }
        }

        private bool IsLoopEndBlock(BasicBlock block)
        {
            if (block == null || _loopEndBlocks.Count == 0)
                return false;
            return _loopEndBlocks.Contains(block);
        }

        private bool ShouldEmitInstruction(IRInstruction instruction)
        {
            // Non-values are usually control-flow or statements and should be emitted
            if (instruction is IRReturn or IRBranch or IRConditionalBranch or IRSwitch or IRLabel)
                return true;

            if (instruction is IRComment)
                return _options.GenerateComments;

            if (instruction is IRStore or IRAssignment)
                return true;

            if (instruction is IRAlloca or IRPhi)
                return false;

            if (instruction is IRCall call)
            {
                // void calls are statements
                var hasReturn = call.Type != null && !call.Type.Name.Equals("Void", StringComparison.OrdinalIgnoreCase);

                // If the IRCall is explicitly named as a declared variable destination, emit assignment statement
                if (IsNamedDestination(call))
                    return true;

                // If the result is unused, emit as a statement call (for side effects)
                if (!hasReturn || GetUseCount(call) == 0)
                    return true;

                // Otherwise, we inline it into expressions (no temp locals)
                return false;
            }

            if (instruction is IRValue v)
            {
                // Only emit expression-producing values when they represent an assignment
                // to a real declared variable (local/param/global). Otherwise inline.
                return IsNamedDestination(v);
            }

            return true;
        }

        private int GetUseCount(IRValue value) => _useCounts.TryGetValue(value, out var c) ? c : 0;

        private bool IsNamedDestination(IRValue value)
        {
            if (value == null) return false;
            if (string.IsNullOrEmpty(value.Name)) return false;
            return _declaredIdentifiers.Contains(value.Name);
        }

        private string GetValueName(IRValue value)
        {
            if (value is IRConstant constant)
                return EmitConstant(constant);

            if (_valueNames.TryGetValue(value, out var name))
                return name;

            if (value is IRVariable variable)
            {
                if (_variableNameMap.TryGetValue(variable.Name, out var mapped))
                {
                    _valueNames[value] = mapped;
                    return mapped;
                }

                name = SanitizeName(variable.Name);
                _variableNameMap[variable.Name] = name;
                _valueNames[value] = name;
                return name;
            }

            if (!string.IsNullOrEmpty(value.Name))
            {
                // Named value: sanitize and cache (this includes IRBinaryOp renamed to a real variable, etc.)
                name = SanitizeName(value.Name);

                if (_variableNameMap.TryGetValue(value.Name, out var mapped))
                    name = mapped;
                else
                    _variableNameMap[value.Name] = name;

                _valueNames[value] = name;
                return name;
            }

            // Unnamed / compiler-temp values should not become locals; but if we end up here,
            // fall back to a stable-ish name to avoid nulls.
            name = "_tmp";
            _valueNames[value] = name;
            return name;
        }

        private string EmitExpression(IRValue value) => EmitExpression(value, new HashSet<IRValue>(), false);

        /// <summary>
        /// Emit an expression, optionally wrapping in parentheses if it's a compound expression used as a sub-expression.
        /// </summary>
        private string EmitExpression(IRValue value, HashSet<IRValue> stack, bool needsParens = false)
        {
            if (value == null) return string.Empty;

            // Prevent infinite recursion on weird cyclic graphs
            if (!stack.Add(value))
                return GetValueName(value);

            try
            {
                switch (value)
                {
                    case IRConstant c:
                        return EmitConstant(c);

                    case IRVariable v:
                        // If it's a real variable, use its name; if it's a temp "register",
                        // try to inline its defining value.
                        if (_declaredIdentifiers.Contains(v.Name) || v.IsParameter || v.IsGlobal)
                            return GetValueName(v);

                        if (!string.IsNullOrEmpty(v.Name) && _tempDefsByName.TryGetValue(v.Name, out var def))
                            return EmitExpression(def, stack, needsParens);

                        return GetValueName(v);

                    case IRBinaryOp bin:
                    {
                        // Sub-expressions need parens to preserve precedence
                        var left = EmitExpression(bin.Left, stack, true);
                        var right = EmitExpression(bin.Right, stack, true);
                        var op = MapBinaryOperator(bin.Operation);
                        var expr = $"{left} {op} {right}";
                        return needsParens ? $"({expr})" : expr;
                    }

                    case IRUnaryOp un:
                    {
                        var operand = EmitExpression(un.Operand, stack, true);
                        var op = MapUnaryOperator(un.Operation);
                        var expr = $"{op}{operand}";
                        return needsParens ? $"({expr})" : expr;
                    }

                    case IRCompare cmp:
                    {
                        var left = EmitExpression(cmp.Left, stack, true);
                        var right = EmitExpression(cmp.Right, stack, true);
                        var op = MapCompareOperator(cmp.Comparison);
                        var expr = $"{left} {op} {right}";
                        return needsParens ? $"({expr})" : expr;
                    }

                    case IRCall call:
                    {
                        var argExprs = call.Arguments.Select(a => EmitExpression(a, stack, false)).ToArray();

                        // Check if this is a standard library function
                        if (_stdLib.CanHandle(call.FunctionName))
                        {
                            // Add required imports
                            foreach (var import in _stdLib.GetRequiredImports(call.FunctionName))
                            {
                                _usings.Add(import);
                            }
                            return _stdLib.EmitCall(call.FunctionName, argExprs);
                        }

                        var fn = SanitizeName(call.FunctionName);
                        var args = string.Join(", ", argExprs);
                        return $"{fn}({args})";
                    }

                    case IRLoad load:
                        return EmitExpression(load.Address, stack, needsParens);

                    case IRGetElementPtr gep:
                    {
                        var baseExpr = EmitExpression(gep.BasePointer, stack, false);
                        var indices = string.Join(", ", gep.Indices.Select(i => EmitExpression(i, stack, false)));
                        return $"{baseExpr}[{indices}]";
                    }

                    case IRCast cast:
                    {
                        var target = MapType(cast.Type);
                        var expr = EmitExpression(cast.Value, stack, false);
                        return $"({target}){expr}";
                    }

                    case IRAlloca alloca:
                    {
                        // IRBuilder sometimes uses <name>_addr as an address placeholder.
                        // In C#, treat it as just <name>.
                        if (!string.IsNullOrEmpty(alloca.Name) &&
                            alloca.Name.EndsWith("_addr", StringComparison.OrdinalIgnoreCase))
                        {
                            var baseName = alloca.Name.Substring(0, alloca.Name.Length - "_addr".Length);
                            return SanitizeName(baseName);
                        }
                        return SanitizeName(alloca.Name);
                    }

                    default:
                        // If it's a named destination, use it; otherwise try defs-by-name.
                        if (!string.IsNullOrEmpty(value.Name) && !_declaredIdentifiers.Contains(value.Name) &&
                            _tempDefsByName.TryGetValue(value.Name, out var def2) && !ReferenceEquals(def2, value))
                        {
                            return EmitExpression(def2, stack, needsParens);
                        }
                        return GetValueName(value);
                }
            }
            finally
            {
                stack.Remove(value);
            }
        }

        private IEnumerable<IRValue> GetOperands(IRInstruction instr)
        {
            switch (instr)
            {
                case IRBinaryOp bin:
                    return new[] { bin.Left, bin.Right };
                case IRUnaryOp un:
                    return new[] { un.Operand };
                case IRCompare cmp:
                    return new[] { cmp.Left, cmp.Right };
                case IRCast cast:
                    return new[] { cast.Value };
                case IRCall call:
                    return call.Arguments;
                case IRAssignment asg:
                    return new[] { asg.Value, asg.Target };
                case IRLoad load:
                    return new[] { load.Address };
                case IRStore store:
                    return new[] { store.Address, store.Value };
                case IRReturn ret:
                    return ret.Value != null ? new[] { ret.Value } : Array.Empty<IRValue>();
                case IRConditionalBranch br:
                    return new[] { br.Condition };
                case IRSwitch sw:
                    return new[] { sw.Value };
                case IRGetElementPtr gep:
                    var ops = new List<IRValue> { gep.BasePointer };
                    ops.AddRange(gep.Indices);
                    return ops;
                case IRPhi phi:
                    return phi.Operands.Select(i => i.Value).ToList();
                default:
                    return Array.Empty<IRValue>();
            }
        }

        // ====================================================================
        // IR Visitor Methods
        // ====================================================================

        public void Visit(IRFunction function) { }
        public void Visit(BasicBlock block) { }
        public void Visit(IRConstant constant) { }
        public void Visit(IRVariable variable) { }

        public void Visit(IRBinaryOp binaryOp)
        {
            if (!IsNamedDestination(binaryOp))
                return;

            var left = EmitExpression(binaryOp.Left);
            var right = EmitExpression(binaryOp.Right);
            var op = MapBinaryOperator(binaryOp.Operation);

            var target = GetValueName(binaryOp);
            WriteLine($"{target} = {left} {op} {right};");
        }

        public void Visit(IRUnaryOp unaryOp)
        {
            if (!IsNamedDestination(unaryOp))
                return;

            var operand = EmitExpression(unaryOp.Operand);
            var op = MapUnaryOperator(unaryOp.Operation);

            var target = GetValueName(unaryOp);
            WriteLine($"{target} = {op}{operand};");
        }

        public void Visit(IRCompare compare)
        {
            if (!IsNamedDestination(compare))
                return;

            var left = EmitExpression(compare.Left);
            var right = EmitExpression(compare.Right);
            var op = MapCompareOperator(compare.Comparison);

            var target = GetValueName(compare);
            WriteLine($"{target} = {left} {op} {right};");
        }

        public void Visit(IRAssignment assignment)
        {
            var value = EmitExpression(assignment.Value);
            var target = GetValueName(assignment.Target);
            WriteLine($"{target} = {value};");
        }

        public void Visit(IRLoad load)
        {
            if (!IsNamedDestination(load))
                return;

            var address = EmitExpression(load.Address);
            var target = GetValueName(load);
            WriteLine($"{target} = {address};");
        }

        public void Visit(IRStore store)
        {
            var value = EmitExpression(store.Value);

            // Array element store
            if (store.Address is IRGetElementPtr gep)
            {
                var baseExpr = EmitExpression(gep.BasePointer);
                var indices = string.Join(", ", gep.Indices.Select(EmitExpression));
                WriteLine($"{baseExpr}[{indices}] = {value};");
                return;
            }

            var address = EmitExpression(store.Address);
            WriteLine($"{address} = {value};");
        }

        public void Visit(IRCall call)
        {
            var argExprs = call.Arguments.Select(EmitExpression).ToArray();
            var functionName = call.FunctionName;

            var hasReturn = call.Type != null && !call.Type.Name.Equals("Void", StringComparison.OrdinalIgnoreCase);

            // Check if this is a standard library function
            if (_stdLib.CanHandle(functionName))
            {
                var stdLibCall = _stdLib.EmitCall(functionName, argExprs);

                // Add required imports
                foreach (var import in _stdLib.GetRequiredImports(functionName))
                {
                    _usings.Add(import);
                }

                if (hasReturn && IsNamedDestination(call))
                {
                    var target = GetValueName(call);
                    WriteLine($"{target} = {stdLibCall};");
                    return;
                }

                // Emit as statement (for void functions like Print)
                WriteLine($"{stdLibCall};");
                return;
            }

            // Regular function call
            var args = string.Join(", ", argExprs);
            var sanitizedName = SanitizeName(functionName);

            // If this call is explicitly targeted at a declared variable, emit assignment.
            if (hasReturn && IsNamedDestination(call))
            {
                var target = GetValueName(call);
                WriteLine($"{target} = {sanitizedName}({args});");
                return;
            }

            // Otherwise emit as statement when result unused / void
            if (!hasReturn || GetUseCount(call) == 0)
            {
                WriteLine($"{sanitizedName}({args});");
                return;
            }

            // If we got here, this call should have been inlined by EmitExpression.
            // Do nothing to avoid creating temps.
        }

        public void Visit(IRReturn ret)
        {
            if (ret.Value != null)
            {
                var value = EmitExpression(ret.Value);
                WriteLine($"return {value};");
            }
            else
            {
                // Skip unnecessary "return;" at end of void methods
                // Check if this is the last instruction in a void function
                bool isVoidFunction = _currentFunction?.ReturnType == null ||
                    _currentFunction.ReturnType.Name.Equals("Void", StringComparison.OrdinalIgnoreCase);

                bool isLastBlock = ret.ParentBlock?.Successors?.Count == 0;

                if (isVoidFunction && isLastBlock)
                {
                    // Don't emit unnecessary return at end of void method
                    return;
                }

                WriteLine("return;");
            }
        }

        public void Visit(IRBranch branch)
        {
            // Handled structurally in GenerateStructuredBlock - no direct goto emission
        }

        public void Visit(IRConditionalBranch condBranch)
        {
            // Handled structurally in HandleConditionalBranch - no direct goto emission
        }

        public void Visit(IRSwitch switchInst)
        {
            var value = EmitExpression(switchInst.Value);

            WriteLine($"switch ({value})");
            WriteLine("{");
            Indent();

            foreach (var (caseValue, target) in switchInst.Cases)
            {
                // Case labels must be compile-time constants in C#. We still stringify defensively.
                var caseExpr = EmitExpression(caseValue);
                WriteLine($"case {caseExpr}: goto {target.Name};");
            }

            WriteLine($"default: goto {switchInst.DefaultTarget.Name};");

            Unindent();
            WriteLine("}");
        }

        public void Visit(IRPhi phi)
        {
            // Phi nodes are SSA merge artifacts; in imperative C# emission they should be lowered earlier.
            WriteLine($"// Phi node: {phi.Name}");
        }

        public void Visit(IRAlloca alloca)
        {
            // No-op for C# (locals are declared from LocalVariables; arrays are references already)
        }

        public void Visit(IRGetElementPtr gep)
        {
            if (!IsNamedDestination(gep))
                return;

            var baseExpr = EmitExpression(gep.BasePointer);
            var indices = string.Join(", ", gep.Indices.Select(EmitExpression));
            var target = GetValueName(gep);

            WriteLine($"{target} = {baseExpr}[{indices}];");
        }

        public void Visit(IRCast cast)
        {
            if (!IsNamedDestination(cast))
                return;

            var value = EmitExpression(cast.Value);
            var targetType = MapType(cast.Type);
            var target = GetValueName(cast);

            WriteLine($"{target} = ({targetType}){value};");
        }

        public void Visit(IRLabel label)
        {
            Unindent();
            WriteLine($"{label.Name}:");
            Indent();
        }

        public void Visit(IRComment comment)
        {
            if (_options.GenerateComments)
                WriteLine($"// {comment.Text}");
        }

        // ====================================================================
        // Helper Methods
        // ====================================================================

        private string EmitConstant(IRConstant constant)
        {
            if (constant.Value == null)
                return "null";

            if (constant.Value is string str)
                return $"\"{EscapeString(str)}\"";

            if (constant.Value is char ch)
                return $"'{EscapeChar(ch)}'";

            if (constant.Value is bool b)
                return b ? "true" : "false";

            if (constant.Value is float f)
                return $"{f}f";

            return constant.Value.ToString();
        }

        private string MapType(TypeInfo type)
        {
            if (type == null)
                return "object";

            if (_typeMap.TryGetValue(type.Name, out var csharpType))
                return csharpType;

            if (type.Kind == TypeKind.Array && type.ElementType != null)
            {
                var elementType = MapType(type.ElementType);
                return $"{elementType}[]";
            }

            return type.Name;
        }

        private string GetDefaultValue(TypeInfo type)
        {
            if (type == null)
                return "null";

            var typeName = type.Name?.ToLower() ?? "";

            return typeName switch
            {
                "integer" => "0",
                "long" => "0L",
                "single" => "0.0f",
                "double" => "0.0",
                "boolean" => "false",
                "char" => "'\\0'",
                "string" => "\"\"",
                _ when type.Kind == TypeKind.Array => "null",
                _ when type.Kind == TypeKind.Pointer => "null",
                _ => "null"
            };
        }

        private string MapBinaryOperator(BinaryOpKind op) => op switch
        {
            BinaryOpKind.Add => "+",
            BinaryOpKind.Sub => "-",
            BinaryOpKind.Mul => "*",
            BinaryOpKind.Div => "/",
            BinaryOpKind.Mod => "%",
            BinaryOpKind.IntDiv => "/",
            BinaryOpKind.And => "&",
            BinaryOpKind.Or => "|",
            BinaryOpKind.Xor => "^",
            BinaryOpKind.Shl => "<<",
            BinaryOpKind.Shr => ">>",
            BinaryOpKind.Concat => "+",
            _ => "?"
        };

        private string MapUnaryOperator(UnaryOpKind op) => op switch
        {
            UnaryOpKind.Neg => "-",
            UnaryOpKind.Not => "!",
            UnaryOpKind.BitwiseNot => "~",
            UnaryOpKind.Inc => "++",
            UnaryOpKind.Dec => "--",
            _ => "?"
        };

        private string MapCompareOperator(CompareKind cmp) => cmp switch
        {
            CompareKind.Eq => "==",
            CompareKind.Ne => "!=",
            CompareKind.Lt => "<",
            CompareKind.Le => "<=",
            CompareKind.Gt => ">",
            CompareKind.Ge => ">=",
            _ => "?"
        };

        private string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "_unnamed";

            var sanitized = new StringBuilder();

            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sanitized.Append(ch);
            }

            var result = sanitized.ToString();

            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            if (IsCSharpKeyword(result))
                result = "@" + result;

            return result.Length > 0 ? result : "_unnamed";
        }

        private bool IsCSharpKeyword(string name)
        {
            var keywords = new HashSet<string>
            {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
                "char", "class", "const", "continue", "default", "do", "double",
                "else", "false", "finally", "for", "foreach", "goto", "if", "int",
                "null", "object", "return", "string", "switch", "this", "true",
                "try", "void", "while"
            };

            return keywords.Contains((name ?? "").ToLowerInvariant());
        }

        private string EscapeString(string str)
        {
            return str.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }

        private string EscapeChar(char ch)
        {
            if (ch == '\'') return "\\'";
            if (ch == '\\') return "\\\\";
            if (ch == '\n') return "\\n";
            if (ch == '\r') return "\\r";
            if (ch == '\t') return "\\t";
            return ch.ToString();
        }

        private void Write(string text) => _output.Append(text);

        private void WriteLine(string text = "")
        {
            if (!string.IsNullOrEmpty(text))
            {
                _output.Append(new string(' ', _indentLevel * _options.IndentSize));
                _output.AppendLine(text);
            }
            else
            {
                _output.AppendLine();
            }
        }

        private void Indent() => _indentLevel++;
        private void Unindent() => _indentLevel = Math.Max(0, _indentLevel - 1);
    }
}
