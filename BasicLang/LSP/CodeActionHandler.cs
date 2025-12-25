using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BasicLang.Compiler.LSP
{
    /// <summary>
    /// Handles code action requests (quick fixes, refactorings)
    /// </summary>
    public class CodeActionHandler : ICodeActionHandler
    {
        private readonly DocumentManager _documentManager;

        public CodeActionHandler(DocumentManager documentManager)
        {
            _documentManager = documentManager;
        }

        public Task<CommandOrCodeActionContainer> Handle(CodeActionParams request, CancellationToken cancellationToken)
        {
            var state = _documentManager.GetDocument(request.TextDocument.Uri);
            if (state == null)
            {
                return Task.FromResult(new CommandOrCodeActionContainer());
            }

            var actions = new List<CommandOrCodeAction>();

            // Get diagnostics in the requested range
            var diagnosticsInRange = request.Context.Diagnostics
                .Where(d => RangesOverlap(d.Range, request.Range))
                .ToList();

            // Generate quick fixes for each diagnostic
            foreach (var diagnostic in diagnosticsInRange)
            {
                var fixes = GetQuickFixesForDiagnostic(state, diagnostic, request.TextDocument.Uri);
                actions.AddRange(fixes);
            }

            // Add refactoring actions based on selection
            var refactorings = GetRefactoringActions(state, request.Range, request.TextDocument.Uri);
            actions.AddRange(refactorings);

            return Task.FromResult(new CommandOrCodeActionContainer(actions));
        }

        private List<CommandOrCodeAction> GetQuickFixesForDiagnostic(
            DocumentState state,
            OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic diagnostic,
            DocumentUri uri)
        {
            var fixes = new List<CommandOrCodeAction>();
            var message = diagnostic.Message ?? "";

            // Fix: Expected 'End If'
            if (message.Contains("Expected 'End If'") || message.Contains("Expected 'End If"))
            {
                fixes.Add(CreateInsertFix(
                    "Add 'End If'",
                    uri,
                    new Position(diagnostic.Range.End.Line + 1, 0),
                    "End If\n",
                    diagnostic));
            }

            // Fix: Expected 'End Sub'
            if (message.Contains("Expected 'End Sub'"))
            {
                fixes.Add(CreateInsertFix(
                    "Add 'End Sub'",
                    uri,
                    new Position(diagnostic.Range.End.Line + 1, 0),
                    "End Sub\n",
                    diagnostic));
            }

            // Fix: Expected 'End Function'
            if (message.Contains("Expected 'End Function'"))
            {
                fixes.Add(CreateInsertFix(
                    "Add 'End Function'",
                    uri,
                    new Position(diagnostic.Range.End.Line + 1, 0),
                    "End Function\n",
                    diagnostic));
            }

            // Fix: Expected 'End Class'
            if (message.Contains("Expected 'End Class'"))
            {
                fixes.Add(CreateInsertFix(
                    "Add 'End Class'",
                    uri,
                    new Position(diagnostic.Range.End.Line + 1, 0),
                    "End Class\n",
                    diagnostic));
            }

            // Fix: Expected 'Wend' or 'End While'
            if (message.Contains("Expected 'Wend'") || message.Contains("Expected 'End While'"))
            {
                fixes.Add(CreateInsertFix(
                    "Add 'Wend'",
                    uri,
                    new Position(diagnostic.Range.End.Line + 1, 0),
                    "Wend\n",
                    diagnostic));
            }

            // Fix: Expected 'Next'
            if (message.Contains("Expected 'Next'"))
            {
                fixes.Add(CreateInsertFix(
                    "Add 'Next'",
                    uri,
                    new Position(diagnostic.Range.End.Line + 1, 0),
                    "Next\n",
                    diagnostic));
            }

            // Fix: Expected 'Loop'
            if (message.Contains("Expected 'Loop'"))
            {
                fixes.Add(CreateInsertFix(
                    "Add 'Loop'",
                    uri,
                    new Position(diagnostic.Range.End.Line + 1, 0),
                    "Loop\n",
                    diagnostic));
            }

            // Fix: Undefined variable - suggest adding Dim
            if (message.Contains("Undefined variable") || message.Contains("is not defined"))
            {
                var varName = ExtractVariableName(message);
                if (!string.IsNullOrEmpty(varName))
                {
                    int insertLine = FindDeclarationInsertLine(state, (int)diagnostic.Range.Start.Line);
                    fixes.Add(CreateInsertFix(
                        $"Declare variable '{varName}'",
                        uri,
                        new Position(insertLine, 0),
                        $"    Dim {varName} As Object\n",
                        diagnostic));
                }
            }

            // Fix: Missing 'Then' after If condition
            if (message.Contains("Expected 'Then'"))
            {
                fixes.Add(CreateInsertFix(
                    "Add 'Then'",
                    uri,
                    diagnostic.Range.End,
                    " Then",
                    diagnostic));
            }

            return fixes;
        }

        private List<CommandOrCodeAction> GetRefactoringActions(
            DocumentState state,
            LspRange range,
            DocumentUri uri)
        {
            var actions = new List<CommandOrCodeAction>();

            if (state?.Lines == null) return actions;

            int startLine = (int)range.Start.Line;
            if (startLine < 0 || startLine >= state.Lines.Length) return actions;

            var lineText = state.Lines[startLine].Trim();
            var originalLine = state.Lines[startLine];

            // Convert Sub to Function
            if (lineText.StartsWith("Sub ") && !lineText.Contains("Sub New"))
            {
                var edit = new WorkspaceEdit
                {
                    Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                    {
                        [uri] = new[]
                        {
                            new TextEdit
                            {
                                Range = new LspRange(
                                    new Position(startLine, 0),
                                    new Position(startLine, originalLine.Length)),
                                NewText = originalLine.Replace("Sub ", "Function ") + " As Object"
                            }
                        }
                    }
                };

                actions.Add(new CommandOrCodeAction(new CodeAction
                {
                    Title = "Convert to Function",
                    Kind = CodeActionKind.RefactorRewrite,
                    Edit = edit
                }));
            }

            // Convert Function to Sub
            if (lineText.StartsWith("Function "))
            {
                var asIndex = originalLine.LastIndexOf(" As ");
                var newLine = asIndex > 0
                    ? originalLine.Substring(0, asIndex).Replace("Function ", "Sub ")
                    : originalLine.Replace("Function ", "Sub ");

                var edit = new WorkspaceEdit
                {
                    Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                    {
                        [uri] = new[]
                        {
                            new TextEdit
                            {
                                Range = new LspRange(
                                    new Position(startLine, 0),
                                    new Position(startLine, originalLine.Length)),
                                NewText = newLine
                            }
                        }
                    }
                };

                actions.Add(new CommandOrCodeAction(new CodeAction
                {
                    Title = "Convert to Sub",
                    Kind = CodeActionKind.RefactorRewrite,
                    Edit = edit
                }));
            }

            // Add Option Explicit
            if (startLine == 0 && !state.Content.TrimStart().StartsWith("Option"))
            {
                var edit = new WorkspaceEdit
                {
                    Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                    {
                        [uri] = new[]
                        {
                            new TextEdit
                            {
                                Range = new LspRange(new Position(0, 0), new Position(0, 0)),
                                NewText = "Option Explicit\n\n"
                            }
                        }
                    }
                };

                actions.Add(new CommandOrCodeAction(new CodeAction
                {
                    Title = "Add 'Option Explicit'",
                    Kind = CodeActionKind.Source,
                    Edit = edit
                }));
            }

            return actions;
        }

        private CommandOrCodeAction CreateInsertFix(
            string title,
            DocumentUri uri,
            Position position,
            string text,
            OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic diagnostic)
        {
            var edit = new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    [uri] = new[]
                    {
                        new TextEdit
                        {
                            Range = new LspRange(position, position),
                            NewText = text
                        }
                    }
                }
            };

            return new CommandOrCodeAction(new CodeAction
            {
                Title = title,
                Kind = CodeActionKind.QuickFix,
                Diagnostics = new Container<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic>(diagnostic),
                Edit = edit,
                IsPreferred = true
            });
        }

        private string ExtractVariableName(string message)
        {
            var quoteStart = message.IndexOf('\'');
            if (quoteStart >= 0)
            {
                var quoteEnd = message.IndexOf('\'', quoteStart + 1);
                if (quoteEnd > quoteStart)
                {
                    return message.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                }
            }
            return null;
        }

        private int FindDeclarationInsertLine(DocumentState state, int currentLine)
        {
            if (state?.Lines == null) return currentLine;

            for (int i = currentLine; i >= 0; i--)
            {
                var line = state.Lines[i].Trim();
                if (line.StartsWith("Sub ") || line.StartsWith("Function ") ||
                    line.StartsWith("Public Sub") || line.StartsWith("Public Function") ||
                    line.StartsWith("Private Sub") || line.StartsWith("Private Function"))
                {
                    return i + 1;
                }
            }
            return currentLine;
        }

        private bool RangesOverlap(LspRange a, LspRange b)
        {
            if (a.End.Line < b.Start.Line) return false;
            if (b.End.Line < a.Start.Line) return false;
            if (a.End.Line == b.Start.Line && a.End.Character < b.Start.Character) return false;
            if (b.End.Line == a.Start.Line && b.End.Character < a.Start.Character) return false;
            return true;
        }

        public CodeActionRegistrationOptions GetRegistrationOptions(
            CodeActionCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new CodeActionRegistrationOptions
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("basiclang"),
                CodeActionKinds = new Container<CodeActionKind>(
                    CodeActionKind.QuickFix,
                    CodeActionKind.Refactor,
                    CodeActionKind.RefactorRewrite,
                    CodeActionKind.Source
                ),
                ResolveProvider = false
            };
        }
    }
}
