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
        FileIO,
        String,
        Math,
        Array,
        Conversion,
        DateTime,
        System,
        Collections
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

    /// <summary>
    /// File I/O functions
    /// </summary>
    public interface IStdFileIO
    {
        // Simple file operations
        string EmitFileRead(string path);
        string EmitFileWrite(string path, string content);
        string EmitFileAppend(string path, string content);
        string EmitFileExists(string path);
        string EmitFileDelete(string path);
        string EmitFileCopy(string source, string dest);
        string EmitFileMove(string source, string dest);

        // Directory operations
        string EmitDirExists(string path);
        string EmitDirCreate(string path);
        string EmitDirDelete(string path);
        string EmitDirGetFiles(string path);
        string EmitDirGetDirs(string path);

        // Path operations
        string EmitPathCombine(string path1, string path2);
        string EmitPathGetFileName(string path);
        string EmitPathGetDirectory(string path);
        string EmitPathGetExtension(string path);
    }

    /// <summary>
    /// Date/Time functions
    /// </summary>
    public interface IStdDateTime
    {
        string EmitNow();
        string EmitToday();
        string EmitYear(string date);
        string EmitMonth(string date);
        string EmitDay(string date);
        string EmitHour(string date);
        string EmitMinute(string date);
        string EmitSecond(string date);
        string EmitDateAdd(string date, string interval, string number);
        string EmitDateDiff(string date1, string date2, string interval);
        string EmitFormatDate(string date, string format);
    }

    /// <summary>
    /// Collections functions - List, Dictionary, HashSet operations
    /// </summary>
    public interface IStdCollections
    {
        // List operations
        string EmitCreateList();
        string EmitListAdd(string list, string item);
        string EmitListGet(string list, string index);
        string EmitListSet(string list, string index, string value);
        string EmitListRemove(string list, string item);
        string EmitListRemoveAt(string list, string index);
        string EmitListCount(string list);
        string EmitListContains(string list, string item);
        string EmitListIndexOf(string list, string item);
        string EmitListClear(string list);
        string EmitListInsert(string list, string index, string item);
        string EmitListToArray(string list);

        // Dictionary operations
        string EmitCreateDictionary();
        string EmitDictAdd(string dict, string key, string value);
        string EmitDictGet(string dict, string key);
        string EmitDictSet(string dict, string key, string value);
        string EmitDictRemove(string dict, string key);
        string EmitDictCount(string dict);
        string EmitDictContainsKey(string dict, string key);
        string EmitDictContainsValue(string dict, string value);
        string EmitDictKeys(string dict);
        string EmitDictValues(string dict);
        string EmitDictClear(string dict);

        // HashSet operations
        string EmitCreateHashSet();
        string EmitSetAdd(string set, string item);
        string EmitSetRemove(string set, string item);
        string EmitSetContains(string set, string item);
        string EmitSetCount(string set);
        string EmitSetClear(string set);
        string EmitSetUnion(string set1, string set2);
        string EmitSetIntersect(string set1, string set2);
        string EmitSetExcept(string set1, string set2);
        string EmitSetToArray(string set);
    }
}
