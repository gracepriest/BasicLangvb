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

        public override string BackendName => "C++";
        public override TargetPlatform Target => TargetPlatform.Cpp;

        public CppCodeGenerator(CppCodeGenOptions options = null)
        {
            _output = new StringBuilder();
            _options = options ?? new CppCodeGenOptions();
            _allTemporaries = new HashSet<IRValue>();
            _headerIncludes = new List<string> { "iostream", "vector", "string", "memory" };
            _typeMapper = new CppTypeMapper();
        }
        
        public override string Generate(IRModule module)
        {
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
            WriteLine("#include <iostream>");
            WriteLine("#include <vector>");
            WriteLine("#include <string>");
            WriteLine("#include <memory>");
            
            foreach (var include in _headerIncludes)
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
            _tempCounter = 0;
            
            // Collect temporaries
            foreach (var block in function.Blocks)
            {
                foreach (var instruction in block.Instructions)
                {
                    if (instruction is IRValue value && !(value is IRConstant))
                    {
                        _allTemporaries.Add(value);
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
            
            // Declare temporaries with proper typing
            var tempsByType = _allTemporaries
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
            var address = GetValueName(store.Address);
            
            WriteLine($"*{address} = {value};");
        }
        
        public override void Visit(IRCall call)
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
