using System;
using System.Collections.Generic;

namespace BasicLang.Compiler.StdLib.CSharp
{
    /// <summary>
    /// C# implementation of standard library functions
    /// Maps BasicLang stdlib to .NET BCL calls
    /// </summary>
    public class CSharpStdLibProvider : IStdLibProvider, IStdIO, IStdString, IStdMath, IStdArray, IStdConversion, IStdFileIO, IStdDateTime
    {
        private static readonly Dictionary<string, StdLibFunction> _functions = new Dictionary<string, StdLibFunction>(StringComparer.OrdinalIgnoreCase)
        {
            // I/O
            ["Print"] = new StdLibFunction { Name = "Print", Category = StdLibCategory.IO, ParameterTypes = new[] { "Object" }, ReturnType = "Void" },
            ["PrintLine"] = new StdLibFunction { Name = "PrintLine", Category = StdLibCategory.IO, ParameterTypes = new[] { "Object" }, ReturnType = "Void" },
            ["Input"] = new StdLibFunction { Name = "Input", Category = StdLibCategory.IO, ParameterTypes = new[] { "String" }, ReturnType = "String" },
            ["ReadLine"] = new StdLibFunction { Name = "ReadLine", Category = StdLibCategory.IO, ParameterTypes = Array.Empty<string>(), ReturnType = "String" },

            // File I/O
            ["FileRead"] = new StdLibFunction { Name = "FileRead", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "String" },
            ["FileWrite"] = new StdLibFunction { Name = "FileWrite", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String", "String" }, ReturnType = "Void" },
            ["FileAppend"] = new StdLibFunction { Name = "FileAppend", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String", "String" }, ReturnType = "Void" },
            ["FileExists"] = new StdLibFunction { Name = "FileExists", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "Boolean" },
            ["FileDelete"] = new StdLibFunction { Name = "FileDelete", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "Void" },
            ["FileCopy"] = new StdLibFunction { Name = "FileCopy", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String", "String" }, ReturnType = "Void" },
            ["FileMove"] = new StdLibFunction { Name = "FileMove", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String", "String" }, ReturnType = "Void" },
            ["DirExists"] = new StdLibFunction { Name = "DirExists", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "Boolean" },
            ["DirCreate"] = new StdLibFunction { Name = "DirCreate", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "Void" },
            ["DirDelete"] = new StdLibFunction { Name = "DirDelete", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "Void" },
            ["DirGetFiles"] = new StdLibFunction { Name = "DirGetFiles", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "String[]" },
            ["DirGetDirs"] = new StdLibFunction { Name = "DirGetDirs", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "String[]" },
            ["PathCombine"] = new StdLibFunction { Name = "PathCombine", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String", "String" }, ReturnType = "String" },
            ["PathGetFileName"] = new StdLibFunction { Name = "PathGetFileName", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "String" },
            ["PathGetDirectory"] = new StdLibFunction { Name = "PathGetDirectory", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "String" },
            ["PathGetExtension"] = new StdLibFunction { Name = "PathGetExtension", Category = StdLibCategory.FileIO, ParameterTypes = new[] { "String" }, ReturnType = "String" },

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
            ["Split"] = new StdLibFunction { Name = "Split", Category = StdLibCategory.String, ParameterTypes = new[] { "String", "String" }, ReturnType = "String[]" },
            ["Join"] = new StdLibFunction { Name = "Join", Category = StdLibCategory.String, ParameterTypes = new[] { "String[]", "String" }, ReturnType = "String" },
            ["Chr"] = new StdLibFunction { Name = "Chr", Category = StdLibCategory.String, ParameterTypes = new[] { "Integer" }, ReturnType = "String" },
            ["Asc"] = new StdLibFunction { Name = "Asc", Category = StdLibCategory.String, ParameterTypes = new[] { "String" }, ReturnType = "Integer" },

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

            // DateTime
            ["Now"] = new StdLibFunction { Name = "Now", Category = StdLibCategory.DateTime, ParameterTypes = Array.Empty<string>(), ReturnType = "DateTime" },
            ["Today"] = new StdLibFunction { Name = "Today", Category = StdLibCategory.DateTime, ParameterTypes = Array.Empty<string>(), ReturnType = "DateTime" },
            ["Year"] = new StdLibFunction { Name = "Year", Category = StdLibCategory.DateTime, ParameterTypes = new[] { "DateTime" }, ReturnType = "Integer" },
            ["Month"] = new StdLibFunction { Name = "Month", Category = StdLibCategory.DateTime, ParameterTypes = new[] { "DateTime" }, ReturnType = "Integer" },
            ["Day"] = new StdLibFunction { Name = "Day", Category = StdLibCategory.DateTime, ParameterTypes = new[] { "DateTime" }, ReturnType = "Integer" },
            ["Hour"] = new StdLibFunction { Name = "Hour", Category = StdLibCategory.DateTime, ParameterTypes = new[] { "DateTime" }, ReturnType = "Integer" },
            ["Minute"] = new StdLibFunction { Name = "Minute", Category = StdLibCategory.DateTime, ParameterTypes = new[] { "DateTime" }, ReturnType = "Integer" },
            ["Second"] = new StdLibFunction { Name = "Second", Category = StdLibCategory.DateTime, ParameterTypes = new[] { "DateTime" }, ReturnType = "Integer" },
            ["DateAdd"] = new StdLibFunction { Name = "DateAdd", Category = StdLibCategory.DateTime, ParameterTypes = new[] { "DateTime", "String", "Integer" }, ReturnType = "DateTime" },
            ["DateDiff"] = new StdLibFunction { Name = "DateDiff", Category = StdLibCategory.DateTime, ParameterTypes = new[] { "DateTime", "DateTime", "String" }, ReturnType = "Integer" },
            ["FormatDate"] = new StdLibFunction { Name = "FormatDate", Category = StdLibCategory.DateTime, ParameterTypes = new[] { "DateTime", "String" }, ReturnType = "String" },
        };

        public bool CanHandle(string functionName) => _functions.ContainsKey(functionName);

        public string EmitCall(string functionName, string[] arguments)
        {
            if (!_functions.TryGetValue(functionName, out var func))
                return null;

            return func.Category switch
            {
                StdLibCategory.IO => EmitIOCall(functionName, arguments),
                StdLibCategory.FileIO => EmitFileIOCall(functionName, arguments),
                StdLibCategory.String => EmitStringCall(functionName, arguments),
                StdLibCategory.Math => EmitMathCall(functionName, arguments),
                StdLibCategory.Array => EmitArrayCall(functionName, arguments),
                StdLibCategory.Conversion => EmitConversionCall(functionName, arguments),
                StdLibCategory.DateTime => EmitDateTimeCall(functionName, arguments),
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

            if (func.Category == StdLibCategory.FileIO)
                yield return "System.IO";
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
                "split" => EmitSplit(args[0], args[1]),
                "join" => EmitJoin(args[0], args[1]),
                "chr" => EmitChr(args[0]),
                "asc" => EmitAsc(args[0]),
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
        public string EmitSplit(string str, string delimiter) => $"{str}.Split({delimiter})";
        public string EmitJoin(string array, string delimiter) => $"string.Join({delimiter}, {array})";
        public string EmitChr(string code) => $"((char){code}).ToString()";
        public string EmitAsc(string str) => $"(int){str}[0]";

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

        #region File I/O Emissions

        private string EmitFileIOCall(string functionName, string[] args)
        {
            return functionName.ToLower() switch
            {
                "fileread" => EmitFileRead(args[0]),
                "filewrite" => EmitFileWrite(args[0], args[1]),
                "fileappend" => EmitFileAppend(args[0], args[1]),
                "fileexists" => EmitFileExists(args[0]),
                "filedelete" => EmitFileDelete(args[0]),
                "filecopy" => EmitFileCopy(args[0], args[1]),
                "filemove" => EmitFileMove(args[0], args[1]),
                "direxists" => EmitDirExists(args[0]),
                "dircreate" => EmitDirCreate(args[0]),
                "dirdelete" => EmitDirDelete(args[0]),
                "dirgetfiles" => EmitDirGetFiles(args[0]),
                "dirgetdirs" => EmitDirGetDirs(args[0]),
                "pathcombine" => EmitPathCombine(args[0], args[1]),
                "pathgetfilename" => EmitPathGetFileName(args[0]),
                "pathgetdirectory" => EmitPathGetDirectory(args[0]),
                "pathgetextension" => EmitPathGetExtension(args[0]),
                _ => null
            };
        }

        public string EmitFileRead(string path) => $"File.ReadAllText({path})";
        public string EmitFileWrite(string path, string content) => $"File.WriteAllText({path}, {content})";
        public string EmitFileAppend(string path, string content) => $"File.AppendAllText({path}, {content})";
        public string EmitFileExists(string path) => $"File.Exists({path})";
        public string EmitFileDelete(string path) => $"File.Delete({path})";
        public string EmitFileCopy(string source, string dest) => $"File.Copy({source}, {dest}, true)";
        public string EmitFileMove(string source, string dest) => $"File.Move({source}, {dest}, true)";

        public string EmitDirExists(string path) => $"Directory.Exists({path})";
        public string EmitDirCreate(string path) => $"Directory.CreateDirectory({path})";
        public string EmitDirDelete(string path) => $"Directory.Delete({path}, true)";
        public string EmitDirGetFiles(string path) => $"Directory.GetFiles({path})";
        public string EmitDirGetDirs(string path) => $"Directory.GetDirectories({path})";

        public string EmitPathCombine(string path1, string path2) => $"Path.Combine({path1}, {path2})";
        public string EmitPathGetFileName(string path) => $"Path.GetFileName({path})";
        public string EmitPathGetDirectory(string path) => $"Path.GetDirectoryName({path})";
        public string EmitPathGetExtension(string path) => $"Path.GetExtension({path})";

        #endregion

        #region DateTime Emissions

        private string EmitDateTimeCall(string functionName, string[] args)
        {
            return functionName.ToLower() switch
            {
                "now" => EmitNow(),
                "today" => EmitToday(),
                "year" => EmitYear(args[0]),
                "month" => EmitMonth(args[0]),
                "day" => EmitDay(args[0]),
                "hour" => EmitHour(args[0]),
                "minute" => EmitMinute(args[0]),
                "second" => EmitSecond(args[0]),
                "dateadd" => EmitDateAdd(args[0], args[1], args[2]),
                "datediff" => EmitDateDiff(args[0], args[1], args[2]),
                "formatdate" => EmitFormatDate(args[0], args[1]),
                _ => null
            };
        }

        public string EmitNow() => "DateTime.Now";
        public string EmitToday() => "DateTime.Today";
        public string EmitYear(string date) => $"{date}.Year";
        public string EmitMonth(string date) => $"{date}.Month";
        public string EmitDay(string date) => $"{date}.Day";
        public string EmitHour(string date) => $"{date}.Hour";
        public string EmitMinute(string date) => $"{date}.Minute";
        public string EmitSecond(string date) => $"{date}.Second";
        public string EmitDateAdd(string date, string interval, string number) =>
            $"({interval}.ToLower() switch {{ \"d\" => {date}.AddDays({number}), \"m\" => {date}.AddMonths({number}), \"y\" => {date}.AddYears({number}), \"h\" => {date}.AddHours({number}), \"n\" => {date}.AddMinutes({number}), \"s\" => {date}.AddSeconds({number}), _ => {date} }})";
        public string EmitDateDiff(string date1, string date2, string interval) =>
            $"({interval}.ToLower() switch {{ \"d\" => (int)({date2} - {date1}).TotalDays, \"m\" => (({date2}.Year - {date1}.Year) * 12 + {date2}.Month - {date1}.Month), \"y\" => {date2}.Year - {date1}.Year, \"h\" => (int)({date2} - {date1}).TotalHours, \"n\" => (int)({date2} - {date1}).TotalMinutes, \"s\" => (int)({date2} - {date1}).TotalSeconds, _ => 0 }})";
        public string EmitFormatDate(string date, string format) => $"{date}.ToString({format})";

        #endregion
    }
}
