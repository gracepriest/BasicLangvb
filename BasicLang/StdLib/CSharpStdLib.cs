using System;
using System.Collections.Generic;

namespace BasicLang.Compiler.StdLib.CSharp
{
    /// <summary>
    /// C# implementation of standard library functions
    /// Maps BasicLang stdlib to .NET BCL calls
    /// </summary>
    public class CSharpStdLibProvider : IStdLibProvider, IStdIO, IStdString, IStdMath, IStdArray, IStdConversion
    {
        private static readonly Dictionary<string, StdLibFunction> _functions = new Dictionary<string, StdLibFunction>(StringComparer.OrdinalIgnoreCase)
        {
            // I/O
            ["Print"] = new StdLibFunction { Name = "Print", Category = StdLibCategory.IO, ParameterTypes = new[] { "Object" }, ReturnType = "Void" },
            ["PrintLine"] = new StdLibFunction { Name = "PrintLine", Category = StdLibCategory.IO, ParameterTypes = new[] { "Object" }, ReturnType = "Void" },
            ["Input"] = new StdLibFunction { Name = "Input", Category = StdLibCategory.IO, ParameterTypes = new[] { "String" }, ReturnType = "String" },
            ["ReadLine"] = new StdLibFunction { Name = "ReadLine", Category = StdLibCategory.IO, ParameterTypes = Array.Empty<string>(), ReturnType = "String" },

            // String
            ["Len"] = new StdLibFunction { Name = "Len", Category = StdLibCategory.String, ParameterTypes = new[] { "String" }, ReturnType = "Integer" },
            ["Mid"] = new StdLibFunction { Name = "Mid", Category = StdLibCategory.String, ParameterTypes = new[] { "String", "Integer", "Integer" }, ReturnType = "String" },
            ["Left"] = new StdLibFunction { Name = "Left", Category = StdLibCategory.String, ParameterTypes = new[] { "String", "Integer" }, ReturnType = "String" },
            ["Right"] = new StdLibFunction { Name = "Right", Category = StdLibCategory.String, ParameterTypes = new[] { "String", "Integer" }, ReturnType = "String" },
            ["UCase"] = new StdLibFunction { Name = "UCase", Category = StdLibCategory.String, ParameterTypes = new[] { "String" }, ReturnType = "String" },
            ["LCase"] = new StdLibFunction { Name = "LCase", Category = StdLibCategory.String, ParameterTypes = new[] { "String" }, ReturnType = "String" },
            ["Trim"] = new StdLibFunction { Name = "Trim", Category = StdLibCategory.String, ParameterTypes = new[] { "String" }, ReturnType = "String" },
            ["InStr"] = new StdLibFunction { Name = "InStr", Category = StdLibCategory.String, ParameterTypes = new[] { "String", "String" }, ReturnType = "Integer" },
            ["Replace"] = new StdLibFunction { Name = "Replace", Category = StdLibCategory.String, ParameterTypes = new[] { "String", "String", "String" }, ReturnType = "String" },

            // Math
            ["Abs"] = new StdLibFunction { Name = "Abs", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Sqrt"] = new StdLibFunction { Name = "Sqrt", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Pow"] = new StdLibFunction { Name = "Pow", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double", "Double" }, ReturnType = "Double" },
            ["Sin"] = new StdLibFunction { Name = "Sin", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Cos"] = new StdLibFunction { Name = "Cos", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Tan"] = new StdLibFunction { Name = "Tan", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Log"] = new StdLibFunction { Name = "Log", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Exp"] = new StdLibFunction { Name = "Exp", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Floor"] = new StdLibFunction { Name = "Floor", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Ceiling"] = new StdLibFunction { Name = "Ceiling", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Round"] = new StdLibFunction { Name = "Round", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double" }, ReturnType = "Double" },
            ["Min"] = new StdLibFunction { Name = "Min", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double", "Double" }, ReturnType = "Double" },
            ["Max"] = new StdLibFunction { Name = "Max", Category = StdLibCategory.Math, ParameterTypes = new[] { "Double", "Double" }, ReturnType = "Double" },
            ["Rnd"] = new StdLibFunction { Name = "Rnd", Category = StdLibCategory.Math, ParameterTypes = Array.Empty<string>(), ReturnType = "Double" },
            ["Randomize"] = new StdLibFunction { Name = "Randomize", Category = StdLibCategory.Math, ParameterTypes = Array.Empty<string>(), ReturnType = "Void" },

            // Array
            ["UBound"] = new StdLibFunction { Name = "UBound", Category = StdLibCategory.Array, ParameterTypes = new[] { "Array", "Integer" }, ReturnType = "Integer" },
            ["LBound"] = new StdLibFunction { Name = "LBound", Category = StdLibCategory.Array, ParameterTypes = new[] { "Array", "Integer" }, ReturnType = "Integer" },

            // Conversion
            ["CInt"] = new StdLibFunction { Name = "CInt", Category = StdLibCategory.Conversion, ParameterTypes = new[] { "Object" }, ReturnType = "Integer" },
            ["CLng"] = new StdLibFunction { Name = "CLng", Category = StdLibCategory.Conversion, ParameterTypes = new[] { "Object" }, ReturnType = "Long" },
            ["CDbl"] = new StdLibFunction { Name = "CDbl", Category = StdLibCategory.Conversion, ParameterTypes = new[] { "Object" }, ReturnType = "Double" },
            ["CSng"] = new StdLibFunction { Name = "CSng", Category = StdLibCategory.Conversion, ParameterTypes = new[] { "Object" }, ReturnType = "Single" },
            ["CStr"] = new StdLibFunction { Name = "CStr", Category = StdLibCategory.Conversion, ParameterTypes = new[] { "Object" }, ReturnType = "String" },
            ["CBool"] = new StdLibFunction { Name = "CBool", Category = StdLibCategory.Conversion, ParameterTypes = new[] { "Object" }, ReturnType = "Boolean" },
        };

        public bool CanHandle(string functionName) => _functions.ContainsKey(functionName);

        public string EmitCall(string functionName, string[] arguments)
        {
            if (!_functions.TryGetValue(functionName, out var func))
                return null;

            return func.Category switch
            {
                StdLibCategory.IO => EmitIOCall(functionName, arguments),
                StdLibCategory.String => EmitStringCall(functionName, arguments),
                StdLibCategory.Math => EmitMathCall(functionName, arguments),
                StdLibCategory.Array => EmitArrayCall(functionName, arguments),
                StdLibCategory.Conversion => EmitConversionCall(functionName, arguments),
                _ => null
            };
        }

        public IEnumerable<string> GetRequiredImports(string functionName)
        {
            if (!_functions.TryGetValue(functionName, out var func))
                yield break;

            yield return "System";

            if (func.Category == StdLibCategory.Math)
                yield return "System.Math";
        }

        public string GetInlineImplementation(string functionName)
        {
            // C# doesn't need inline implementations - just uses BCL
            return null;
        }

        #region I/O Emissions

        private string EmitIOCall(string functionName, string[] args)
        {
            return functionName.ToLower() switch
            {
                "print" => EmitPrint(args[0]),
                "printline" => EmitPrintLine(args[0]),
                "input" => EmitInput(args[0]),
                "readline" => EmitReadLine(),
                _ => null
            };
        }

        public string EmitPrint(string value) => $"Console.Write({value})";
        public string EmitPrintLine(string value) => $"Console.WriteLine({value})";
        public string EmitInput(string prompt) => $"new Func<string>(() => {{ Console.Write({prompt}); return Console.ReadLine(); }})()";
        public string EmitReadLine() => "Console.ReadLine()";

        #endregion

        #region String Emissions

        private string EmitStringCall(string functionName, string[] args)
        {
            return functionName.ToLower() switch
            {
                "len" => EmitLen(args[0]),
                "mid" => EmitMid(args[0], args[1], args[2]),
                "left" => EmitLeft(args[0], args[1]),
                "right" => EmitRight(args[0], args[1]),
                "ucase" => EmitUCase(args[0]),
                "lcase" => EmitLCase(args[0]),
                "trim" => EmitTrim(args[0]),
                "instr" => EmitInStr(args[0], args[1]),
                "replace" => EmitReplace(args[0], args[1], args[2]),
                _ => null
            };
        }

        public string EmitLen(string str) => $"{str}.Length";
        public string EmitMid(string str, string start, string length) => $"{str}.Substring({start} - 1, {length})";
        public string EmitLeft(string str, string length) => $"{str}.Substring(0, {length})";
        public string EmitRight(string str, string length) => $"{str}.Substring({str}.Length - {length})";
        public string EmitUCase(string str) => $"{str}.ToUpper()";
        public string EmitLCase(string str) => $"{str}.ToLower()";
        public string EmitTrim(string str) => $"{str}.Trim()";
        public string EmitInStr(string str, string search) => $"({str}.IndexOf({search}) + 1)";
        public string EmitReplace(string str, string find, string replaceWith) => $"{str}.Replace({find}, {replaceWith})";

        #endregion

        #region Math Emissions

        private string EmitMathCall(string functionName, string[] args)
        {
            return functionName.ToLower() switch
            {
                "abs" => EmitAbs(args[0]),
                "sqrt" => EmitSqrt(args[0]),
                "pow" => EmitPow(args[0], args[1]),
                "sin" => EmitSin(args[0]),
                "cos" => EmitCos(args[0]),
                "tan" => EmitTan(args[0]),
                "log" => EmitLog(args[0]),
                "exp" => EmitExp(args[0]),
                "floor" => EmitFloor(args[0]),
                "ceiling" => EmitCeiling(args[0]),
                "round" => EmitRound(args[0]),
                "min" => EmitMin(args[0], args[1]),
                "max" => EmitMax(args[0], args[1]),
                "rnd" => EmitRnd(),
                "randomize" => EmitRandomize(),
                _ => null
            };
        }

        public string EmitAbs(string value) => $"Math.Abs({value})";
        public string EmitSqrt(string value) => $"Math.Sqrt({value})";
        public string EmitPow(string baseVal, string exponent) => $"Math.Pow({baseVal}, {exponent})";
        public string EmitSin(string value) => $"Math.Sin({value})";
        public string EmitCos(string value) => $"Math.Cos({value})";
        public string EmitTan(string value) => $"Math.Tan({value})";
        public string EmitLog(string value) => $"Math.Log({value})";
        public string EmitExp(string value) => $"Math.Exp({value})";
        public string EmitFloor(string value) => $"Math.Floor({value})";
        public string EmitCeiling(string value) => $"Math.Ceiling({value})";
        public string EmitRound(string value) => $"Math.Round({value})";
        public string EmitMin(string a, string b) => $"Math.Min({a}, {b})";
        public string EmitMax(string a, string b) => $"Math.Max({a}, {b})";
        public string EmitRnd() => "Random.Shared.NextDouble()";
        public string EmitRandomize() => "/* Randomize - no-op in .NET */";

        #endregion

        #region Array Emissions

        private string EmitArrayCall(string functionName, string[] args)
        {
            return functionName.ToLower() switch
            {
                "ubound" => args.Length > 1 ? EmitUBoundDim(args[0], args[1]) : EmitUBound(args[0]),
                "lbound" => args.Length > 1 ? EmitLBoundDim(args[0], args[1]) : EmitLBound(args[0]),
                "length" => EmitLength(args[0]),
                _ => null
            };
        }

        // For 1D arrays
        public string EmitUBound(string array) => $"({array}.Length - 1)";
        public string EmitLBound(string array) => "0";

        // For multi-dimensional arrays
        public string EmitUBoundDim(string array, string dimension) => $"({array}.GetUpperBound({dimension}))";
        public string EmitLBoundDim(string array, string dimension) => $"({array}.GetLowerBound({dimension}))";

        public string EmitLength(string array) => $"{array}.Length";
        public string EmitReDim(string array, string newSize) => $"Array.Resize(ref {array}, {newSize})";

        #endregion

        #region Conversion Emissions

        private string EmitConversionCall(string functionName, string[] args)
        {
            return functionName.ToLower() switch
            {
                "cint" => EmitCInt(args[0]),
                "clng" => EmitCLng(args[0]),
                "cdbl" => EmitCDbl(args[0]),
                "csng" => EmitCSng(args[0]),
                "cstr" => EmitCStr(args[0]),
                "cbool" => EmitCBool(args[0]),
                "cchar" => EmitCChar(args[0]),
                _ => null
            };
        }

        public string EmitCInt(string value) => $"Convert.ToInt32({value})";
        public string EmitCLng(string value) => $"Convert.ToInt64({value})";
        public string EmitCDbl(string value) => $"Convert.ToDouble({value})";
        public string EmitCSng(string value) => $"Convert.ToSingle({value})";
        public string EmitCStr(string value) => $"Convert.ToString({value})";
        public string EmitCBool(string value) => $"Convert.ToBoolean({value})";
        public string EmitCChar(string value) => $"Convert.ToChar({value})";

        #endregion
    }
}
