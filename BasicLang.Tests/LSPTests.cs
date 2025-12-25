using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;
using BasicLang.Compiler.LSP;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace BasicLang.Tests
{
    /// <summary>
    /// Tests for LSP (Language Server Protocol) functionality
    /// </summary>
    public class LSPTests
    {
        // ====================================================================
        // DocumentManager Tests
        // ====================================================================

        [Fact]
        public void DocumentManager_UpdateDocument_ParsesContent()
        {
            var manager = new DocumentManager();
            var uri = DocumentUri.From("file:///test.bas");

            var state = manager.UpdateDocument(uri, "Dim x As Integer = 42");

            Assert.NotNull(state);
            Assert.True(state.ParseSuccessful);
            Assert.NotNull(state.Tokens);
            Assert.True(state.Tokens.Count > 0);
        }

        [Fact]
        public void DocumentManager_UpdateDocument_SameContent_ReturnsCached()
        {
            var manager = new DocumentManager();
            var uri = DocumentUri.From("file:///test.bas");
            var content = "Dim x As Integer = 42";

            var state1 = manager.UpdateDocument(uri, content);
            var state2 = manager.UpdateDocument(uri, content);

            // Should return same cached state (same reference)
            Assert.Same(state1, state2);
        }

        [Fact]
        public void DocumentManager_UpdateDocument_DifferentContent_ReturnsNewState()
        {
            var manager = new DocumentManager();
            var uri = DocumentUri.From("file:///test.bas");

            var state1 = manager.UpdateDocument(uri, "Dim x As Integer = 42");
            var state2 = manager.UpdateDocument(uri, "Dim x As Integer = 100");

            Assert.NotSame(state1, state2);
        }

        [Fact]
        public void DocumentManager_GetDocument_ReturnsStoredState()
        {
            var manager = new DocumentManager();
            var uri = DocumentUri.From("file:///test.bas");

            manager.UpdateDocument(uri, "Dim x As Integer = 42");
            var state = manager.GetDocument(uri);

            Assert.NotNull(state);
        }

        [Fact]
        public void DocumentManager_GetDocument_UnknownUri_ReturnsNull()
        {
            var manager = new DocumentManager();
            var uri = DocumentUri.From("file:///unknown.bas");

            var state = manager.GetDocument(uri);

            Assert.Null(state);
        }

        [Fact]
        public void DocumentManager_CloseDocument_RemovesFromCache()
        {
            var manager = new DocumentManager();
            var uri = DocumentUri.From("file:///test.bas");

            manager.UpdateDocument(uri, "Dim x As Integer = 42");
            manager.CloseDocument(uri);
            var state = manager.GetDocument(uri);

            Assert.Null(state);
        }

        [Fact]
        public void DocumentManager_GetAllDocuments_ReturnsAllOpenDocs()
        {
            var manager = new DocumentManager();
            var uri1 = DocumentUri.From("file:///test1.bas");
            var uri2 = DocumentUri.From("file:///test2.bas");

            manager.UpdateDocument(uri1, "Dim x As Integer");
            manager.UpdateDocument(uri2, "Dim y As String");

            var docs = manager.GetAllDocuments().ToList();

            Assert.Equal(2, docs.Count);
        }

        [Fact]
        public void DocumentManager_CacheStats_ReturnsCorrectCounts()
        {
            var manager = new DocumentManager();
            var uri = DocumentUri.From("file:///test.bas");

            manager.UpdateDocument(uri, "Dim x As Integer = 42");
            var (docCount, _) = manager.GetCacheStats();

            Assert.Equal(1, docCount);
        }

        // ====================================================================
        // DocumentState Tests
        // ====================================================================

        [Fact]
        public void DocumentState_ContentHash_ConsistentForSameContent()
        {
            var uri = DocumentUri.From("file:///test.bas");
            var content = "Dim x As Integer";

            var state1 = new DocumentState(uri, content);
            var state2 = new DocumentState(uri, content);

            Assert.Equal(state1.ContentHash, state2.ContentHash);
        }

        [Fact]
        public void DocumentState_ContentHash_DifferentForDifferentContent()
        {
            var uri = DocumentUri.From("file:///test.bas");

            var state1 = new DocumentState(uri, "Dim x As Integer");
            var state2 = new DocumentState(uri, "Dim y As String");

            Assert.NotEqual(state1.ContentHash, state2.ContentHash);
        }

        [Fact]
        public void DocumentState_GetWordAtPosition_ReturnsWord()
        {
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, "Dim myVariable As Integer");
            state.Parse();

            var word = state.GetWordAtPosition(0, 5); // Position within "myVariable"

            Assert.Equal("myVariable", word);
        }

        [Fact]
        public void DocumentState_GetWordAtPosition_OutOfBounds_ReturnsNull()
        {
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, "Dim x");
            state.Parse();

            var word = state.GetWordAtPosition(10, 0); // Line 10 doesn't exist

            Assert.Null(word);
        }

        [Fact]
        public void DocumentState_GetTokenAtPosition_ReturnsToken()
        {
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, "Dim myVariable As Integer");
            state.Parse();

            var token = state.GetTokenAtPosition(0, 5); // Position within "myVariable"

            Assert.NotNull(token);
            Assert.Equal(TokenType.Identifier, token.Type);
        }

        [Fact]
        public void DocumentState_Lines_SplitsCorrectly()
        {
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, "Line1\nLine2\nLine3");

            Assert.Equal(3, state.Lines.Length);
            Assert.Equal("Line1", state.Lines[0]);
        }

        // ====================================================================
        // SymbolService Tests
        // ====================================================================

        [Fact]
        public void SymbolService_GetHoverInfo_BuiltInKeyword_ReturnsDoc()
        {
            var symbolService = new SymbolService();
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, "Dim x As Integer");
            state.Parse();

            var hoverInfo = symbolService.GetHoverInfo(state, "Dim");

            Assert.NotNull(hoverInfo);
            Assert.Contains("Dim", hoverInfo);
        }

        [Fact]
        public void SymbolService_GetHoverInfo_BuiltInFunction_ReturnsDoc()
        {
            var symbolService = new SymbolService();
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, "Print(\"Hello\")");
            state.Parse();

            var hoverInfo = symbolService.GetHoverInfo(state, "Print");

            Assert.NotNull(hoverInfo);
            Assert.Contains("Print", hoverInfo);
        }

        [Fact]
        public void SymbolService_GetHoverInfo_CollectionsFunction_ReturnsDoc()
        {
            var symbolService = new SymbolService();
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, "Dim list = CreateList()");
            state.Parse();

            var hoverInfo = symbolService.GetHoverInfo(state, "CreateList");

            Assert.NotNull(hoverInfo);
            Assert.Contains("List", hoverInfo);
        }

        [Fact]
        public void SymbolService_GetHoverInfo_LinqFunction_ReturnsDoc()
        {
            var symbolService = new SymbolService();
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, "Dim result = Where(items, pred)");
            state.Parse();

            var hoverInfo = symbolService.GetHoverInfo(state, "Where");

            Assert.NotNull(hoverInfo);
            Assert.Contains("Filter", hoverInfo);
        }

        [Fact]
        public void SymbolService_GetHoverInfo_UnknownSymbol_ReturnsNull()
        {
            var symbolService = new SymbolService();
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, "Dim x As Integer");
            state.Parse();

            var hoverInfo = symbolService.GetHoverInfo(state, "UnknownFunction");

            Assert.Null(hoverInfo);
        }

        [Fact]
        public void SymbolService_GetDocumentSymbols_ReturnsSymbols()
        {
            var symbolService = new SymbolService();
            var uri = DocumentUri.From("file:///test.bas");
            var state = new DocumentState(uri, @"
Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function
");
            state.Parse();

            var symbols = symbolService.GetDocumentSymbols(state);

            Assert.NotEmpty(symbols);
            Assert.Contains(symbols, s => s.Name == "Add");
        }
    }
}
