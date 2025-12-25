using System;
using System.IO;
using BasicLang.Compiler;
using BasicLang.Compiler.CodeGen;
using BasicLang.Compiler.CodeGen.CSharp;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.SemanticAnalysis;

class Program
{
    static void Main(string[] args)
    {
        string sourceFile = Path.Combine("..", "test_generics_minimal.bl");
        string outputFile = Path.Combine("..", "GeneratedCode", "TestGenerics.cs");

        if (!File.Exists(sourceFile))
        {
            Console.WriteLine($"Error: Source file '{sourceFile}' not found!");
            Console.WriteLine($"Looking in: {Path.GetFullPath(sourceFile)}");
            return;
        }

        Console.WriteLine("BasicLang Generics Test Compiler");
        Console.WriteLine("=".PadRight(80, '='));

        // Read source code
        string sourceCode = File.ReadAllText(sourceFile);

        // Lexer
        var lexer = new Lexer(sourceCode);
        var tokens = lexer.Tokenize();
        Console.WriteLine($"✓ Lexical analysis: {tokens.Count} tokens");

        // Parser
        var parser = new Parser(tokens);
        var ast = default(BasicLang.Compiler.AST.ProgramNode);
        try
        {
            ast = parser.Parse();
            Console.WriteLine($"✓ Parsing: AST with {ast.Declarations.Count} declarations");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Parse exception: {ex.Message}");
        }

        if (parser.Errors.Count > 0)
        {
            Console.WriteLine($"✗ Parse errors: {parser.Errors.Count}");
            int shown = 0;
            foreach (var error in parser.Errors)
            {
                Console.WriteLine($"  {error}");
                if (++shown >= 20) break;
            }
            if (parser.Errors.Count > 20)
                Console.WriteLine($"  ... and {parser.Errors.Count - 20} more errors");
            return;
        }

        if (ast == null)
        {
            Console.WriteLine("✗ Failed to parse");
            return;
        }

        // Semantic analysis
        var semanticAnalyzer = new SemanticAnalyzer();
        bool semanticSuccess = semanticAnalyzer.Analyze(ast);
        Console.WriteLine($"✓ Semantic analysis: {semanticAnalyzer.Errors.Count} errors");

        if (!semanticSuccess)
        {
            foreach (var error in semanticAnalyzer.Errors)
            {
                Console.WriteLine($"  {error}");
            }
            return;
        }

        // IR generation
        var irBuilder = new IRBuilder(semanticAnalyzer);
        var irModule = irBuilder.Build(ast, "TestGenerics");
        Console.WriteLine($"✓ IR generation: {irModule.Functions.Count} functions, {irModule.Classes.Count} classes");

        // C# code generation
        var codeGen = new ImprovedCSharpCodeGenerator(new CodeGenOptions
        {
            Namespace = "GeneratedCode",
            ClassName = "TestGenerics"
        });
        string csharpCode = codeGen.Generate(irModule);
        Console.WriteLine($"✓ C# code generation: {csharpCode.Length} characters");

        // Save output
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
        File.WriteAllText(outputFile, csharpCode);
        Console.WriteLine($"✓ Saved to: {Path.GetFullPath(outputFile)}");

        Console.WriteLine();
        Console.WriteLine("Generated C# code:");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine(csharpCode);
    }
}
