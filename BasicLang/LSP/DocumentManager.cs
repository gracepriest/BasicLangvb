using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;
using BasicLang.Compiler.SemanticAnalysis;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace BasicLang.Compiler.LSP
{
    /// <summary>
    /// Manages open documents and their parsed state
    /// </summary>
    public class DocumentManager
    {
        private readonly ConcurrentDictionary<DocumentUri, DocumentState> _documents;

        public DocumentManager()
        {
            _documents = new ConcurrentDictionary<DocumentUri, DocumentState>();
        }

        /// <summary>
        /// Open or update a document
        /// </summary>
        public DocumentState UpdateDocument(DocumentUri uri, string content)
        {
            var state = new DocumentState(uri, content);
            state.Parse();
            _documents[uri] = state;
            return state;
        }

        /// <summary>
        /// Get a document's state
        /// </summary>
        public DocumentState GetDocument(DocumentUri uri)
        {
            _documents.TryGetValue(uri, out var state);
            return state;
        }

        /// <summary>
        /// Close a document
        /// </summary>
        public void CloseDocument(DocumentUri uri)
        {
            _documents.TryRemove(uri, out _);
        }

        /// <summary>
        /// Get all open documents
        /// </summary>
        public IEnumerable<DocumentState> GetAllDocuments()
        {
            return _documents.Values;
        }
    }

    /// <summary>
    /// Represents the state of a single document
    /// </summary>
    public class DocumentState
    {
        public DocumentUri Uri { get; }
        public string Content { get; private set; }
        public string[] Lines { get; private set; }
        public List<Token> Tokens { get; private set; }
        public ProgramNode AST { get; private set; }
        public SemanticAnalyzer SemanticAnalyzer { get; private set; }
        public List<Diagnostic> Diagnostics { get; private set; }
        public bool ParseSuccessful { get; private set; }
        public bool SemanticSuccessful { get; private set; }

        public DocumentState(DocumentUri uri, string content)
        {
            Uri = uri;
            Content = content;
            Lines = content.Split('\n');
            Tokens = new List<Token>();
            Diagnostics = new List<Diagnostic>();
        }

        /// <summary>
        /// Parse the document and perform semantic analysis
        /// </summary>
        public void Parse()
        {
            Diagnostics.Clear();
            ParseSuccessful = false;
            SemanticSuccessful = false;

            try
            {
                // Lexical analysis
                var lexer = new Lexer(Content);
                Tokens = lexer.Tokenize();

                // Parsing
                var parser = new Parser(Tokens);
                AST = parser.Parse();
                ParseSuccessful = true;

                // Semantic analysis
                SemanticAnalyzer = new SemanticAnalyzer();
                SemanticSuccessful = SemanticAnalyzer.Analyze(AST);

                // Collect semantic errors
                foreach (var error in SemanticAnalyzer.Errors)
                {
                    Diagnostics.Add(new Diagnostic
                    {
                        Message = error.Message,
                        Severity = error.Severity == BasicLang.Compiler.SemanticAnalysis.ErrorSeverity.Warning
                            ? DiagnosticSeverity.Warning
                            : DiagnosticSeverity.Error,
                        Line = error.Line,
                        Column = error.Column
                    });
                }
            }
            catch (ParseException ex)
            {
                Diagnostics.Add(new Diagnostic
                {
                    Message = ex.Message,
                    Severity = DiagnosticSeverity.Error,
                    Line = ex.Token?.Line ?? 1,
                    Column = ex.Token?.Column ?? 1
                });
            }
            catch (Exception ex)
            {
                Diagnostics.Add(new Diagnostic
                {
                    Message = $"Internal error: {ex.Message}",
                    Severity = DiagnosticSeverity.Error,
                    Line = 1,
                    Column = 1
                });
            }
        }

        /// <summary>
        /// Get the word at a specific position
        /// </summary>
        public string GetWordAtPosition(int line, int character)
        {
            if (line < 0 || line >= Lines.Length)
                return null;

            var lineText = Lines[line];
            if (character < 0 || character >= lineText.Length)
                return null;

            // Find word boundaries
            int start = character;
            int end = character;

            while (start > 0 && IsIdentifierChar(lineText[start - 1]))
                start--;

            while (end < lineText.Length && IsIdentifierChar(lineText[end]))
                end++;

            if (start == end)
                return null;

            return lineText.Substring(start, end - start);
        }

        /// <summary>
        /// Get the token at a specific position
        /// </summary>
        public Token GetTokenAtPosition(int line, int character)
        {
            // Lines in LSP are 0-based, but our tokens are 1-based
            int targetLine = line + 1;

            foreach (var token in Tokens)
            {
                if (token.Line == targetLine)
                {
                    int tokenEnd = token.Column + (token.Lexeme?.Length ?? 0);
                    if (character >= token.Column - 1 && character < tokenEnd - 1)
                    {
                        return token;
                    }
                }
            }

            return null;
        }

        private bool IsIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }

    /// <summary>
    /// Simple diagnostic class for internal use
    /// </summary>
    public class Diagnostic
    {
        public string Message { get; set; }
        public DiagnosticSeverity Severity { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
    }

    public enum DiagnosticSeverity
    {
        Error = 1,
        Warning = 2,
        Information = 3,
        Hint = 4
    }
}
