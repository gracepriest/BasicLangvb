using System;
using System.Collections.Generic;
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
    /// Handles rename symbol requests
    /// </summary>
    public class RenameHandler : RenameHandlerBase
    {
        private readonly DocumentManager _documentManager;

        public RenameHandler(DocumentManager documentManager)
        {
            _documentManager = documentManager;
        }

        public override Task<WorkspaceEdit> Handle(RenameParams request, CancellationToken cancellationToken)
        {
            var state = _documentManager.GetDocument(request.TextDocument.Uri);
            if (state == null)
            {
                return Task.FromResult<WorkspaceEdit>(null);
            }

            // Get the word at the cursor position
            var word = state.GetWordAtPosition(request.Position.Line, request.Position.Character);
            if (string.IsNullOrEmpty(word))
            {
                return Task.FromResult<WorkspaceEdit>(null);
            }

            var newName = request.NewName;
            var edits = new List<TextEdit>();

            // Find all references to the word and create edits
            foreach (var token in state.Tokens)
            {
                if (token.Type == TokenType.Identifier &&
                    string.Equals(token.Lexeme, word, StringComparison.OrdinalIgnoreCase))
                {
                    edits.Add(new TextEdit
                    {
                        Range = new LspRange(
                            new Position(token.Line - 1, token.Column - 1),
                            new Position(token.Line - 1, token.Column - 1 + token.Lexeme.Length)),
                        NewText = newName
                    });
                }
            }

            if (edits.Count == 0)
            {
                return Task.FromResult<WorkspaceEdit>(null);
            }

            var workspaceEdit = new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    { state.Uri, edits }
                }
            };

            return Task.FromResult(workspaceEdit);
        }

        protected override RenameRegistrationOptions CreateRegistrationOptions(
            RenameCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new RenameRegistrationOptions
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("basiclang"),
                PrepareProvider = true
            };
        }
    }

    /// <summary>
    /// Handles prepare rename requests (validates if rename is possible)
    /// </summary>
    public class PrepareRenameHandler : PrepareRenameHandlerBase
    {
        private readonly DocumentManager _documentManager;

        public PrepareRenameHandler(DocumentManager documentManager)
        {
            _documentManager = documentManager;
        }

        public override Task<RangeOrPlaceholderRange> Handle(PrepareRenameParams request, CancellationToken cancellationToken)
        {
            var state = _documentManager.GetDocument(request.TextDocument.Uri);
            if (state == null)
            {
                return Task.FromResult<RangeOrPlaceholderRange>(null);
            }

            // Get the word at the cursor position
            var word = state.GetWordAtPosition(request.Position.Line, request.Position.Character);
            if (string.IsNullOrEmpty(word))
            {
                return Task.FromResult<RangeOrPlaceholderRange>(null);
            }

            // Find the token at this position
            var token = state.GetTokenAtPosition(request.Position.Line, request.Position.Character);
            if (token == null || token.Type != TokenType.Identifier)
            {
                return Task.FromResult<RangeOrPlaceholderRange>(null);
            }

            // Return the range and placeholder
            var range = new LspRange(
                new Position(token.Line - 1, token.Column - 1),
                new Position(token.Line - 1, token.Column - 1 + token.Lexeme.Length));

            return Task.FromResult<RangeOrPlaceholderRange>(new RangeOrPlaceholderRange(
                new PlaceholderRange
                {
                    Range = range,
                    Placeholder = word
                }));
        }

        protected override RenameRegistrationOptions CreateRegistrationOptions(
            RenameCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new RenameRegistrationOptions
            {
                DocumentSelector = TextDocumentSelector.ForLanguage("basiclang"),
                PrepareProvider = true
            };
        }
    }
}
