using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BasicLang.Compiler.LSP
{
    /// <summary>
    /// Service that provides completion items
    /// </summary>
    public class CompletionService
    {
        /// <summary>
        /// Get completion items based on context
        /// </summary>
        public List<CompletionItem> GetCompletions(DocumentState state, int line, int character)
        {
            var completions = new List<CompletionItem>();

            // Add keywords
            completions.AddRange(GetKeywordCompletions());

            // Add built-in functions
            completions.AddRange(GetBuiltInFunctionCompletions());

            // Add built-in types
            completions.AddRange(GetTypeCompletions());

            // Add symbols from the current document
            if (state?.SemanticAnalyzer != null)
            {
                completions.AddRange(GetSymbolCompletions(state));
            }

            return completions;
        }

        private IEnumerable<CompletionItem> GetKeywordCompletions()
        {
            var keywords = new[]
            {
                ("Sub", "Subroutine declaration", "Sub ${1:Name}()\n\t$0\nEnd Sub"),
                ("Function", "Function declaration", "Function ${1:Name}() As ${2:Integer}\n\t$0\nEnd Function"),
                ("If", "If statement", "If ${1:condition} Then\n\t$0\nEnd If"),
                ("If...Else", "If-Else statement", "If ${1:condition} Then\n\t$2\nElse\n\t$0\nEnd If"),
                ("For", "For loop", "For ${1:i} = ${2:1} To ${3:10}\n\t$0\nNext"),
                ("While", "While loop", "While ${1:condition}\n\t$0\nWend"),
                ("Do While", "Do While loop", "Do While ${1:condition}\n\t$0\nLoop"),
                ("Do Until", "Do Until loop", "Do Until ${1:condition}\n\t$0\nLoop"),
                ("Select Case", "Select Case statement", "Select Case ${1:expression}\n\tCase ${2:value}\n\t\t$0\n\tCase Else\n\t\t\nEnd Select"),
                ("Class", "Class declaration", "Class ${1:Name}\n\t$0\nEnd Class"),
                ("Dim", "Variable declaration", "Dim ${1:name} As ${2:Integer}"),
                ("Const", "Constant declaration", "Const ${1:NAME} As ${2:Integer} = ${3:0}"),
                ("Return", "Return statement", "Return ${1:value}"),
                ("Exit", "Exit statement", "Exit ${1|For,While,Do,Sub,Function|}"),
                ("Try", "Try-Catch block", "Try\n\t$0\nCatch ex As Exception\n\t\nEnd Try"),
                ("Property", "Property declaration", "Property ${1:Name} As ${2:Integer}\n\tGet\n\t\tReturn ${3:_value}\n\tEnd Get\n\tSet(value As ${2:Integer})\n\t\t${3:_value} = value\n\tEnd Set\nEnd Property"),
            };

            foreach (var (label, detail, snippet) in keywords)
            {
                yield return new CompletionItem
                {
                    Label = label,
                    Kind = CompletionItemKind.Keyword,
                    Detail = detail,
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = snippet
                };
            }

            // Simple keywords without snippets
            var simpleKeywords = new[]
            {
                "And", "Or", "Not", "Xor", "Mod",
                "True", "False", "Nothing",
                "Public", "Private", "Protected",
                "Shared", "Overridable", "Overrides",
                "Inherits", "Implements",
                "Me", "MyBase",
                "New", "As", "Of",
                "Then", "Else", "ElseIf",
                "End", "Next", "Wend", "Loop",
                "To", "Step", "Each", "In"
            };

            foreach (var keyword in simpleKeywords)
            {
                yield return new CompletionItem
                {
                    Label = keyword,
                    Kind = CompletionItemKind.Keyword,
                    InsertText = keyword
                };
            }
        }

        private IEnumerable<CompletionItem> GetBuiltInFunctionCompletions()
        {
            var functions = new[]
            {
                // Console I/O
                ("PrintLine", "Prints a line to the console", "PrintLine(${1:text})", "Sub"),
                ("Print", "Prints to the console without newline", "Print(${1:text})", "Sub"),
                ("ReadLine", "Reads a line from the console", "ReadLine()", "String"),
                ("ReadKey", "Reads a key press", "ReadKey()", "String"),

                // String functions
                ("Len", "Returns the length of a string", "Len(${1:str})", "Integer"),
                ("Left", "Returns leftmost characters", "Left(${1:str}, ${2:count})", "String"),
                ("Right", "Returns rightmost characters", "Right(${1:str}, ${2:count})", "String"),
                ("Mid", "Returns a substring", "Mid(${1:str}, ${2:start}, ${3:length})", "String"),
                ("UCase", "Converts to uppercase", "UCase(${1:str})", "String"),
                ("LCase", "Converts to lowercase", "LCase(${1:str})", "String"),
                ("Trim", "Removes leading/trailing whitespace", "Trim(${1:str})", "String"),
                ("LTrim", "Removes leading whitespace", "LTrim(${1:str})", "String"),
                ("RTrim", "Removes trailing whitespace", "RTrim(${1:str})", "String"),
                ("InStr", "Finds substring position", "InStr(${1:str}, ${2:search})", "Integer"),
                ("Replace", "Replaces occurrences in string", "Replace(${1:str}, ${2:old}, ${3:new})", "String"),
                ("Split", "Splits string into array", "Split(${1:str}, ${2:delimiter})", "String()"),
                ("Join", "Joins array into string", "Join(${1:arr}, ${2:delimiter})", "String"),

                // Math functions
                ("Abs", "Returns absolute value", "Abs(${1:num})", "Double"),
                ("Sqrt", "Returns square root", "Sqrt(${1:num})", "Double"),
                ("Pow", "Returns power", "Pow(${1:base}, ${2:exponent})", "Double"),
                ("Sin", "Returns sine", "Sin(${1:radians})", "Double"),
                ("Cos", "Returns cosine", "Cos(${1:radians})", "Double"),
                ("Tan", "Returns tangent", "Tan(${1:radians})", "Double"),
                ("Log", "Returns natural logarithm", "Log(${1:num})", "Double"),
                ("Log10", "Returns base-10 logarithm", "Log10(${1:num})", "Double"),
                ("Exp", "Returns e raised to power", "Exp(${1:num})", "Double"),
                ("Floor", "Rounds down", "Floor(${1:num})", "Double"),
                ("Ceiling", "Rounds up", "Ceiling(${1:num})", "Double"),
                ("Round", "Rounds to nearest", "Round(${1:num})", "Double"),
                ("Min", "Returns minimum", "Min(${1:a}, ${2:b})", "Double"),
                ("Max", "Returns maximum", "Max(${1:a}, ${2:b})", "Double"),
                ("Rnd", "Returns random number 0-1", "Rnd()", "Double"),
                ("Randomize", "Seeds random generator", "Randomize(${1:seed})", "Sub"),

                // Type conversion
                ("CInt", "Converts to Integer", "CInt(${1:value})", "Integer"),
                ("CLng", "Converts to Long", "CLng(${1:value})", "Long"),
                ("CDbl", "Converts to Double", "CDbl(${1:value})", "Double"),
                ("CSng", "Converts to Single", "CSng(${1:value})", "Single"),
                ("CStr", "Converts to String", "CStr(${1:value})", "String"),
                ("CBool", "Converts to Boolean", "CBool(${1:value})", "Boolean"),
                ("Chr", "Converts ASCII to character", "Chr(${1:code})", "Char"),
                ("Asc", "Converts character to ASCII", "Asc(${1:char})", "Integer"),
                ("Val", "Converts string to number", "Val(${1:str})", "Double"),

                // Array functions
                ("UBound", "Returns upper bound of array", "UBound(${1:arr})", "Integer"),
                ("LBound", "Returns lower bound of array", "LBound(${1:arr})", "Integer"),
                ("Array", "Creates an array", "Array(${1:values})", "Variant()"),
            };

            foreach (var (name, detail, snippet, returnType) in functions)
            {
                yield return new CompletionItem
                {
                    Label = name,
                    Kind = CompletionItemKind.Function,
                    Detail = $"{detail} -> {returnType}",
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = snippet
                };
            }
        }

        private IEnumerable<CompletionItem> GetTypeCompletions()
        {
            var types = new[]
            {
                ("Integer", "32-bit signed integer"),
                ("Long", "64-bit signed integer"),
                ("Single", "32-bit floating point"),
                ("Double", "64-bit floating point"),
                ("String", "Text string"),
                ("Boolean", "True or False"),
                ("Char", "Single character"),
                ("Byte", "8-bit unsigned integer"),
                ("Object", "Base type for all objects"),
                ("Variant", "Can hold any type"),
            };

            foreach (var (name, detail) in types)
            {
                yield return new CompletionItem
                {
                    Label = name,
                    Kind = CompletionItemKind.Class,
                    Detail = detail,
                    InsertText = name
                };
            }
        }

        private IEnumerable<CompletionItem> GetSymbolCompletions(DocumentState state)
        {
            if (state.AST == null) yield break;

            // Add declared functions and subs
            foreach (var decl in state.AST.Declarations)
            {
                if (decl is BasicLang.Compiler.AST.FunctionNode func)
                {
                    yield return new CompletionItem
                    {
                        Label = func.Name,
                        Kind = CompletionItemKind.Function,
                        Detail = $"Function {func.Name}() As {func.ReturnType?.Name ?? "Void"}",
                        InsertText = func.Name
                    };
                }
                else if (decl is BasicLang.Compiler.AST.SubroutineNode sub)
                {
                    yield return new CompletionItem
                    {
                        Label = sub.Name,
                        Kind = CompletionItemKind.Function,
                        Detail = $"Sub {sub.Name}()",
                        InsertText = sub.Name
                    };
                }
                else if (decl is BasicLang.Compiler.AST.ClassNode cls)
                {
                    yield return new CompletionItem
                    {
                        Label = cls.Name,
                        Kind = CompletionItemKind.Class,
                        Detail = $"Class {cls.Name}",
                        InsertText = cls.Name
                    };
                }
                else if (decl is BasicLang.Compiler.AST.VariableDeclarationNode varDecl)
                {
                    yield return new CompletionItem
                    {
                        Label = varDecl.Name,
                        Kind = CompletionItemKind.Variable,
                        Detail = $"{varDecl.Name} As {varDecl.Type?.Name ?? "Variant"}",
                        InsertText = varDecl.Name
                    };
                }
            }
        }
    }
}
