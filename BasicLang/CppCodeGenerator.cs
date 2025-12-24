using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Compiler.CodeGen.CPlusPlus
{
    /// <summary>
    /// C++ code generator - transpiles IR to C++
    /// Targets C++17 for modern language features
    /// </summary>
    public class CppCodeGenerator : CodeGeneratorBase
    {
        private readonly StringBuilder _output;
        private readonly CppCodeGenOptions _options;
        private readonly HashSet<IRValue> _allTemporaries;
        private readonly List<string> _headerIncludes;
        private readonly HashSet<string> _declaredIdentifiers;
        private IRModule _module;

        public override string BackendName => "C++";
        public override TargetPlatform Target => TargetPlatform.Cpp;

        public CppCodeGenerator(CppCodeGenOptions options = null)
        {
            _output = new StringBuilder();
            _options = options ?? new CppCodeGenOptions();
            _allTemporaries = new HashSet<IRValue>();
            _headerIncludes = new List<string> { "iostream", "vector", "string", "memory" };
            _declaredIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _typeMapper = new CppTypeMapper();
        }
        
        public override string Generate(IRModule module)
        {
            _module = module;
            _output.Clear();
            _valueNames.Clear();
            _allTemporaries.Clear();
            _tempCounter = 0;
            
            // Generate header
            GenerateHeader(module);
            
            // Generate global variables
            if (module.GlobalVariables.Count > 0)
            {
                WriteLine("// Global variables");
                foreach (var globalVar in module.GlobalVariables.Values)
                {
                    var type = MapType(globalVar.Type);
                    var name = SanitizeName(globalVar.Name);
                    WriteLine($"{type} {name} = {{}};");
                }
                WriteLine();
            }
            
            // Generate function declarations
            WriteLine("// Function declarations");
            foreach (var function in module.Functions)
            {
                if (!function.IsExternal)
                {
                    GenerateFunctionDeclaration(function);
                }
            }
            WriteLine();
            
            // Generate function implementations
            WriteLine("// Function implementations");
            foreach (var function in module.Functions)
            {
                if (!function.IsExternal)
                {
                    GenerateFunction(function);
                    WriteLine();
                }
            }
            
            // Generate main function if requested
            if (_options.GenerateMainFunction)
            {
                GenerateMainFunction(module);
            }
            
            return _output.ToString();
        }
        
        private void GenerateHeader(IRModule module)
        {
            WriteLine("#pragma once");

            // Collect unique includes
            var includes = new HashSet<string> { "iostream", "vector", "string", "cstdint", "cmath", "algorithm", "cstdlib", "ctime" };
            foreach (var inc in _headerIncludes)
            {
                includes.Add(inc);
            }

            foreach (var include in includes)
            {
                WriteLine($"#include <{include}>");
            }

            WriteLine();
            WriteLine("using namespace std;");
            WriteLine();
        }
        
        private void GenerateFunctionDeclaration(IRFunction function)
        {
            var returnType = MapType(function.ReturnType);
            var functionName = SanitizeName(function.Name);
            
            var parameters = string.Join(", ",
                function.Parameters.Select(p => $"{MapType(p.Type)} {GetValueName(p)}"));
            
            WriteLine($"{returnType} {functionName}({parameters});");
        }
        
        private void GenerateFunction(IRFunction function)
        {
            _currentFunction = function;
            _allTemporaries.Clear();
            _valueNames.Clear();
            _declaredIdentifiers.Clear();
            _tempCounter = 0;

            // Collect declared identifiers (parameters and locals)
            foreach (var param in function.Parameters)
                _declaredIdentifiers.Add(param.Name);

            foreach (var local in function.LocalVariables)
                _declaredIdentifiers.Add(local.Name);

            // Collect temporaries (values that aren't named destinations)
            foreach (var block in function.Blocks)
            {
                foreach (var instruction in block.Instructions)
                {
                    if (instruction is IRValue value && !(value is IRConstant))
                    {
                        // Skip values that are named destinations (they'll be emitted directly)
                        if (!IsNamedDestination(value))
                        {
                            _allTemporaries.Add(value);
                        }
                    }
                }
            }
            
            // Generate signature
            var returnType = MapType(function.ReturnType);
            var functionName = SanitizeName(function.Name);
            var parameters = string.Join(", ",
                function.Parameters.Select(p => $"{MapType(p.Type)} {GetValueName(p)}"));
            
            WriteLine($"{returnType} {functionName}({parameters})");
            WriteLine("{");
            Indent();

            // Declare local variables
            foreach (var local in function.LocalVariables)
            {
                var localType = MapType(local.Type);
                var localName = SanitizeName(local.Name);
                if (localType != "void")
                {
                    var defaultVal = GetDefaultValue(local.Type);
                    WriteLine($"{localType} {localName} = {defaultVal};");
                }
            }

            if (function.LocalVariables.Count > 0)
                WriteLine();

            // Declare temporaries with proper typing (skip void types)
            var tempsByType = _allTemporaries
                .Where(t => t.Type?.Name != "Void" && MapType(t.Type) != "void")
                .GroupBy(t => MapType(t.Type))
                .ToList();

            foreach (var group in tempsByType)
            {
                var cppType = group.Key;
                var tempNames = group.Select(GetValueName).Distinct();

                foreach (var tempName in tempNames)
                {
                    WriteLine($"{cppType} {tempName} = {{}};");
                }
            }

            if (_allTemporaries.Count > 0)
                WriteLine();
            
            // Generate body
            if (function.EntryBlock != null)
            {
                GenerateBlock(function.EntryBlock, new HashSet<BasicBlock>());
            }
            
            Unindent();
            WriteLine("}");
        }
        
        private void GenerateBlock(BasicBlock block, HashSet<BasicBlock> visited)
        {
            if (visited.Contains(block)) return;
            visited.Add(block);
            
            // Label (if needed)
            if (block.Predecessors.Count > 1 || block != _currentFunction.EntryBlock)
            {
                Unindent();
                WriteLine($"{block.Name}:");
                Indent();
            }
            
            // Instructions
            foreach (var instruction in block.Instructions)
            {
                instruction.Accept(this);
            }
            
            // Process successors
            foreach (var successor in block.Successors.Where(s => !visited.Contains(s)))
            {
                GenerateBlock(successor, visited);
            }
        }
        
        private void GenerateMainFunction(IRModule module)
        {
            WriteLine("int main(int argc, char* argv[])");
            WriteLine("{");
            Indent();
            
            var mainFunc = module.Functions.FirstOrDefault(f =>
                f.Name.Equals("Main", StringComparison.OrdinalIgnoreCase));
            
            if (mainFunc != null)
            {
                var functionName = SanitizeName(mainFunc.Name);
                if (mainFunc.ReturnType.Name == "Void")
                {
                    WriteLine($"{functionName}();");
                }
                else
                {
                    WriteLine($"auto result = {functionName}();");
                    WriteLine("cout << result << endl;");
                }
            }
            else
            {
                WriteLine("cout << \"No Main function found\" << endl;");
            }
            
            WriteLine("return 0;");
            Unindent();
            WriteLine("}");
        }
        
        protected override void InitializeTypeMap()
        {
            base.InitializeTypeMap();
            
            // C++ specific mappings
            _typeMap["Integer"] = "int32_t";
            _typeMap["Long"] = "int64_t";
            _typeMap["Single"] = "float";
            _typeMap["Double"] = "double";
            _typeMap["String"] = "std::string";
            _typeMap["Boolean"] = "bool";
            _typeMap["Char"] = "char";
            _typeMap["Void"] = "void";
            _typeMap["Object"] = "void*";
        }
        
        #region Visitor Methods
        
        public override void Visit(IRFunction function) { }
        public override void Visit(BasicBlock block) { }
        public override void Visit(IRConstant constant) { }
        public override void Visit(IRVariable variable) { }
        
        public override void Visit(IRBinaryOp binaryOp)
        {
            var left = GetValueName(binaryOp.Left);
            var right = GetValueName(binaryOp.Right);
            var op = MapBinaryOperator(binaryOp.Operation);
            var result = GetValueName(binaryOp);
            
            WriteLine($"{result} = {left} {op} {right};");
        }
        
        public override void Visit(IRUnaryOp unaryOp)
        {
            var operand = GetValueName(unaryOp.Operand);
            var op = MapUnaryOperator(unaryOp.Operation);
            var result = GetValueName(unaryOp);
            
            if (unaryOp.Operation == UnaryOpKind.Inc || unaryOp.Operation == UnaryOpKind.Dec)
            {
                WriteLine($"{result}{op};");
            }
            else
            {
                WriteLine($"{result} = {op}{operand};");
            }
        }
        
        public override void Visit(IRCompare compare)
        {
            var left = GetValueName(compare.Left);
            var right = GetValueName(compare.Right);
            var op = MapCompareOperator(compare.Comparison);
            var result = GetValueName(compare);
            
            WriteLine($"{result} = {left} {op} {right};");
        }
        
        public override void Visit(IRAssignment assignment)
        {
            var value = GetValueName(assignment.Value);
            var target = GetValueName(assignment.Target);
            
            WriteLine($"{target} = {value};");
        }
        
        public override void Visit(IRLoad load)
        {
            var address = GetValueName(load.Address);
            var result = GetValueName(load);
            
            WriteLine($"{result} = *{address};");
        }
        
        public override void Visit(IRStore store)
        {
            var value = GetValueName(store.Value);

            // For IRVariable addresses (local variables), emit as simple assignment
            if (store.Address is IRVariable variable)
            {
                var varName = SanitizeName(variable.Name);
                WriteLine($"{varName} = {value};");
                return;
            }

            // Array element store
            if (store.Address is IRGetElementPtr gep)
            {
                var baseExpr = GetValueName(gep.BasePointer);
                var index = GetValueName(gep.Indices[0]);
                WriteLine($"{baseExpr}[{index}] = {value};");
                return;
            }

            var address = GetValueName(store.Address);

            // Check if this is a pointer store or a regular assignment
            if (store.Address.Type?.Kind == TypeKind.Pointer)
            {
                WriteLine($"*{address} = {value};");
            }
            else
            {
                // Regular variable assignment
                WriteLine($"{address} = {value};");
            }
        }
        
        public override void Visit(IRCall call)
        {
            var args = call.Arguments.Select(GetValueName).ToList();
            var functionName = call.FunctionName;
            var hasReturn = call.Type != null && !call.Type.Name.Equals("Void", StringComparison.OrdinalIgnoreCase);

            // Check if this is an extern function call
            if (_module != null && _module.IsExtern(functionName))
            {
                var externDecl = _module.GetExtern(functionName);
                if (externDecl != null && externDecl.HasImplementation("Cpp"))
                {
                    var impl = externDecl.GetImplementation("Cpp");
                    var argsArr = args.ToArray();

                    string externCall;
                    if (impl.Contains("{"))
                    {
                        externCall = string.Format(impl, argsArr);
                    }
                    else
                    {
                        externCall = $"{impl}({string.Join(", ", args)})";
                    }

                    if (hasReturn && IsNamedDestination(call))
                    {
                        var target = SanitizeName(call.Name);
                        WriteLine($"{target} = {externCall};");
                    }
                    else
                    {
                        WriteLine($"{externCall};");
                    }
                    return;
                }
            }

            // Handle standard library calls
            var stdlibCall = EmitStdLibCall(functionName, args);
            if (stdlibCall != null)
            {
                if (hasReturn && IsNamedDestination(call))
                {
                    var target = SanitizeName(call.Name);
                    WriteLine($"{target} = {stdlibCall};");
                }
                else
                {
                    WriteLine($"{stdlibCall};");
                }
                return;
            }

            // Regular function call
            var sanitizedName = SanitizeName(functionName);
            var argsStr = string.Join(", ", args);

            // If this call result is assigned to a declared variable, emit directly
            if (hasReturn && IsNamedDestination(call))
            {
                var target = SanitizeName(call.Name);
                WriteLine($"{target} = {sanitizedName}({argsStr});");
            }
            else if (hasReturn && !string.IsNullOrEmpty(call.Name))
            {
                // Otherwise use temp variable
                var result = GetValueName(call);
                WriteLine($"{result} = {sanitizedName}({argsStr});");
            }
            else
            {
                // Void call
                WriteLine($"{sanitizedName}({argsStr});");
            }
        }

        private string EmitStdLibCall(string functionName, List<string> args)
        {
            return functionName.ToLower() switch
            {
                "print" => $"cout << {args[0]}",
                "printline" => $"cout << {args[0]} << endl",
                "readline" => "([](){ string s; getline(cin, s); return s; })()",
                "len" => $"static_cast<int32_t>({args[0]}.length())",
                "left" => $"{args[0]}.substr(0, {args[1]})",
                "right" => $"{args[0]}.substr({args[0]}.length() - {args[1]})",
                "mid" => $"{args[0]}.substr({args[1]} - 1, {args[2]})",
                "trim" => $"([](string s){{ auto start = s.find_first_not_of(\" \\t\"); auto end = s.find_last_not_of(\" \\t\"); return start == string::npos ? \"\" : s.substr(start, end - start + 1); }})({args[0]})",
                "ucase" => $"([](string s){{ transform(s.begin(), s.end(), s.begin(), ::toupper); return s; }})({args[0]})",
                "lcase" => $"([](string s){{ transform(s.begin(), s.end(), s.begin(), ::tolower); return s; }})({args[0]})",
                "instr" => $"static_cast<int32_t>({args[0]}.find({args[1]}) + 1)",
                "replace" => $"([](string s, const string& from, const string& to){{ size_t pos = 0; while ((pos = s.find(from, pos)) != string::npos) {{ s.replace(pos, from.length(), to); pos += to.length(); }} return s; }})({args[0]}, {args[1]}, {args[2]})",
                "abs" => $"abs({args[0]})",
                "sqrt" => $"sqrt({args[0]})",
                "pow" => $"pow({args[0]}, {args[1]})",
                "sin" => $"sin({args[0]})",
                "cos" => $"cos({args[0]})",
                "tan" => $"tan({args[0]})",
                "log" => $"log({args[0]})",
                "exp" => $"exp({args[0]})",
                "floor" => $"floor({args[0]})",
                "ceiling" => $"ceil({args[0]})",
                "round" => $"round({args[0]})",
                "min" => $"min({args[0]}, {args[1]})",
                "max" => $"max({args[0]}, {args[1]})",
                "cint" => $"static_cast<int32_t>({args[0]})",
                "clng" => $"static_cast<int64_t>({args[0]})",
                "cdbl" => $"static_cast<double>({args[0]})",
                "csng" => $"static_cast<float>({args[0]})",
                "cstr" => $"to_string({args[0]})",
                "cbool" => $"static_cast<bool>({args[0]})",
                "ubound" => $"(static_cast<int32_t>({args[0]}.size()) - 1)",
                "lbound" => "0",
                "rnd" => "(static_cast<double>(rand()) / RAND_MAX)",
                "randomize" => "srand(time(nullptr))",
                _ => null
            };
        }
        
        public override void Visit(IRReturn ret)
        {
            if (ret.Value != null)
            {
                var value = GetValueName(ret.Value);
                WriteLine($"return {value};");
            }
            else
            {
                WriteLine("return;");
            }
        }
        
        public override void Visit(IRBranch branch)
        {
            WriteLine($"goto {branch.Target.Name};");
        }
        
        public override void Visit(IRConditionalBranch condBranch)
        {
            var condition = GetValueName(condBranch.Condition);
            
            WriteLine($"if ({condition}) {{");
            Indent();
            WriteLine($"goto {condBranch.TrueTarget.Name};");
            Unindent();
            WriteLine("}");
            WriteLine($"else {{");
            Indent();
            WriteLine($"goto {condBranch.FalseTarget.Name};");
            Unindent();
            WriteLine("}");
        }
        
        public override void Visit(IRSwitch switchInst)
        {
            var value = GetValueName(switchInst.Value);
            
            WriteLine($"switch ({value}) {{");
            Indent();
            
            foreach (var (caseValue, target) in switchInst.Cases)
            {
                var caseVal = GetValueName(caseValue);
                WriteLine($"case {caseVal}: goto {target.Name};");
            }
            
            WriteLine($"default: goto {switchInst.DefaultTarget.Name};");
            
            Unindent();
            WriteLine("}");
        }
        
        public override void Visit(IRPhi phi)
        {
            WriteLine($"// Phi node: {phi.Name}");
        }
        
        public override void Visit(IRAlloca alloca)
        {
            // Memory allocation handled in declarations
        }
        
        public override void Visit(IRGetElementPtr gep)
        {
            var basePtr = GetValueName(gep.BasePointer);
            var result = GetValueName(gep);
            
            if (gep.Indices.Count == 1)
            {
                var index = GetValueName(gep.Indices[0]);
                WriteLine($"{result} = &{basePtr}[{index}];");
            }
            else
            {
                var indices = string.Join("][", gep.Indices.Select(GetValueName));
                WriteLine($"{result} = &{basePtr}[{indices}];");
            }
        }
        
        public override void Visit(IRCast cast)
        {
            var value = GetValueName(cast.Value);
            var targetType = MapType(cast.Type);
            var result = GetValueName(cast);
            
            WriteLine($"{result} = static_cast<{targetType}>({value});");
        }
        
        public override void Visit(IRLabel label)
        {
            Unindent();
            WriteLine($"{label.Name}:");
            Indent();
        }
        
        public override void Visit(IRComment comment)
        {
            if (_options.GenerateComments)
            {
                WriteLine($"// {comment.Text}");
            }
        }

        public override void Visit(IRArrayAlloc arrayAlloc)
        {
            var elementType = MapType(arrayAlloc.ElementType);
            WriteLine($"{elementType} {arrayAlloc.Name}[{arrayAlloc.Size}];");
        }

        public override void Visit(IRArrayStore arrayStore)
        {
            var arrayName = arrayStore.Array.Name;
            var indexVal = arrayStore.Index is IRConstant c ? c.Value.ToString() : GetValueName(arrayStore.Index);
            var valueVal = arrayStore.Value is IRConstant vc ? EmitConstant(vc) : GetValueName(arrayStore.Value);
            WriteLine($"{arrayName}[{indexVal}] = {valueVal};");
        }

        public override void Visit(IRAwait awaitInst)
        {
            // C++ doesn't have native async/await, generate a comment
            WriteLine($"// await {awaitInst.Expression?.Name ?? "expression"} - C++ async not supported");
        }

        public override void Visit(IRYield yieldInst)
        {
            // C++ doesn't have native yield, generate a comment
            if (yieldInst.IsBreak)
                WriteLine("// yield break - C++ iterators not supported");
            else
                WriteLine($"// yield return {yieldInst.Value?.Name ?? "value"} - C++ iterators not supported");
        }

        public override void Visit(IRNewObject newObj)
        {
            var className = SanitizeName(newObj.ClassName);
            var args = string.Join(", ", newObj.Arguments.Select(a => GetValueName(a)));
            var type = MapType(newObj.Type);
            WriteLine($"{type} {newObj.Name} = {className}({args});");
        }

        public override void Visit(IRInstanceMethodCall methodCall)
        {
            var obj = GetValueName(methodCall.Object);
            var methodName = SanitizeName(methodCall.MethodName);
            var args = string.Join(", ", methodCall.Arguments.Select(a => GetValueName(a)));
            if (methodCall.Type == null || methodCall.Type.Name == "Void")
                WriteLine($"{obj}.{methodName}({args});");
            else
                WriteLine($"auto {methodCall.Name} = {obj}.{methodName}({args});");
        }

        public override void Visit(IRBaseMethodCall baseCall)
        {
            var methodName = SanitizeName(baseCall.MethodName);
            var args = string.Join(", ", baseCall.Arguments.Select(a => GetValueName(a)));
            // C++ uses explicit base class name
            WriteLine($"// base.{methodName}({args}) - C++ needs explicit base class name");
        }

        public override void Visit(IRFieldAccess fieldAccess)
        {
            var obj = GetValueName(fieldAccess.Object);
            var fieldName = SanitizeName(fieldAccess.FieldName);
            var type = MapType(fieldAccess.Type);
            WriteLine($"{type} {fieldAccess.Name} = {obj}.{fieldName};");
        }

        #endregion
        
        private string MapBinaryOperator(BinaryOpKind op) => op switch
        {
            BinaryOpKind.Add => "+",
            BinaryOpKind.Sub => "-",
            BinaryOpKind.Mul => "*",
            BinaryOpKind.Div => "/",
            BinaryOpKind.Mod => "%",
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

        private bool IsNamedDestination(IRValue value)
        {
            if (value == null) return false;
            if (string.IsNullOrEmpty(value.Name)) return false;
            return _declaredIdentifiers.Contains(value.Name);
        }

        private string GetDefaultValue(TypeInfo type)
        {
            if (type == null) return "{}";

            var typeName = type.Name?.ToLower() ?? "";

            return typeName switch
            {
                "integer" or "int" => "0",
                "long" => "0LL",
                "single" or "float" => "0.0f",
                "double" => "0.0",
                "boolean" or "bool" => "false",
                "char" => "'\\0'",
                "string" => "\"\"",
                _ when type.Kind == TypeKind.Array => "{}",
                _ when type.Kind == TypeKind.Pointer => "nullptr",
                _ => "{}"
            };
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
    }
    
    public class CppCodeGenOptions
    {
        public int IndentSize { get; set; } = 4;
        public bool GenerateComments { get; set; } = true;
        public bool GenerateMainFunction { get; set; } = true;
        public string Namespace { get; set; } = "BasicLang";
    }
}
