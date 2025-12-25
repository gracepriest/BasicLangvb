using System;
using System.IO;
using System.Linq;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;
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
        Dim doubler As Object
        doubler = Function(x As Integer) x * 2
        PrintLine(""Lambda created successfully"")
    End Sub
End Module
";

        try
        {
            Console.WriteLine("Compiling lambda test...\n");

            // Lex
            var lexer = new Lexer(code);
            var tokens = lexer.Tokenize();
            Console.WriteLine($"✓ Lexical analysis: {tokens.Count} tokens");

            // Parse
            var parser = new Parser(tokens);
            ProgramNode ast = null;

            try
            {
                ast = parser.Parse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nParse Exception: {ex.Message}");
                if (parser.Errors.Count > 0)
                {
                    Console.WriteLine($"\nParse Errors ({parser.Errors.Count}):");
                    foreach (var error in parser.Errors.Take(10))
                    {
                        Console.WriteLine($"  - Line {error.Token?.Line ?? 0}: {error.Message}");
                    }
                }
                return;
            }

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
