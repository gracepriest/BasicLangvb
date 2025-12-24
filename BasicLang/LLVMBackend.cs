using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Compiler.CodeGen.LLVM
{
    /// <summary>
    /// LLVM IR code generator - emits textual LLVM IR (.ll files)
    /// Can be compiled with llc or clang to native code
    /// </summary>
    public class LLVMCodeGenerator : CodeGeneratorBase
    {
        private readonly StringBuilder _output;
        private readonly LLVMCodeGenOptions _options;
        private readonly Dictionary<string, int> _stringConstants;
        private readonly HashSet<string> _declaredIdentifiers;
        private readonly Dictionary<IRValue, string> _llvmNames;
        private IRModule _module;
        private int _tempCounter;
        private int _labelCounter;
        private int _stringCounter;

        public override string BackendName => "LLVM";
        public override TargetPlatform Target => TargetPlatform.LLVM;

        public LLVMCodeGenerator(LLVMCodeGenOptions options = null)
        {
            _output = new StringBuilder();
            _options = options ?? new LLVMCodeGenOptions();
            _stringConstants = new Dictionary<string, int>();
            _declaredIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _llvmNames = new Dictionary<IRValue, string>();
            _typeMapper = new LLVMTypeMapper();
        }

        protected override void InitializeTypeMap()
        {
            // LLVM IR type mappings
            _typeMap["Integer"] = "i32";
            _typeMap["Long"] = "i64";
            _typeMap["Single"] = "float";
            _typeMap["Double"] = "double";
            _typeMap["String"] = "i8*";
            _typeMap["Boolean"] = "i1";
            _typeMap["Char"] = "i8";
            _typeMap["Void"] = "void";
            _typeMap["Object"] = "i8*";
            _typeMap["Byte"] = "i8";
            _typeMap["Short"] = "i16";
            _typeMap["UInteger"] = "i32";
            _typeMap["ULong"] = "i64";
        }

        public override string Generate(IRModule module)
        {
            _module = module;
            _output.Clear();
            _stringConstants.Clear();
            _tempCounter = 0;
            _labelCounter = 0;
            _stringCounter = 0;

            // Collect all string constants first
            CollectStringConstants(module);

            // Generate module header
            GenerateHeader(module);

            // Generate string constant declarations
            GenerateStringConstants();

            // Generate external function declarations
            GenerateExternals();

            // Generate function definitions
            foreach (var function in module.Functions)
            {
                if (!function.IsExternal)
                {
                    GenerateFunction(function);
                    WriteLine();
                }
            }

            return _output.ToString();
        }

        private void CollectStringConstants(IRModule module)
        {
            foreach (var function in module.Functions)
            {
                foreach (var block in function.Blocks)
                {
                    foreach (var instruction in block.Instructions)
                    {
                        CollectStringsFromInstruction(instruction);
                    }
                }
            }
        }

        private void CollectStringsFromInstruction(IRInstruction instruction)
        {
            if (instruction is IRConstant constant && constant.Value is string str)
            {
                if (!_stringConstants.ContainsKey(str))
                {
                    _stringConstants[str] = _stringCounter++;
                }
            }

            if (instruction is IRCall call)
            {
                foreach (var arg in call.Arguments)
                {
                    if (arg is IRConstant c && c.Value is string s)
                    {
                        if (!_stringConstants.ContainsKey(s))
                        {
                            _stringConstants[s] = _stringCounter++;
                        }
                    }
                }
            }

            if (instruction is IRBinaryOp binOp)
            {
                CollectStringsFromInstruction(binOp.Left as IRInstruction);
                CollectStringsFromInstruction(binOp.Right as IRInstruction);
            }
        }

        private void GenerateHeader(IRModule module)
        {
            WriteLine($"; ModuleID = '{module.Name}'");
            WriteLine($"source_filename = \"{module.Name}.bas\"");
            WriteLine("target datalayout = \"e-m:w-i64:64-f80:128-n8:16:32:64-S128\"");
            WriteLine("target triple = \"x86_64-pc-windows-msvc\"");
            WriteLine();
        }

        private void GenerateStringConstants()
        {
            // Generate format strings for printf
            WriteLine("; Format strings for printf");
            WriteLine("@.fmt.int = private unnamed_addr constant [4 x i8] c\"%d\\0A\\00\"");
            WriteLine("@.fmt.long = private unnamed_addr constant [5 x i8] c\"%ld\\0A\\00\"");
            WriteLine("@.fmt.double = private unnamed_addr constant [4 x i8] c\"%f\\0A\\00\"");
            WriteLine("@.fmt.str = private unnamed_addr constant [4 x i8] c\"%s\\0A\\00\"");
            WriteLine("@.fmt.0 = private unnamed_addr constant [4 x i8] c\"%d\\0A\\00\"");  // Default int format
            WriteLine();

            if (_stringConstants.Count == 0) return;

            WriteLine("; String constants");
            foreach (var (str, id) in _stringConstants)
            {
                var escaped = EscapeLLVMString(str);
                var len = str.Length + 1; // +1 for null terminator
                WriteLine($"@.str.{id} = private unnamed_addr constant [{len} x i8] c\"{escaped}\\00\"");
            }
            WriteLine();
        }

        private void GenerateExternals()
        {
            WriteLine("; External function declarations");
            WriteLine("declare i32 @printf(i8*, ...)");
            WriteLine("declare i32 @puts(i8*)");
            WriteLine("declare i32 @scanf(i8*, ...)");
            WriteLine("declare double @sqrt(double)");
            WriteLine("declare double @pow(double, double)");
            WriteLine("declare double @sin(double)");
            WriteLine("declare double @cos(double)");
            WriteLine("declare double @tan(double)");
            WriteLine("declare double @log(double)");
            WriteLine("declare double @exp(double)");
            WriteLine("declare double @floor(double)");
            WriteLine("declare double @ceil(double)");
            WriteLine("declare double @fabs(double)");
            WriteLine("declare i32 @rand()");
            WriteLine("declare void @srand(i32)");
            WriteLine("declare i64 @time(i64*)");
            WriteLine();
        }

        private void GenerateFunction(IRFunction function)
        {
            _currentFunction = function;
            _llvmNames.Clear();
            _declaredIdentifiers.Clear();
            _tempCounter = 0;
            _labelCounter = 0;

            // Collect declared identifiers
            foreach (var param in function.Parameters)
                _declaredIdentifiers.Add(param.Name);
            foreach (var local in function.LocalVariables)
                _declaredIdentifiers.Add(local.Name);

            // Generate function signature
            var returnType = MapType(function.ReturnType);
            var funcName = SanitizeLLVMName(function.Name);
            var paramList = string.Join(", ", function.Parameters.Select(p =>
                $"{MapType(p.Type)} %{SanitizeLLVMName(p.Name)}"));

            WriteLine($"define {returnType} @{funcName}({paramList}) {{");

            // Entry block
            WriteLine("entry:");

            // Allocate stack space for locals
            foreach (var local in function.LocalVariables)
            {
                var localType = MapType(local.Type);
                var localName = SanitizeLLVMName(local.Name);
                WriteLine($"  %{localName}.addr = alloca {localType}");

                // Initialize to zero/default
                var defaultVal = GetDefaultValue(local.Type, localType);
                WriteLine($"  store {localType} {defaultVal}, {localType}* %{localName}.addr");
            }

            // Allocate stack space for parameters (so they can be modified)
            foreach (var param in function.Parameters)
            {
                var paramType = MapType(param.Type);
                var paramName = SanitizeLLVMName(param.Name);
                WriteLine($"  %{paramName}.addr = alloca {paramType}");
                WriteLine($"  store {paramType} %{paramName}, {paramType}* %{paramName}.addr");
            }

            if (function.LocalVariables.Count > 0 || function.Parameters.Count > 0)
                WriteLine();

            // Generate basic blocks
            var visitedBlocks = new HashSet<BasicBlock>();
            if (function.EntryBlock != null)
            {
                GenerateBasicBlock(function.EntryBlock, visitedBlocks, isEntry: true);
            }

            // Ensure function has a return if void
            if (returnType == "void" && !EndsWithTerminator())
            {
                WriteLine("  ret void");
            }

            WriteLine("}");
        }

        private bool EndsWithTerminator()
        {
            var lines = _output.ToString().Split('\n');
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;
                return line.StartsWith("ret ") || line.StartsWith("br ") || line.StartsWith("unreachable");
            }
            return false;
        }

        private void GenerateBasicBlock(BasicBlock block, HashSet<BasicBlock> visited, bool isEntry = false)
        {
            if (visited.Contains(block)) return;
            visited.Add(block);

            // Emit label (skip for entry block as we already emitted "entry:")
            if (!isEntry)
            {
                WriteLine($"{SanitizeLLVMLabel(block.Name)}:");
            }

            // Process instructions
            foreach (var instruction in block.Instructions)
            {
                instruction.Accept(this);
            }

            // Process successor blocks
            foreach (var successor in block.Successors.Where(s => !visited.Contains(s)))
            {
                GenerateBasicBlock(successor, visited);
            }
        }

        private string GetLLVMName(IRValue value)
        {
            if (value is IRConstant constant)
                return EmitConstantValue(constant);

            if (_llvmNames.TryGetValue(value, out var name))
                return name;

            if (value is IRVariable variable)
            {
                name = $"%{SanitizeLLVMName(variable.Name)}";
            }
            else
            {
                name = $"%t{_tempCounter++}";
            }

            _llvmNames[value] = name;
            return name;
        }

        private string EmitConstantValue(IRConstant constant)
        {
            if (constant.Value == null) return "null";
            if (constant.Value is bool b) return b ? "true" : "false";
            if (constant.Value is string s)
            {
                if (_stringConstants.TryGetValue(s, out var id))
                {
                    var len = s.Length + 1;
                    return $"getelementptr inbounds ([{len} x i8], [{len} x i8]* @.str.{id}, i64 0, i64 0)";
                }
                return "null";
            }
            if (constant.Value is char c) return ((int)c).ToString();
            if (constant.Value is float f) return FormatFloat(f);
            if (constant.Value is double d) return FormatDouble(d);
            return constant.Value.ToString();
        }

        private string FormatFloat(float f)
        {
            if (float.IsPositiveInfinity(f)) return "0x7FF0000000000000";
            if (float.IsNegativeInfinity(f)) return "0xFFF0000000000000";
            if (float.IsNaN(f)) return "0x7FF8000000000000";
            return f.ToString("G17");
        }

        private string FormatDouble(double d)
        {
            if (double.IsPositiveInfinity(d)) return "0x7FF0000000000000";
            if (double.IsNegativeInfinity(d)) return "0xFFF0000000000000";
            if (double.IsNaN(d)) return "0x7FF8000000000000";
            return d.ToString("G17");
        }

        private string GetDefaultValue(TypeInfo type, string llvmType)
        {
            if (type == null) return "zeroinitializer";

            var typeName = type.Name?.ToLower() ?? "";
            return typeName switch
            {
                "integer" or "long" or "short" or "byte" or "char" => "0",
                "single" or "float" => "0.0",
                "double" => "0.0",
                "boolean" => "false",
                "string" => "null",
                _ when llvmType.EndsWith("*") => "null",
                _ => "zeroinitializer"
            };
        }

        private string SanitizeLLVMName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unnamed";

            var result = new StringBuilder();
            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '.')
                    result.Append(ch);
                else
                    result.Append('_');
            }

            var sanitized = result.ToString();
            if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            return sanitized;
        }

        private string SanitizeLLVMLabel(string name)
        {
            return SanitizeLLVMName(name).Replace(".", "_");
        }

        private string EscapeLLVMString(string str)
        {
            var sb = new StringBuilder();
            foreach (var ch in str)
            {
                if (ch == '\\') sb.Append("\\5C");
                else if (ch == '"') sb.Append("\\22");
                else if (ch == '\n') sb.Append("\\0A");
                else if (ch == '\r') sb.Append("\\0D");
                else if (ch == '\t') sb.Append("\\09");
                else if (ch < 32 || ch > 126) sb.Append($"\\{((int)ch):X2}");
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        private bool IsFloatType(TypeInfo type)
        {
            if (type == null) return false;
            var name = type.Name?.ToLower() ?? "";
            return name is "single" or "float" or "double";
        }

        private bool IsNamedDestination(IRValue value)
        {
            if (value == null) return false;
            if (string.IsNullOrEmpty(value.Name)) return false;
            return _declaredIdentifiers.Contains(value.Name);
        }

        #region Visitor Methods

        public override void Visit(IRFunction function) { }
        public override void Visit(BasicBlock block) { }
        public override void Visit(IRConstant constant) { }
        public override void Visit(IRVariable variable) { }

        public override void Visit(IRBinaryOp binaryOp)
        {
            var leftVal = GetLLVMName(binaryOp.Left);
            var rightVal = GetLLVMName(binaryOp.Right);
            var result = GetLLVMName(binaryOp);
            var type = MapType(binaryOp.Type);

            // Load values if they're addresses
            leftVal = LoadIfNeeded(binaryOp.Left, leftVal);
            rightVal = LoadIfNeeded(binaryOp.Right, rightVal);

            string op;
            if (IsFloatType(binaryOp.Type))
            {
                op = ((LLVMTypeMapper)_typeMapper).MapFloatBinaryOperator(binaryOp.Operation);
            }
            else
            {
                op = _typeMapper.MapBinaryOperator(binaryOp.Operation);
            }

            WriteLine($"  {result} = {op} {type} {leftVal}, {rightVal}");

            // If this is assigned to a declared variable, store it
            if (IsNamedDestination(binaryOp))
            {
                var varName = SanitizeLLVMName(binaryOp.Name);
                WriteLine($"  store {type} {result}, {type}* %{varName}.addr");
            }
        }

        public override void Visit(IRUnaryOp unaryOp)
        {
            var operandVal = GetLLVMName(unaryOp.Operand);
            var result = GetLLVMName(unaryOp);
            var type = MapType(unaryOp.Type);

            operandVal = LoadIfNeeded(unaryOp.Operand, operandVal);

            switch (unaryOp.Operation)
            {
                case UnaryOpKind.Neg:
                    if (IsFloatType(unaryOp.Type))
                        WriteLine($"  {result} = fneg {type} {operandVal}");
                    else
                        WriteLine($"  {result} = sub {type} 0, {operandVal}");
                    break;
                case UnaryOpKind.Not:
                    WriteLine($"  {result} = xor {type} {operandVal}, true");
                    break;
                case UnaryOpKind.BitwiseNot:
                    WriteLine($"  {result} = xor {type} {operandVal}, -1");
                    break;
                default:
                    WriteLine($"  ; Unsupported unary op: {unaryOp.Operation}");
                    break;
            }
        }

        public override void Visit(IRCompare compare)
        {
            var leftVal = GetLLVMName(compare.Left);
            var rightVal = GetLLVMName(compare.Right);
            var result = GetLLVMName(compare);
            var type = MapType(compare.Left.Type);

            leftVal = LoadIfNeeded(compare.Left, leftVal);
            rightVal = LoadIfNeeded(compare.Right, rightVal);

            string cmpOp;
            string cmpInst;

            if (IsFloatType(compare.Left.Type))
            {
                cmpInst = "fcmp";
                cmpOp = ((LLVMTypeMapper)_typeMapper).MapFloatComparisonOperator(compare.Comparison);
            }
            else
            {
                cmpInst = "icmp";
                cmpOp = _typeMapper.MapComparisonOperator(compare.Comparison);
            }

            WriteLine($"  {result} = {cmpInst} {cmpOp} {type} {leftVal}, {rightVal}");
        }

        public override void Visit(IRAssignment assignment)
        {
            var value = GetLLVMName(assignment.Value);
            var targetName = SanitizeLLVMName(assignment.Target.Name);
            var type = MapType(assignment.Target.Type);

            value = LoadIfNeeded(assignment.Value, value);

            WriteLine($"  store {type} {value}, {type}* %{targetName}.addr");
        }

        public override void Visit(IRLoad load)
        {
            var result = GetLLVMName(load);
            var type = MapType(load.Type);

            if (load.Address is IRVariable variable)
            {
                var varName = SanitizeLLVMName(variable.Name);
                WriteLine($"  {result} = load {type}, {type}* %{varName}.addr");
            }
            else
            {
                var addr = GetLLVMName(load.Address);
                WriteLine($"  {result} = load {type}, {type}* {addr}");
            }
        }

        public override void Visit(IRStore store)
        {
            var value = GetLLVMName(store.Value);
            var type = MapType(store.Value.Type);

            value = LoadIfNeeded(store.Value, value);

            if (store.Address is IRVariable variable)
            {
                var varName = SanitizeLLVMName(variable.Name);
                WriteLine($"  store {type} {value}, {type}* %{varName}.addr");
            }
            else if (store.Address is IRGetElementPtr gep)
            {
                var gepResult = GetLLVMName(gep);
                WriteLine($"  store {type} {value}, {type}* {gepResult}");
            }
            else
            {
                var addr = GetLLVMName(store.Address);
                WriteLine($"  store {type} {value}, {type}* {addr}");
            }
        }

        public override void Visit(IRCall call)
        {
            var args = new List<string>();
            foreach (var arg in call.Arguments)
            {
                var argVal = GetLLVMName(arg);
                argVal = LoadIfNeeded(arg, argVal);
                var argType = MapType(arg.Type);
                args.Add($"{argType} {argVal}");
            }

            var funcName = call.FunctionName;
            var hasReturn = call.Type != null && !call.Type.Name.Equals("Void", StringComparison.OrdinalIgnoreCase);

            // Check if this is an extern function call
            if (_module != null && _module.IsExtern(funcName))
            {
                var externDecl = _module.GetExtern(funcName);
                if (externDecl != null && externDecl.HasImplementation("LLVM"))
                {
                    var impl = externDecl.GetImplementation("LLVM");
                    var extRetType = MapType(call.Type);

                    // For LLVM, the implementation is typically just the function name to call
                    var extArgList = string.Join(", ", args);
                    var externCall = $"call {extRetType} @{impl}({extArgList})";

                    if (hasReturn && !string.IsNullOrEmpty(call.Name))
                    {
                        var extResultName = GetLLVMName(call);
                        if (_declaredIdentifiers.Contains(call.Name))
                        {
                            WriteLine($"  {extResultName} = {externCall}");
                            WriteLine($"  store {extRetType} {extResultName}, {extRetType}* %{SanitizeLLVMName(call.Name)}.addr");
                        }
                        else
                        {
                            WriteLine($"  {extResultName} = {externCall}");
                        }
                    }
                    else
                    {
                        WriteLine($"  {externCall}");
                    }
                    return;
                }
            }

            // Handle standard library calls
            var stdlibResult = EmitStdLibCall(funcName, call.Arguments.ToList(), args);
            if (stdlibResult != null)
            {
                if (hasReturn && !string.IsNullOrEmpty(call.Name))
                {
                    var resultName = GetLLVMName(call);
                    var resultType = MapType(call.Type);

                    // If it's a named destination (local variable), store it
                    if (_declaredIdentifiers.Contains(call.Name))
                    {
                        WriteLine($"  {resultName} = {stdlibResult}");
                        WriteLine($"  store {resultType} {resultName}, {resultType}* %{SanitizeLLVMName(call.Name)}.addr");
                    }
                    else
                    {
                        WriteLine($"  {resultName} = {stdlibResult}");
                    }
                }
                else
                {
                    WriteLine($"  {stdlibResult}");
                }
                return;
            }

            // Regular function call
            var returnType = MapType(call.Type);
            var sanitizedName = SanitizeLLVMName(funcName);
            var argList = string.Join(", ", args);

            if (hasReturn && !string.IsNullOrEmpty(call.Name))
            {
                var resultName = GetLLVMName(call);

                if (_declaredIdentifiers.Contains(call.Name))
                {
                    WriteLine($"  {resultName} = call {returnType} @{sanitizedName}({argList})");
                    WriteLine($"  store {returnType} {resultName}, {returnType}* %{SanitizeLLVMName(call.Name)}.addr");
                }
                else
                {
                    WriteLine($"  {resultName} = call {returnType} @{sanitizedName}({argList})");
                }
            }
            else
            {
                WriteLine($"  call {returnType} @{sanitizedName}({argList})");
            }
        }

        private string EmitStdLibCall(string funcName, List<IRValue> argValues, List<string> args)
        {
            var lower = funcName.ToLower();

            switch (lower)
            {
                case "printline":
                    if (argValues.Count > 0)
                    {
                        var arg = argValues[0];
                        var argType = MapType(arg.Type);

                        if (arg is IRConstant c && c.Value is string str)
                        {
                            if (_stringConstants.TryGetValue(str, out var id))
                            {
                                var len = str.Length + 1;
                                return $"call i32 @puts(i8* getelementptr inbounds ([{len} x i8], [{len} x i8]* @.str.{id}, i64 0, i64 0))";
                            }
                        }

                        // For non-string types, use printf with format
                        var formatId = GetOrCreateFormatString(argType);
                        var argVal = args[0];
                        return $"call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fmt.{formatId}, i64 0, i64 0), {argVal})";
                    }
                    return null;

                case "print":
                    if (args.Count > 0)
                    {
                        var argType = MapType(argValues[0].Type);
                        var formatId = GetOrCreateFormatString(argType, newline: false);
                        return $"call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @.fmt.{formatId}, i64 0, i64 0), {args[0]})";
                    }
                    return null;

                case "sqrt":
                    return $"call double @sqrt({args[0]})";
                case "pow":
                    return $"call double @pow({args[0]}, {args[1]})";
                case "sin":
                    return $"call double @sin({args[0]})";
                case "cos":
                    return $"call double @cos({args[0]})";
                case "tan":
                    return $"call double @tan({args[0]})";
                case "log":
                    return $"call double @log({args[0]})";
                case "exp":
                    return $"call double @exp({args[0]})";
                case "floor":
                    return $"call double @floor({args[0]})";
                case "ceiling":
                    return $"call double @ceil({args[0]})";
                case "abs":
                    return $"call double @fabs({args[0]})";
                case "rnd":
                    // rand() returns int, convert to double in [0,1)
                    return "call i32 @rand()"; // Caller needs to convert
                case "randomize":
                    return "call void @srand(i32 0)"; // Simplified

                default:
                    return null;
            }
        }

        private Dictionary<string, int> _formatStrings = new Dictionary<string, int>();
        private int _formatCounter = 0;

        private int GetOrCreateFormatString(string llvmType, bool newline = true)
        {
            var key = $"{llvmType}_{newline}";
            if (_formatStrings.TryGetValue(key, out var id))
                return id;

            id = _formatCounter++;
            _formatStrings[key] = id;

            // We'll need to add these format strings to the output
            // For now, assume they exist
            return id;
        }

        public override void Visit(IRReturn ret)
        {
            if (ret.Value != null)
            {
                var value = GetLLVMName(ret.Value);
                var type = MapType(ret.Value.Type);
                value = LoadIfNeeded(ret.Value, value);
                WriteLine($"  ret {type} {value}");
            }
            else
            {
                WriteLine("  ret void");
            }
        }

        public override void Visit(IRBranch branch)
        {
            var target = SanitizeLLVMLabel(branch.Target.Name);
            WriteLine($"  br label %{target}");
        }

        public override void Visit(IRConditionalBranch condBranch)
        {
            var condition = GetLLVMName(condBranch.Condition);
            condition = LoadIfNeeded(condBranch.Condition, condition);

            var trueTarget = SanitizeLLVMLabel(condBranch.TrueTarget.Name);
            var falseTarget = SanitizeLLVMLabel(condBranch.FalseTarget.Name);

            WriteLine($"  br i1 {condition}, label %{trueTarget}, label %{falseTarget}");
        }

        public override void Visit(IRSwitch switchInst)
        {
            var value = GetLLVMName(switchInst.Value);
            value = LoadIfNeeded(switchInst.Value, value);
            var type = MapType(switchInst.Value.Type);
            var defaultTarget = SanitizeLLVMLabel(switchInst.DefaultTarget.Name);

            var cases = new StringBuilder();
            foreach (var (caseValue, target) in switchInst.Cases)
            {
                var caseVal = GetLLVMName(caseValue);
                var caseTarget = SanitizeLLVMLabel(target.Name);
                cases.Append($" {type} {caseVal}, label %{caseTarget}");
            }

            WriteLine($"  switch {type} {value}, label %{defaultTarget} [{cases}]");
        }

        public override void Visit(IRPhi phi)
        {
            var result = GetLLVMName(phi);
            var type = MapType(phi.Type);

            var incomingValues = new List<string>();
            foreach (var (value, block) in phi.Operands)
            {
                var val = GetLLVMName(value);
                var blockLabel = SanitizeLLVMLabel(block.Name);
                incomingValues.Add($"[ {val}, %{blockLabel} ]");
            }

            WriteLine($"  {result} = phi {type} {string.Join(", ", incomingValues)}");
        }

        public override void Visit(IRAlloca alloca)
        {
            var result = GetLLVMName(alloca);
            var type = MapType(alloca.Type);

            if (alloca.Size > 1)
            {
                WriteLine($"  {result} = alloca {type}, i32 {alloca.Size}");
            }
            else
            {
                WriteLine($"  {result} = alloca {type}");
            }
        }

        public override void Visit(IRGetElementPtr gep)
        {
            var result = GetLLVMName(gep);
            var basePtr = GetLLVMName(gep.BasePointer);
            var baseType = MapType(gep.BasePointer.Type);

            // Remove pointer suffix for element type
            var elementType = baseType.EndsWith("*") ? baseType.Substring(0, baseType.Length - 1) : baseType;

            var indices = string.Join(", ", gep.Indices.Select(i =>
            {
                var idx = GetLLVMName(i);
                idx = LoadIfNeeded(i, idx);
                return $"i64 {idx}";
            }));

            WriteLine($"  {result} = getelementptr inbounds {elementType}, {baseType} {basePtr}, {indices}");
        }

        public override void Visit(IRCast cast)
        {
            var result = GetLLVMName(cast);
            var value = GetLLVMName(cast.Value);
            value = LoadIfNeeded(cast.Value, value);

            var fromType = MapType(cast.Value.Type);
            var toType = MapType(cast.Type);

            var castOp = GetCastOperation(cast.Value.Type, cast.Type);
            WriteLine($"  {result} = {castOp} {fromType} {value} to {toType}");
        }

        private string GetCastOperation(TypeInfo fromType, TypeInfo toType)
        {
            var fromFloat = IsFloatType(fromType);
            var toFloat = IsFloatType(toType);
            var fromSize = GetTypeSize(fromType);
            var toSize = GetTypeSize(toType);

            if (fromFloat && !toFloat) return "fptosi";
            if (!fromFloat && toFloat) return "sitofp";
            if (fromFloat && toFloat)
            {
                return fromSize < toSize ? "fpext" : "fptrunc";
            }

            // Integer conversions
            if (fromSize < toSize) return "sext";
            if (fromSize > toSize) return "trunc";
            return "bitcast";
        }

        private int GetTypeSize(TypeInfo type)
        {
            if (type == null) return 32;
            var name = type.Name?.ToLower() ?? "";
            return name switch
            {
                "byte" or "char" or "boolean" => 8,
                "short" => 16,
                "integer" or "single" or "float" => 32,
                "long" or "double" => 64,
                _ => 32
            };
        }

        public override void Visit(IRLabel label)
        {
            WriteLine($"{SanitizeLLVMLabel(label.Name)}:");
        }

        public override void Visit(IRComment comment)
        {
            if (_options.GenerateComments)
            {
                WriteLine($"  ; {comment.Text}");
            }
        }

        public override void Visit(IRArrayAlloc arrayAlloc)
        {
            var elementType = MapType(arrayAlloc.ElementType);
            var result = $"%{SanitizeLLVMName(arrayAlloc.Name)}";
            WriteLine($"  {result} = alloca [{arrayAlloc.Size} x {elementType}]");
        }

        public override void Visit(IRArrayStore arrayStore)
        {
            var arrayName = arrayStore.Array is IRVariable v ? SanitizeLLVMName(v.Name) : arrayStore.Array.Name;
            var elementType = MapType(arrayStore.Array.Type?.ElementType ?? new TypeInfo("i32", TypeKind.Primitive));
            var indexVal = arrayStore.Index is IRConstant ic ? $"i32 {ic.Value}" : $"i32 {GetLLVMName(arrayStore.Index)}";
            var valueVal = arrayStore.Value is IRConstant vc ? $"{vc.Value}" : GetLLVMName(arrayStore.Value);
            var gep = $"%t{_tempCounter++}";
            var arraySize = (arrayStore.Array as IRArrayAlloc)?.Size ?? 0;
            WriteLine($"  {gep} = getelementptr [{arraySize} x {elementType}], [{arraySize} x {elementType}]* %{arrayName}, i32 0, {indexVal}");
            WriteLine($"  store {elementType} {valueVal}, {elementType}* {gep}");
        }

        public override void Visit(IRAwait awaitInst)
        {
            // LLVM doesn't have native async/await, generate a comment
            WriteLine($"  ; await - LLVM async not supported");
        }

        public override void Visit(IRYield yieldInst)
        {
            // LLVM doesn't have native yield, generate a comment
            if (yieldInst.IsBreak)
                WriteLine("  ; yield break - LLVM iterators not supported");
            else
                WriteLine($"  ; yield return - LLVM iterators not supported");
        }

        public override void Visit(IRNewObject newObj)
        {
            // LLVM OOP requires struct allocation
            WriteLine($"  ; new {newObj.ClassName}() - LLVM OOP not fully supported");
        }

        public override void Visit(IRInstanceMethodCall methodCall)
        {
            // LLVM would need vtable dispatch
            WriteLine($"  ; {methodCall.Object?.Name}.{methodCall.MethodName}() - LLVM method calls not fully supported");
        }

        public override void Visit(IRBaseMethodCall baseCall)
        {
            // LLVM doesn't have base class concept directly
            WriteLine($"  ; base.{baseCall.MethodName}() - LLVM base calls not supported");
        }

        public override void Visit(IRFieldAccess fieldAccess)
        {
            // LLVM would need GEP for struct field access
            WriteLine($"  ; {fieldAccess.Object?.Name}.{fieldAccess.FieldName} - LLVM field access not fully supported");
        }

        #endregion

        private string LoadIfNeeded(IRValue value, string llvmValue)
        {
            // If this is a variable reference, we need to load from its address
            if (value is IRVariable variable && _declaredIdentifiers.Contains(variable.Name))
            {
                var type = MapType(variable.Type);
                var loadResult = $"%t{_tempCounter++}";
                var varName = SanitizeLLVMName(variable.Name);
                WriteLine($"  {loadResult} = load {type}, {type}* %{varName}.addr");
                return loadResult;
            }
            return llvmValue;
        }

        private void WriteLine(string text = "")
        {
            _output.AppendLine(text);
        }
    }

    public class LLVMCodeGenOptions
    {
        public bool GenerateComments { get; set; } = true;
        public bool OptimizationEnabled { get; set; } = false;
        public string TargetTriple { get; set; } = "x86_64-pc-windows-msvc";
    }
}
