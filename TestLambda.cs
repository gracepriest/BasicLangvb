using System;
using System.IO;
using BasicLang.Compiler;
using BasicLang.Compiler.CodeGen.CSharp;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.IR.Optimization;
using BasicLang.Compiler.SemanticAnalysis;

class TestLambda
{
    static void Main()
    {
        var code = @"
Module TestLambda
    Sub Main()
        ' Simple single-line lambda
        Dim double = Function(x As Integer) x * 2
        Dim result = double(21)
        PrintLine(result)

        ' Lambda without explicit types
        Dim add = Function(a, b) a + b
        PrintLine(add(10, 20))

        ' Sub lambda (Action)
        Dim greet = Sub(name As String)
            PrintLine(""Hello, "" & name)
        End Sub
        greet(""World"")
    End Sub
End Module
";

        try
        {
            Console.WriteLine("Compiling lambda test...\n");

            // Lex
            var lexer = new BasicLangLexer(code);
            var tokens = lexer.Tokenize();
            Console.WriteLine($"✓ Lexical analysis: {tokens.Count} tokens");

            // Parse
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            Console.WriteLine($"✓ Parsing: AST with {ast.Declarations.Count} declarations");

            // Semantic analysis
            var analyzer = new SemanticAnalyzer();
            bool valid = analyzer.Analyze(ast);
            Console.WriteLine($"✓ Semantic analysis: {analyzer.Errors.Count} errors");

            if (!valid)
            {
                Console.WriteLine("\nSemantic Errors:");
                foreach (var error in analyzer.Errors)
                {
                    Console.WriteLine($"  - {error.Message}");
                }
                return;
            }

            // Build IR
            var irBuilder = new IRBuilder(analyzer);
            var module = irBuilder.Build(ast);
            Console.WriteLine($"✓ IR generation: {module.Functions.Count} functions");

            // Generate C#
            var csharpGen = new ImprovedCSharpCodeGenerator();
            var csharpCode = csharpGen.Generate(module);
            Console.WriteLine($"✓ C# code generation: {csharpCode.Length} characters\n");

            Console.WriteLine("Generated C#:");
            Console.WriteLine("=============");
            Console.WriteLine(csharpCode);

            // Save to file
            File.WriteAllText("test_lambda_output.cs", csharpCode);
            Console.WriteLine("\n✓ Saved to: test_lambda_output.cs");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
