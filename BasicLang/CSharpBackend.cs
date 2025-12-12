using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Compiler.CodeGen.CSharp
{
    /// <summary>
    /// Improved C# code generator with two-pass approach for better temporary handling
    /// </summary>
    public class ImprovedCSharpCodeGenerator : IIRVisitor
    {
        private readonly StringBuilder _output;
        private readonly CodeGenOptions _options;
        private int _indentLevel;
        private readonly Dictionary<string, string> _typeMap;
        private readonly HashSet<string> _usings;
        private IRFunction _currentFunction;

        // Two-pass approach: first collect all temporaries, then generate code
        private readonly HashSet<IRValue> _allTemporaries;
        private readonly Dictionary<IRValue, string> _valueNames;
        private readonly Dictionary<string, string> _variableNameMap; // Maps variable names to sanitized names
        private int _tempCounter;

        public string GeneratedCode => _output.ToString();

        public ImprovedCSharpCodeGenerator(CodeGenOptions options = null)
        {
            _output = new StringBuilder();
            _options = options ?? new CodeGenOptions();
            _indentLevel = 0;
            _usings = new HashSet<string>();
            _allTemporaries = new HashSet<IRValue>();
            _valueNames = new Dictionary<IRValue, string>();
            _variableNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _tempCounter = 0;

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

            // Add default usings
            _usings.Add("System");
        }

        /// <summary>
        /// Generate C# code from IR module
        /// </summary>
        public string Generate(IRModule module)
        {
            _output.Clear();
            _indentLevel = 0;
            _usings.Clear();

            // Add default usings
            _usings.Add("System");
            _usings.Add("System.Collections.Generic");

            // Generate using directives
            foreach (var usingDirective in _usings.OrderBy(u => u))
            {
                WriteLine($"using {usingDirective};");
            }
            WriteLine();

            // Generate namespace
            WriteLine($"namespace {_options.Namespace}");
            WriteLine("{");
            Indent();

            // Generate main class
            WriteLine($"{_options.ClassAccessModifier} class {_options.ClassName}");
            WriteLine("{");
            Indent();

            // Generate global variables
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

            // Generate functions
            bool hasUserMain = false;
            foreach (var function in module.Functions)
            {
                if (!function.IsExternal)
                {
                    GenerateFunction(function);
                    WriteLine();

                    // Track if user defined Main (Sub Main or Function Main)
                    if (function.Name.Equals("Main", StringComparison.OrdinalIgnoreCase))
                    {
                        hasUserMain = true;
                    }
                }
            }

            // Generate Main entry point if requested and user didn't define one
            if (_options.GenerateMainMethod && !hasUserMain)
            {
                GenerateMainMethod(module);
            }

            Unindent();
            WriteLine("}"); // End class

            Unindent();
            WriteLine("}"); // End namespace

            return _output.ToString();
        }

        private void GenerateMainMethod(IRModule module)
        {
            // This method is only called if no user-defined Main exists
            // Generate a default entry point
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
            _allTemporaries.Clear();
            _valueNames.Clear();
            _variableNameMap.Clear();
            _tempCounter = 0;

            // Map parameters first
            foreach (var param in function.Parameters)
            {
                var sanitizedName = SanitizeName(param.Name);
                _valueNames[param] = sanitizedName;
                _variableNameMap[param.Name] = sanitizedName;
            }

            // Map local variables (from Dim statements)
            foreach (var localVar in function.LocalVariables)
            {
                var sanitizedName = SanitizeName(localVar.Name);
                _valueNames[localVar] = sanitizedName;
                _variableNameMap[localVar.Name] = sanitizedName;
            }

            // Pass 1: Collect all temporaries and assign names
            // (must happen AFTER local variables are mapped)
            CollectTemporaries(function);

            // Generate function signature
            var returnType = MapType(function.ReturnType);
            var functionName = SanitizeName(function.Name);

            var paramList = new List<string>();
            foreach (var p in function.Parameters)
            {
                var paramType = MapType(p.Type);
                var paramName = SanitizeName(p.Name);
                paramList.Add($"{paramType} {paramName}");
            }
            var parameters = string.Join(", ", paramList);

            WriteLine($"{_options.MethodAccessModifier} static {returnType} {functionName}({parameters})");

            WriteLine("{");
            Indent();

            // Declare local variables (from Dim statements)
            var declaredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var localVar in function.LocalVariables)
            {
                var varName = GetValueName(localVar);
                if (!declaredNames.Contains(varName))
                {
                    var csharpType = MapType(localVar.Type);
                    WriteLine($"{csharpType} {varName} = default({csharpType});");
                    declaredNames.Add(varName);
                }
            }

            // Declare all temporaries (but skip local variables we already declared)
            var tempsByType = _allTemporaries
                .GroupBy(t => t.Type?.Name ?? "object")
                .ToList();

            foreach (var group in tempsByType)
            {
                var csharpType = MapType(group.First().Type);
                var tempNames = group.Select(GetValueName).Distinct();

                foreach (var tempName in tempNames)
                {
                    // Skip if already declared as a local variable
                    if (!declaredNames.Contains(tempName))
                    {
                        WriteLine($"{csharpType} {tempName} = default({csharpType});");
                        declaredNames.Add(tempName);
                    }
                }
            }

            if (function.LocalVariables.Count > 0 || _allTemporaries.Count > 0)
                WriteLine();

            // Pass 2: Generate code
            if (function.EntryBlock != null)
            {
                GenerateBlock(function.EntryBlock, new HashSet<BasicBlock>());
            }

            Unindent();
            WriteLine("}");

            _currentFunction = null;
        }

        private void CollectTemporaries(IRFunction function)
        {
            // Build set of local variable names to detect renamed temporaries
            var localVarNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var localVar in function.LocalVariables)
            {
                localVarNames.Add(localVar.Name);
            }

            // Also include parameter names
            foreach (var param in function.Parameters)
            {
                localVarNames.Add(param.Name);
            }

            foreach (var block in function.Blocks)
            {
                foreach (var instruction in block.Instructions)
                {
                    // Skip non-value instructions
                    if (!(instruction is IRValue value))
                        continue;

                    // Skip constants - they don't need declarations
                    if (value is IRConstant)
                        continue;

                    // Skip parameters - they're declared in the signature
                    if (value is IRVariable variable && variable.IsParameter)
                        continue;

                    // Skip if this value's Name matches a local variable or parameter
                    // (IRBuilder renames calls/ops to target variable to avoid extra assignments)
                    if (!string.IsNullOrEmpty(value.Name) && localVarNames.Contains(value.Name))
                    {
                        continue;
                    }

                    // Skip IRAssignment - we handle the target variable, not the assignment itself
                    if (instruction is IRAssignment)
                        continue;

                    _allTemporaries.Add(value);
                }
            }
        }

        private void GenerateBlock(BasicBlock block, HashSet<BasicBlock> visited)
        {
            if (visited.Contains(block))
                return;

            visited.Add(block);

            // Generate label if block has multiple predecessors
            if (block.Predecessors.Count > 1 || block != _currentFunction.EntryBlock)
            {
                Unindent();
                WriteLine($"{block.Name}:");
                Indent();
            }

            // Generate instructions
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

        private string GetValueName(IRValue value)
        {
            if (value is IRConstant constant)
            {
                return EmitConstant(constant);
            }

            // First try exact object reference lookup
            if (_valueNames.TryGetValue(value, out var name))
            {
                return name;
            }

            // For variables, try name-based lookup (handles case where different 
            // IRVariable objects represent the same logical variable)
            if (value is IRVariable variable)
            {
                if (_variableNameMap.TryGetValue(variable.Name, out var mappedName))
                {
                    // Cache the mapping for this object too
                    _valueNames[value] = mappedName;
                    return mappedName;
                }

                // Create new name
                name = SanitizeName(variable.Name);
                _variableNameMap[variable.Name] = name;
            }
            // For calls, binary ops, etc. - use their Name property if set
            // (IRBuilder may have renamed them to the target variable)
            else if (!string.IsNullOrEmpty(value.Name))
            {
                // Check if this Name corresponds to a known local variable
                // (IRBuilder renames call results to target variable)
                if (_variableNameMap.TryGetValue(value.Name, out var existingName))
                {
                    name = existingName;
                }
                else
                {
                    name = SanitizeName(value.Name);
                    _variableNameMap[value.Name] = name;
                }
            }
            else
            {
                name = $"t{_tempCounter++}";
            }

            _valueNames[value] = name;
            return name;
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
            var left = GetValueName(binaryOp.Left);
            var right = GetValueName(binaryOp.Right);
            var op = MapBinaryOperator(binaryOp.Operation);
            var result = GetValueName(binaryOp);

            WriteLine($"{result} = {left} {op} {right};");
        }

        public void Visit(IRUnaryOp unaryOp)
        {
            var operand = GetValueName(unaryOp.Operand);
            var op = MapUnaryOperator(unaryOp.Operation);
            var result = GetValueName(unaryOp);

            if (unaryOp.Operation == UnaryOpKind.Inc || unaryOp.Operation == UnaryOpKind.Dec)
            {
                WriteLine($"{result} = {operand};");
                WriteLine($"{result}{op};");
            }
            else
            {
                WriteLine($"{result} = {op}{operand};");
            }
        }

        public void Visit(IRCompare compare)
        {
            var left = GetValueName(compare.Left);
            var right = GetValueName(compare.Right);
            var op = MapCompareOperator(compare.Comparison);
            var result = GetValueName(compare);

            WriteLine($"{result} = {left} {op} {right};");
        }

        public void Visit(IRAssignment assignment)
        {
            var value = GetValueName(assignment.Value);
            var target = GetValueName(assignment.Target);

            WriteLine($"{target} = {value};");
        }

        public void Visit(IRLoad load)
        {
            var address = GetValueName(load.Address);
            var result = GetValueName(load);

            WriteLine($"{result} = {address};");
        }

        public void Visit(IRStore store)
        {
            var value = GetValueName(store.Value);
            var address = GetValueName(store.Address);

            WriteLine($"{address} = {value};");
        }

        public void Visit(IRCall call)
        {
            var args = string.Join(", ", call.Arguments.Select(GetValueName));
            var functionName = SanitizeName(call.FunctionName);

            if (call.Type != null && call.Type.Name != "Void" && !string.IsNullOrEmpty(call.Name))
            {
                var result = GetValueName(call);
                WriteLine($"{result} = {functionName}({args});");
            }
            else
            {
                WriteLine($"{functionName}({args});");
            }
        }

        public void Visit(IRReturn ret)
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

        public void Visit(IRBranch branch)
        {
            WriteLine($"goto {branch.Target.Name};");
        }

        public void Visit(IRConditionalBranch condBranch)
        {
            var condition = GetValueName(condBranch.Condition);

            WriteLine($"if ({condition})");
            WriteLine("{");
            Indent();
            WriteLine($"goto {condBranch.TrueTarget.Name};");
            Unindent();
            WriteLine("}");
            WriteLine("else");
            WriteLine("{");
            Indent();
            WriteLine($"goto {condBranch.FalseTarget.Name};");
            Unindent();
            WriteLine("}");
        }

        public void Visit(IRSwitch switchInst)
        {
            var value = GetValueName(switchInst.Value);

            WriteLine($"switch ({value})");
            WriteLine("{");
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

        public void Visit(IRPhi phi)
        {
            WriteLine($"// Phi node: {phi.Name}");
        }

        public void Visit(IRAlloca alloca)
        {
            // Handled in temporary declarations
        }

        public void Visit(IRGetElementPtr gep)
        {
            var basePtr = GetValueName(gep.BasePointer);
            var result = GetValueName(gep);

            if (gep.Indices.Count == 1)
            {
                var index = GetValueName(gep.Indices[0]);
                WriteLine($"{result} = {basePtr}[{index}];");
            }
            else
            {
                var indices = string.Join(", ", gep.Indices.Select(GetValueName));
                WriteLine($"{result} = {basePtr}[{indices}];");
            }
        }

        public void Visit(IRCast cast)
        {
            var value = GetValueName(cast.Value);
            var targetType = MapType(cast.Type);
            var result = GetValueName(cast);

            WriteLine($"{result} = ({targetType}){value};");
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
            {
                WriteLine($"// {comment.Text}");
            }
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
            {
                return csharpType;
            }

            if (type.Kind == TypeKind.Array && type.ElementType != null)
            {
                var elementType = MapType(type.ElementType);
                return $"{elementType}[]";
            }

            return type.Name;
        }

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

        private string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "_unnamed";

            var sanitized = new StringBuilder();

            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                {
                    sanitized.Append(ch);
                }
            }

            var result = sanitized.ToString();

            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            if (IsCSharpKeyword(result))
            {
                result = "@" + result;
            }

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

            return keywords.Contains(name.ToLower());
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

        private void Write(string text)
        {
            _output.Append(text);
        }

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

        private void Indent()
        {
            _indentLevel++;
        }

        private void Unindent()
        {
            if (_indentLevel > 0)
                _indentLevel--;
        }
    }
}