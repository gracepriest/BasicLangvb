using System;
using System.IO;
using BasicLang.Compiler;
using BasicLang.Compiler.Lexing;
using BasicLang.Compiler.Parsing;
using BasicLang.Compiler.SemanticAnalysis;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.CodeGen.MSIL;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Read test file
            var sourceCode = File.ReadAllText("test_oop.bas");
            Console.WriteLine("=== Source Code ===");
            Console.WriteLine(sourceCode);
            Console.WriteLine();

            // Lexing
            var lexer = new Lexer(sourceCode, "test_oop.bas");
            var tokens = lexer.Tokenize();

            // Parsing
            var parser = new Parser(tokens);
            var ast = parser.Parse();

            // Semantic analysis
            var analyzer = new SemanticAnalyzer();
            analyzer.Analyze(ast);

            if (analyzer.Errors.Count > 0)
            {
                Console.WriteLine("=== Semantic Errors ===");
                foreach (var error in analyzer.Errors)
                {
                    Console.WriteLine(error);
                }
                return;
            }

            // IR generation
            var irBuilder = new IRBuilder(analyzer);
            var irModule = irBuilder.Build(ast, "TestOOP");

            // MSIL code generation
            var msilOptions = new MSILCodeGenOptions
            {
                GenerateComments = true,
                AssemblyName = "TestOOP"
            };
            var msilGen = new MSILCodeGenerator(msilOptions);
            var msilCode = msilGen.Generate(irModule);

            // Write output
            Console.WriteLine("=== Generated MSIL Code ===");
            Console.WriteLine(msilCode);

            // Save to file
            File.WriteAllText("test_oop.il", msilCode);
            Console.WriteLine();
            Console.WriteLine("MSIL code saved to test_oop.il");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
