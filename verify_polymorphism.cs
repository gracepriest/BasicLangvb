using System;
using System.Collections.Generic;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.SemanticAnalysis;
using BasicLang.Compiler.CodeGen.CSharp;

namespace BasicLang.Verification
{
    class PolymorphismVerification
    {
        static void Main(string[] args)
        {
            Console.WriteLine("BasicLang Polymorphism Feature Verification");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

            string code = @"
Class Animal
    Public Overridable Function Speak() As String
        Return ""...""
    End Function
End Class

Class Dog
    Inherits Animal

    Public Overrides Function Speak() As String
        Return ""Woof!""
    End Function
End Class

Sub Main()
    Dim animal As Animal
    animal = New Dog()
    Print animal.Speak()
End Sub
";

            try
            {
                // Lex
                var lexer = new BasicLangLexer(code);
                var tokens = lexer.Tokenize();
                Console.WriteLine($"[OK] Lexing completed: {tokens.Count} tokens");

                // Parse
                var parser = new Parser(tokens);
                var ast = parser.Parse();
                Console.WriteLine($"[OK] Parsing completed: {ast.Declarations.Count} declarations");

                // Check if virtual/override flags are set
                foreach (var decl in ast.Declarations)
                {
                    if (decl is ClassNode classNode)
                    {
                        Console.WriteLine($"\nClass: {classNode.Name}");
                        if (!string.IsNullOrEmpty(classNode.BaseClass))
                        {
                            Console.WriteLine($"  Inherits: {classNode.BaseClass}");
                        }

                        foreach (var member in classNode.Members)
                        {
                            if (member is FunctionNode func)
                            {
                                Console.WriteLine($"  Method: {func.Name}");
                                if (func.IsVirtual)
                                    Console.WriteLine($"    - IsVirtual: TRUE");
                                if (func.IsOverride)
                                    Console.WriteLine($"    - IsOverride: TRUE");
                                if (func.IsSealed)
                                    Console.WriteLine($"    - IsSealed: TRUE");
                                if (func.IsAbstract)
                                    Console.WriteLine($"    - IsAbstract: TRUE");
                            }
                        }
                    }
                }

                // Semantic analysis
                var analyzer = new SemanticAnalyzer();
                analyzer.Analyze(ast);
                Console.WriteLine($"\n[OK] Semantic analysis completed: {analyzer.Errors.Count} errors");

                if (analyzer.Errors.Count > 0)
                {
                    foreach (var error in analyzer.Errors)
                    {
                        Console.WriteLine($"  ERROR: {error.Message}");
                    }
                }

                // IR building
                var irBuilder = new IRBuilder(analyzer);
                var irModule = irBuilder.Build(ast);
                Console.WriteLine($"[OK] IR building completed: {irModule.Classes.Count} classes, {irModule.Functions.Count} functions");

                // Check IR for virtual/override flags
                foreach (var irClass in irModule.Classes.Values)
                {
                    Console.WriteLine($"\nIR Class: {irClass.Name}");
                    foreach (var method in irClass.Methods)
                    {
                        Console.WriteLine($"  Method: {method.Name}");
                        if (method.IsVirtual)
                            Console.WriteLine($"    - IR IsVirtual: TRUE");
                        if (method.IsOverride)
                            Console.WriteLine($"    - IR IsOverride: TRUE");
                        if (method.IsSealed)
                            Console.WriteLine($"    - IR IsSealed: TRUE");
                        if (method.IsAbstract)
                            Console.WriteLine($"    - IR IsAbstract: TRUE");
                    }
                }

                // C# code generation
                var codeGen = new ImprovedCSharpCodeGenerator();
                var csharpCode = codeGen.Generate(irModule);
                Console.WriteLine("\n[OK] C# code generation completed");

                Console.WriteLine("\nGenerated C# Code:");
                Console.WriteLine("=".PadRight(60, '='));
                Console.WriteLine(csharpCode);

                // Verify that C# code contains expected keywords
                bool hasVirtual = csharpCode.Contains("virtual ");
                bool hasOverride = csharpCode.Contains("override ");

                Console.WriteLine("\n" + "=".PadRight(60, '='));
                Console.WriteLine("Verification Results:");
                Console.WriteLine($"  virtual keyword found: {(hasVirtual ? "YES" : "NO")}");
                Console.WriteLine($"  override keyword found: {(hasOverride ? "YES" : "NO")}");

                if (hasVirtual && hasOverride)
                {
                    Console.WriteLine("\n[SUCCESS] Polymorphism implementation verified!");
                }
                else
                {
                    Console.WriteLine("\n[FAILURE] Polymorphism implementation incomplete!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
