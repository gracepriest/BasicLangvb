using System;
using System.IO;
using BasicLang.Compiler;
using BasicLang.Compiler.Parsing;
using BasicLang.Compiler.SemanticAnalysis;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.CodeGen.CPlusPlus;

namespace BasicLang
{
    /// <summary>
    /// Test program to demonstrate C++ OOP code generation
    /// </summary>
    public class TestCppOOPDemo
    {
        public static void RunDemo()
        {
            Console.WriteLine("=== C++ OOP Code Generator Test ===\n");

            // Read test file
            var testFile = "TestCppOOP.bas";
            if (!File.Exists(testFile))
            {
                Console.WriteLine($"Error: Test file '{testFile}' not found!");
                return;
            }

            var sourceCode = File.ReadAllText(testFile);
            Console.WriteLine("Source Code:");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine(sourceCode);
            Console.WriteLine(new string('=', 60));
            Console.WriteLine();

            try
            {
                // Lexical analysis
                var lexer = new Lexer(sourceCode);
                var tokens = lexer.Tokenize();

                // Parsing
                var parser = new Parser(tokens);
                var ast = parser.ParseProgram();

                // Semantic analysis
                var semanticAnalyzer = new SemanticAnalyzer();
                semanticAnalyzer.Analyze(ast);

                if (semanticAnalyzer.Errors.Count > 0)
                {
                    Console.WriteLine("Semantic Errors:");
                    foreach (var error in semanticAnalyzer.Errors)
                        Console.WriteLine($"  - {error}");
                    return;
                }

                // IR generation
                var irBuilder = new IRBuilder();
                var irModule = irBuilder.BuildModule(ast);

                // C++ code generation
                var cppGenerator = new CppCodeGenerator(new CppCodeGenOptions
                {
                    IndentSize = 4,
                    GenerateComments = true,
                    GenerateMainFunction = true
                });

                var cppCode = cppGenerator.Generate(irModule);

                Console.WriteLine("Generated C++ Code:");
                Console.WriteLine(new string('=', 60));
                Console.WriteLine(cppCode);
                Console.WriteLine(new string('=', 60));
                Console.WriteLine();

                // Save to file
                var outputFile = "TestCppOOP.cpp";
                File.WriteAllText(outputFile, cppCode);
                Console.WriteLine($"C++ code saved to: {outputFile}");

                // Show key OOP features
                Console.WriteLine("\n=== OOP Features Demonstrated ===");
                Console.WriteLine("1. Class declarations with inheritance (: public BaseClass)");
                Console.WriteLine("2. Constructor generation with initializer lists");
                Console.WriteLine("3. Virtual destructor (when class has virtual methods)");
                Console.WriteLine("4. Getter/setter methods with const correctness");
                Console.WriteLine("5. Virtual methods with override keyword");
                Console.WriteLine("6. Static members with external initialization");
                Console.WriteLine("7. Access specifiers (public/private/protected)");
                Console.WriteLine("8. Base class method calls (BaseClass::method)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
