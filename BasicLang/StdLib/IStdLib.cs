using System;
using System.Collections.Generic;

namespace BasicLang.Compiler.StdLib
{
    /// <summary>
    /// Standard library function categories
    /// </summary>
    public enum StdLibCategory
    {
        IO,
        String,
        Math,
        Array,
        Conversion,
        System
    }

    /// <summary>
    /// Represents a standard library function that can be emitted differently per backend
    /// </summary>
    public class StdLibFunction
    {
        public string Name { get; set; }
        public StdLibCategory Category { get; set; }
        public string[] ParameterTypes { get; set; }
        public string ReturnType { get; set; }
        public bool IsVoid => ReturnType == "Void";
    }

    /// <summary>
    /// Interface for standard library implementations per backend
    /// </summary>
    public interface IStdLibProvider
    {
        /// <summary>
        /// Check if this provider handles the given function
        /// </summary>
        bool CanHandle(string functionName);

        /// <summary>
        /// Emit the function call for this backend
        /// </summary>
        string EmitCall(string functionName, string[] arguments);

        /// <summary>
        /// Get any required imports/includes for this function
        /// </summary>
        IEnumerable<string> GetRequiredImports(string functionName);

        /// <summary>
        /// Get inline implementation if needed (for backends that can't call external code)
        /// </summary>
        string GetInlineImplementation(string functionName);
    }

    /// <summary>
    /// Standard I/O functions
    /// </summary>
    public interface IStdIO
    {
        string EmitPrint(string value);
        string EmitPrintLine(string value);
        string EmitInput(string prompt);
        string EmitReadLine();
    }

    /// <summary>
    /// Standard string functions
    /// </summary>
    public interface IStdString
    {
        string EmitLen(string str);
        string EmitMid(string str, string start, string length);
        string EmitLeft(string str, string length);
        string EmitRight(string str, string length);
        string EmitUCase(string str);
        string EmitLCase(string str);
        string EmitTrim(string str);
        string EmitInStr(string str, string search);
        string EmitReplace(string str, string find, string replaceWith);
    }

    /// <summary>
    /// Standard math functions
    /// </summary>
    public interface IStdMath
    {
        string EmitAbs(string value);
        string EmitSqrt(string value);
        string EmitPow(string baseVal, string exponent);
        string EmitSin(string value);
        string EmitCos(string value);
        string EmitTan(string value);
        string EmitLog(string value);
        string EmitExp(string value);
        string EmitFloor(string value);
        string EmitCeiling(string value);
        string EmitRound(string value);
        string EmitMin(string a, string b);
        string EmitMax(string a, string b);
    }

    /// <summary>
    /// Standard array functions
    /// </summary>
    public interface IStdArray
    {
        // For 1D arrays
        string EmitUBound(string array);
        string EmitLBound(string array);
        // For multi-dimensional arrays
        string EmitUBoundDim(string array, string dimension);
        string EmitLBoundDim(string array, string dimension);
        string EmitLength(string array);
        string EmitReDim(string array, string newSize);
    }

    /// <summary>
    /// Type conversion functions
    /// </summary>
    public interface IStdConversion
    {
        string EmitCInt(string value);
        string EmitCLng(string value);
        string EmitCDbl(string value);
        string EmitCSng(string value);
        string EmitCStr(string value);
        string EmitCBool(string value);
        string EmitCChar(string value);
    }
}
