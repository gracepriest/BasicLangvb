using System.Collections.Generic;
using System.Linq;
using BasicLang.Compiler.AST;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BasicLang.Compiler.LSP
{
    /// <summary>
    /// Service that provides symbol information
    /// </summary>
    public class SymbolService
    {
        private static readonly Dictionary<string, string> BuiltInDocs = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            // Keywords
            ["Sub"] = "**Sub** - Declares a subroutine (procedure that doesn't return a value)\n\n```vb\nSub Name(parameters)\n    ' statements\nEnd Sub\n```",
            ["Function"] = "**Function** - Declares a function that returns a value\n\n```vb\nFunction Name(parameters) As ReturnType\n    Return value\nEnd Function\n```",
            ["If"] = "**If** - Conditional statement\n\n```vb\nIf condition Then\n    ' statements\nElseIf condition Then\n    ' statements\nElse\n    ' statements\nEnd If\n```",
            ["For"] = "**For** - Counting loop\n\n```vb\nFor i = start To end Step increment\n    ' statements\nNext\n```",
            ["While"] = "**While** - Loop while condition is true\n\n```vb\nWhile condition\n    ' statements\nWend\n```",
            ["Do"] = "**Do** - Loop with condition\n\n```vb\nDo While condition\n    ' statements\nLoop\n```",
            ["Class"] = "**Class** - Declares a class\n\n```vb\nClass Name\n    ' members\nEnd Class\n```",
            ["Dim"] = "**Dim** - Declares a variable\n\n```vb\nDim name As Type\nDim name As Type = initialValue\n```",
            ["Const"] = "**Const** - Declares a constant\n\n```vb\nConst NAME As Type = value\n```",

            // Built-in functions
            ["PrintLine"] = "**PrintLine**(text As String)\n\nPrints a line of text to the console followed by a newline.",
            ["Print"] = "**Print**(text As String)\n\nPrints text to the console without a newline.",
            ["ReadLine"] = "**ReadLine**() As String\n\nReads a line of text from the console.",
            ["Len"] = "**Len**(str As String) As Integer\n\nReturns the length of a string.",
            ["Left"] = "**Left**(str As String, count As Integer) As String\n\nReturns the leftmost characters of a string.",
            ["Right"] = "**Right**(str As String, count As Integer) As String\n\nReturns the rightmost characters of a string.",
            ["Mid"] = "**Mid**(str As String, start As Integer, length As Integer) As String\n\nReturns a substring from the middle of a string.",
            ["UCase"] = "**UCase**(str As String) As String\n\nConverts a string to uppercase.",
            ["LCase"] = "**LCase**(str As String) As String\n\nConverts a string to lowercase.",
            ["Trim"] = "**Trim**(str As String) As String\n\nRemoves leading and trailing whitespace.",
            ["InStr"] = "**InStr**(str As String, search As String) As Integer\n\nFinds the position of a substring (1-based, 0 if not found).",
            ["Replace"] = "**Replace**(str As String, old As String, new As String) As String\n\nReplaces all occurrences of a substring.",
            ["Abs"] = "**Abs**(num As Double) As Double\n\nReturns the absolute value of a number.",
            ["Sqrt"] = "**Sqrt**(num As Double) As Double\n\nReturns the square root of a number.",
            ["Pow"] = "**Pow**(base As Double, exponent As Double) As Double\n\nReturns base raised to the power of exponent.",
            ["Sin"] = "**Sin**(radians As Double) As Double\n\nReturns the sine of an angle in radians.",
            ["Cos"] = "**Cos**(radians As Double) As Double\n\nReturns the cosine of an angle in radians.",
            ["Tan"] = "**Tan**(radians As Double) As Double\n\nReturns the tangent of an angle in radians.",
            ["Floor"] = "**Floor**(num As Double) As Double\n\nRounds down to the nearest integer.",
            ["Ceiling"] = "**Ceiling**(num As Double) As Double\n\nRounds up to the nearest integer.",
            ["Round"] = "**Round**(num As Double) As Double\n\nRounds to the nearest integer.",
            ["Min"] = "**Min**(a As Double, b As Double) As Double\n\nReturns the smaller of two values.",
            ["Max"] = "**Max**(a As Double, b As Double) As Double\n\nReturns the larger of two values.",
            ["Rnd"] = "**Rnd**() As Double\n\nReturns a random number between 0 and 1.",
            ["CInt"] = "**CInt**(value) As Integer\n\nConverts a value to Integer.",
            ["CDbl"] = "**CDbl**(value) As Double\n\nConverts a value to Double.",
            ["CStr"] = "**CStr**(value) As String\n\nConverts a value to String.",
            ["CBool"] = "**CBool**(value) As Boolean\n\nConverts a value to Boolean.",
            ["UBound"] = "**UBound**(arr As Array) As Integer\n\nReturns the upper bound (last index) of an array.",
            ["LBound"] = "**LBound**(arr As Array) As Integer\n\nReturns the lower bound (first index) of an array.",

            // Types
            ["Integer"] = "**Integer**\n\n32-bit signed integer (-2,147,483,648 to 2,147,483,647)",
            ["Long"] = "**Long**\n\n64-bit signed integer",
            ["Single"] = "**Single**\n\n32-bit floating-point number",
            ["Double"] = "**Double**\n\n64-bit floating-point number",
            ["String"] = "**String**\n\nText string of Unicode characters",
            ["Boolean"] = "**Boolean**\n\nTrue or False value",
            ["Char"] = "**Char**\n\nSingle Unicode character",
            ["Object"] = "**Object**\n\nBase type for all reference types",

            // Operators
            ["And"] = "**And** - Logical AND operator\n\n```vb\nIf a And b Then\n```",
            ["Or"] = "**Or** - Logical OR operator\n\n```vb\nIf a Or b Then\n```",
            ["Not"] = "**Not** - Logical NOT operator\n\n```vb\nIf Not condition Then\n```",
            ["Xor"] = "**Xor** - Logical XOR operator\n\n```vb\nIf a Xor b Then\n```",
            ["Mod"] = "**Mod** - Modulo (remainder) operator\n\n```vb\nresult = a Mod b\n```",
        };

        /// <summary>
        /// Get hover information for a word
        /// </summary>
        public string GetHoverInfo(DocumentState state, string word)
        {
            // Check built-in docs first
            if (BuiltInDocs.TryGetValue(word, out var docs))
            {
                return docs;
            }

            // Check document symbols
            if (state?.AST != null)
            {
                foreach (var decl in state.AST.Declarations)
                {
                    var info = GetDeclarationHoverInfo(decl, word);
                    if (info != null)
                        return info;
                }
            }

            return null;
        }

        private string GetDeclarationHoverInfo(ASTNode node, string word)
        {
            switch (node)
            {
                case FunctionNode func when func.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    var funcParams = string.Join(", ", func.Parameters.Select(p => $"{p.Name} As {p.Type?.Name ?? "Variant"}"));
                    return $"**Function** {func.Name}({funcParams}) As {func.ReturnType?.Name ?? "Void"}\n\n*User-defined function*";

                case SubroutineNode sub when sub.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    var subParams = string.Join(", ", sub.Parameters.Select(p => $"{p.Name} As {p.Type?.Name ?? "Variant"}"));
                    return $"**Sub** {sub.Name}({subParams})\n\n*User-defined subroutine*";

                case ClassNode cls when cls.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    var inheritance = !string.IsNullOrEmpty(cls.BaseClass) ? $" Inherits {cls.BaseClass}" : "";
                    return $"**Class** {cls.Name}{inheritance}\n\n*User-defined class*";

                case VariableDeclarationNode varDecl when varDecl.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    return $"**{varDecl.Name}** As {varDecl.Type?.Name ?? "Variant"}\n\n*Variable*";

                case ConstantDeclarationNode constDecl when constDecl.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    return $"**Const** {constDecl.Name} As {constDecl.Type?.Name ?? "Variant"}\n\n*Constant*";
            }

            return null;
        }

        /// <summary>
        /// Find the definition location of a symbol
        /// </summary>
        public Location FindDefinition(DocumentState state, string word)
        {
            if (state?.AST == null)
                return null;

            foreach (var decl in state.AST.Declarations)
            {
                var location = FindDeclarationLocation(state, decl, word);
                if (location != null)
                    return location;
            }

            return null;
        }

        private Location FindDeclarationLocation(DocumentState state, ASTNode node, string word)
        {
            int line = -1;
            int column = -1;

            switch (node)
            {
                case FunctionNode func when func.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    line = func.Line;
                    column = func.Column;
                    break;

                case SubroutineNode sub when sub.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    line = sub.Line;
                    column = sub.Column;
                    break;

                case ClassNode cls when cls.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    line = cls.Line;
                    column = cls.Column;
                    break;

                case VariableDeclarationNode varDecl when varDecl.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    line = varDecl.Line;
                    column = varDecl.Column;
                    break;

                case ConstantDeclarationNode constDecl when constDecl.Name.Equals(word, System.StringComparison.OrdinalIgnoreCase):
                    line = constDecl.Line;
                    column = constDecl.Column;
                    break;
            }

            if (line > 0)
            {
                return new Location
                {
                    Uri = state.Uri,
                    Range = new LspRange(
                        new Position(line - 1, column - 1),
                        new Position(line - 1, column - 1 + word.Length))
                };
            }

            return null;
        }

        /// <summary>
        /// Get document symbols for outline view
        /// </summary>
        public List<DocumentSymbol> GetDocumentSymbols(DocumentState state)
        {
            var symbols = new List<DocumentSymbol>();

            if (state?.AST == null)
                return symbols;

            foreach (var decl in state.AST.Declarations)
            {
                var symbol = CreateDocumentSymbol(decl);
                if (symbol != null)
                    symbols.Add(symbol);
            }

            return symbols;
        }

        private DocumentSymbol CreateDocumentSymbol(ASTNode node)
        {
            switch (node)
            {
                case FunctionNode func:
                    var funcChildren = new List<DocumentSymbol>();
                    foreach (var param in func.Parameters)
                    {
                        funcChildren.Add(new DocumentSymbol
                        {
                            Name = param.Name,
                            Kind = SymbolKind.Variable,
                            Range = new LspRange(new Position(param.Line - 1, 0), new Position(param.Line - 1, 100)),
                            SelectionRange = new LspRange(new Position(param.Line - 1, 0), new Position(param.Line - 1, param.Name.Length))
                        });
                    }
                    return new DocumentSymbol
                    {
                        Name = func.Name,
                        Kind = SymbolKind.Function,
                        Detail = $"As {func.ReturnType?.Name ?? "Void"}",
                        Range = new LspRange(new Position(func.Line - 1, 0), new Position(func.Line + 10, 0)),
                        SelectionRange = new LspRange(new Position(func.Line - 1, 0), new Position(func.Line - 1, func.Name.Length + 10)),
                        Children = funcChildren.Count > 0 ? funcChildren : null
                    };

                case SubroutineNode sub:
                    return new DocumentSymbol
                    {
                        Name = sub.Name,
                        Kind = SymbolKind.Method,
                        Range = new LspRange(new Position(sub.Line - 1, 0), new Position(sub.Line + 10, 0)),
                        SelectionRange = new LspRange(new Position(sub.Line - 1, 0), new Position(sub.Line - 1, sub.Name.Length + 5))
                    };

                case ClassNode cls:
                    var classChildren = new List<DocumentSymbol>();
                    foreach (var member in cls.Members)
                    {
                        var memberSymbol = CreateDocumentSymbol(member);
                        if (memberSymbol != null)
                            classChildren.Add(memberSymbol);
                    }
                    return new DocumentSymbol
                    {
                        Name = cls.Name,
                        Kind = SymbolKind.Class,
                        Detail = !string.IsNullOrEmpty(cls.BaseClass) ? $"Inherits {cls.BaseClass}" : null,
                        Range = new LspRange(new Position(cls.Line - 1, 0), new Position(cls.Line + 50, 0)),
                        SelectionRange = new LspRange(new Position(cls.Line - 1, 0), new Position(cls.Line - 1, cls.Name.Length + 7)),
                        Children = classChildren.Count > 0 ? classChildren : null
                    };

                case VariableDeclarationNode varDecl:
                    return new DocumentSymbol
                    {
                        Name = varDecl.Name,
                        Kind = SymbolKind.Variable,
                        Detail = varDecl.Type?.Name,
                        Range = new LspRange(new Position(varDecl.Line - 1, 0), new Position(varDecl.Line - 1, 100)),
                        SelectionRange = new LspRange(new Position(varDecl.Line - 1, 0), new Position(varDecl.Line - 1, varDecl.Name.Length))
                    };

                case ConstantDeclarationNode constDecl:
                    return new DocumentSymbol
                    {
                        Name = constDecl.Name,
                        Kind = SymbolKind.Constant,
                        Detail = constDecl.Type?.Name,
                        Range = new LspRange(new Position(constDecl.Line - 1, 0), new Position(constDecl.Line - 1, 100)),
                        SelectionRange = new LspRange(new Position(constDecl.Line - 1, 0), new Position(constDecl.Line - 1, constDecl.Name.Length))
                    };
            }

            return null;
        }
    }
}
