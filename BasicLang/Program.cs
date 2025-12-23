using System;
using System.IO;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;
using BasicLang.Compiler.CodeGen;
using BasicLang.Compiler.CodeGen.CSharp;
using BasicLang.Compiler.CodeGen.CPlusPlus;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.IR.Optimization;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Compiler.Driver
{
    /// <summary>
    /// Main entry point for the BasicLang compiler
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine("BasicLang Multi-Target Transpiler - Complete Pipeline Demo");
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine();

            // Run all demos
            DemoSimpleFunction();
            DemoFibonacci();
            DemoLoops();
            DemoArrays();
            DemoCompleteProgram();
            DemoStringOperations();
            DemoMathOperations();
            DemoTypeConversion();
            DemoArrayBoundsAndConcat();
            DemoSelectCase();
            DemoExitStatements();
            DemoDoLoopVariations();
            DemoRandomNumbers();

            Console.WriteLine();
            Console.WriteLine("Demo complete! Check the generated C# files in GeneratedCode folder.");
        }

        // ====================================================================
        // Demo 1: Simple Function
        // ====================================================================

        static void DemoSimpleFunction()
        {
            Console.WriteLine("Demo 1: Simple Function with PrintLine");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function

Sub Main()
    Dim result As Integer
    result = Add(5, 3)
    PrintLine(""The result is:"")
    PrintLine(result)
End Sub
";

            var csharpCode = CompileToCSharp(source, "SimpleFunction");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("SimpleFunction.cs", csharpCode);
        }



        // ====================================================================
        // Demo 2: Fibonacci
        // ====================================================================

        static void DemoFibonacci()
        {
            Console.WriteLine("Demo 2: Fibonacci Calculator");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Function Fibonacci(n As Integer) As Integer
    If n <= 1 Then
        Return n
    Else
        Return Fibonacci(n - 1) + Fibonacci(n - 2)
    End If
End Function

Sub Main()
    Dim result As Integer
    result = Fibonacci(10)
End Sub
";

            var csharpCode = CompileToCSharp(source, "Fibonacci");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("Fibonacci.cs", csharpCode);
        }

        // ====================================================================
        // Demo 3: Loops
        // ====================================================================

        static void DemoLoops()
        {
            Console.WriteLine("Demo 3: Loop Examples");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Function SumNumbers(n As Integer) As Integer
    Dim sum As Integer
    Dim i As Integer
    
    sum = 0
    
    For i = 1 To n
        sum = sum + i
    Next i
    
    Return sum
End Function

Sub Main()
    Dim total As Integer
    total = SumNumbers(100)
End Sub
";

            var csharpCode = CompileToCSharp(source, "Loops");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("Loops.cs", csharpCode);
        }

        // ====================================================================
        // Demo 4: Arrays
        // ====================================================================

        static void DemoArrays()
        {
            Console.WriteLine("Demo 4: Array Processing");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Function FindMax(arr[10] As Integer) As Integer
    Dim max As Integer
    Dim i As Integer
    
    max = arr[0]
    
    For i = 1 To 9
        If arr[i] > max Then
            max = arr[i]
        End If
    Next i
    
    Return max
End Function

Sub Main()
    Dim numbers[10] As Integer
    Dim maximum As Integer
    maximum = FindMax(numbers)
End Sub
";

            var csharpCode = CompileToCSharp(source, "Arrays");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("Arrays.cs", csharpCode);
        }

        // ====================================================================
        // Demo 5: Complete Program
        // ====================================================================

        static void DemoCompleteProgram()
        {
            Console.WriteLine("Demo 5: Complete Program with Multiple Functions");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Function IsPrime(n As Integer) As Boolean
    Dim i As Integer
    
    If n <= 1 Then
        Return False
    End If
    
    If n <= 3 Then
        Return True
    End If
    
    For i = 2 To n \ 2
        If n % i = 0 Then
            Return False
        End If
    Next i
    
    Return True
End Function

Function CountPrimes(max As Integer) As Integer
    Dim count As Integer
    Dim i As Integer
    
    count = 0
    
    For i = 2 To max
        If IsPrime(i) Then
            count = count + 1
        End If
    Next i
    
    Return count
End Function

Sub Main()
    Dim primeCount As Integer
    primeCount = CountPrimes(100)
End Sub
";

            var csharpCode = CompileToCSharp(source, "PrimeCounter");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("PrimeCounter.cs", csharpCode);
        }

        // ====================================================================
        // Demo 6: String Operations
        // ====================================================================

        static void DemoStringOperations()
        {
            Console.WriteLine("Demo 6: String Operations");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Sub Main()
    Dim text As String
    Dim length As Integer
    Dim upper As String
    Dim lower As String
    Dim sub1 As String
    Dim sub2 As String
    Dim sub3 As String
    Dim trimmed As String
    Dim pos As Integer
    Dim replaced As String

    text = ""  Hello World  ""
    trimmed = Trim(text)
    length = Len(trimmed)
    upper = UCase(trimmed)
    lower = LCase(trimmed)
    sub1 = Left(trimmed, 5)
    sub2 = Right(trimmed, 5)
    sub3 = Mid(trimmed, 7, 5)
    pos = InStr(trimmed, ""World"")
    replaced = Replace(trimmed, ""World"", ""BasicLang"")

    PrintLine(trimmed)
    PrintLine(length)
    PrintLine(upper)
    PrintLine(lower)
    PrintLine(sub1)
    PrintLine(sub2)
    PrintLine(sub3)
    PrintLine(pos)
    PrintLine(replaced)
End Sub
";

            var csharpCode = CompileToCSharp(source, "StringOps");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("StringOps.cs", csharpCode);
        }

        // ====================================================================
        // Demo 7: Math Operations
        // ====================================================================

        static void DemoMathOperations()
        {
            Console.WriteLine("Demo 7: Math Operations");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Sub Main()
    Dim x As Double
    Dim y As Double
    Dim result As Double

    x = -5.5
    y = 2.0

    PrintLine(Abs(x))
    PrintLine(Sqrt(16.0))
    PrintLine(Pow(2.0, 8.0))
    PrintLine(Floor(3.7))
    PrintLine(Ceiling(3.2))
    PrintLine(Round(3.5))
    PrintLine(Min(x, y))
    PrintLine(Max(x, y))
    PrintLine(Sin(0.0))
    PrintLine(Cos(0.0))
    PrintLine(Log(2.718281828))
    PrintLine(Exp(1.0))
End Sub
";

            var csharpCode = CompileToCSharp(source, "MathOps");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("MathOps.cs", csharpCode);
        }

        // ====================================================================
        // Demo 8: Type Conversion
        // ====================================================================

        static void DemoTypeConversion()
        {
            Console.WriteLine("Demo 8: Type Conversion");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Sub Main()
    Dim strNum As String
    Dim intVal As Integer
    Dim dblVal As Double
    Dim strResult As String
    Dim boolVal As Boolean

    strNum = ""42""
    intVal = CInt(strNum)
    dblVal = CDbl(""3.14159"")
    strResult = CStr(intVal)
    boolVal = CBool(1)

    PrintLine(intVal)
    PrintLine(dblVal)
    PrintLine(strResult)
    PrintLine(boolVal)
End Sub
";

            var csharpCode = CompileToCSharp(source, "ConversionOps");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("ConversionOps.cs", csharpCode);
        }

        // ====================================================================
        // Demo 9: Array Bounds and String Concatenation
        // ====================================================================

        static void DemoArrayBoundsAndConcat()
        {
            Console.WriteLine("Demo 9: Array Bounds and String Concatenation");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Sub Main()
    Dim arr[5] As Integer
    Dim lower As Integer
    Dim upper As Integer
    Dim greeting As String
    Dim name As String
    Dim message As String

    lower = LBound(arr)
    upper = UBound(arr)

    name = ""World""
    greeting = ""Hello, "" & name & ""!""

    PrintLine(lower)
    PrintLine(upper)
    PrintLine(greeting)
End Sub
";

            var csharpCode = CompileToCSharp(source, "ArrayBoundsConcat");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("ArrayBoundsConcat.cs", csharpCode);
        }

        // ====================================================================
        // Demo 10: Select Case
        // ====================================================================

        static void DemoSelectCase()
        {
            Console.WriteLine("Demo 10: Select Case");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Function GetDayName(day As Integer) As String
    Dim result As String

    Select Case day
        Case 1
            result = ""Monday""
        Case 2
            result = ""Tuesday""
        Case 3
            result = ""Wednesday""
        Case 4
            result = ""Thursday""
        Case 5
            result = ""Friday""
        Case Else
            result = ""Weekend""
    End Select

    Return result
End Function

Sub Main()
    PrintLine(GetDayName(1))
    PrintLine(GetDayName(3))
    PrintLine(GetDayName(6))
End Sub
";

            var csharpCode = CompileToCSharp(source, "SelectCaseDemo");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("SelectCaseDemo.cs", csharpCode);
        }

        // ====================================================================
        // Demo 11: Exit Statements
        // ====================================================================

        static void DemoExitStatements()
        {
            Console.WriteLine("Demo 11: Exit Statements");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Function FindFirst(arr[10] As Integer, target As Integer) As Integer
    Dim i As Integer
    Dim result As Integer

    result = -1

    For i = 0 To 9
        If arr[i] = target Then
            result = i
            Exit For
        End If
    Next i

    Return result
End Function

Sub Main()
    Dim numbers[10] As Integer
    Dim found As Integer

    numbers[0] = 5
    numbers[1] = 10
    numbers[2] = 15
    numbers[3] = 20
    numbers[4] = 25
    numbers[5] = 30
    numbers[6] = 35
    numbers[7] = 40
    numbers[8] = 45
    numbers[9] = 50

    found = FindFirst(numbers, 25)
    PrintLine(found)

    found = FindFirst(numbers, 100)
    PrintLine(found)
End Sub
";

            var csharpCode = CompileToCSharp(source, "ExitStatements");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("ExitStatements.cs", csharpCode);
        }

        // ====================================================================
        // Demo 12: Do...Loop Variations
        // ====================================================================

        static void DemoDoLoopVariations()
        {
            Console.WriteLine("Demo 12: Do...Loop Variations");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Sub Main()
    Dim count As Integer

    PrintLine(""Do While...Loop (condition at start):"")
    count = 1
    Do While count <= 3
        PrintLine(count)
        count = count + 1
    Loop

    PrintLine(""Do Until...Loop (condition at start):"")
    count = 1
    Do Until count > 3
        PrintLine(count)
        count = count + 1
    Loop

    PrintLine(""Do...Loop While (condition at end):"")
    count = 1
    Do
        PrintLine(count)
        count = count + 1
    Loop While count <= 3

    PrintLine(""Do...Loop Until (condition at end):"")
    count = 1
    Do
        PrintLine(count)
        count = count + 1
    Loop Until count > 3
End Sub
";

            var csharpCode = CompileToCSharp(source, "DoLoopDemo");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("DoLoopDemo.cs", csharpCode);
        }

        // ====================================================================
        // Demo 13: Random Numbers
        // ====================================================================

        static void DemoRandomNumbers()
        {
            Console.WriteLine("Demo 13: Random Numbers");
            Console.WriteLine("-".PadRight(70, '-'));

            string source = @"
Sub Main()
    Dim i As Integer
    Dim rand As Double
    Dim diceRoll As Integer

    PrintLine(""Random numbers (0 to 1):"")
    For i = 1 To 5
        rand = Rnd()
        PrintLine(rand)
    Next i

    PrintLine(""Simulated dice rolls:"")
    For i = 1 To 6
        diceRoll = CInt(Floor(Rnd() * 6.0)) + 1
        PrintLine(diceRoll)
    Next i
End Sub
";

            var csharpCode = CompileToCSharp(source, "RandomDemo");

            Console.WriteLine("Generated C#:");
            Console.WriteLine(csharpCode);
            Console.WriteLine();

            SaveToFile("RandomDemo.cs", csharpCode);
        }

        // ====================================================================
        // Compilation Pipeline
        // ====================================================================

        static string CompileToCSharp(string source, string className)
        {
            try
            {
                // Phase 1: Lexical Analysis
                var lexer = new Lexer(source);
                var tokens = lexer.Tokenize();

                Console.WriteLine($"✓ Lexical analysis: {tokens.Count} tokens");

                // Phase 2: Parsing
                var parser = new Parser(tokens);
                var ast = parser.Parse();

                Console.WriteLine($"✓ Parsing: AST with {ast.Declarations.Count} declarations");

                // Phase 3: Semantic Analysis
                var semanticAnalyzer = new SemanticAnalyzer();
                bool semanticSuccess = semanticAnalyzer.Analyze(ast);

                if (!semanticSuccess)
                {
                    Console.WriteLine("✗ Semantic analysis failed:");
                    foreach (var error in semanticAnalyzer.Errors)
                    {
                        Console.WriteLine($"  {error}");
                    }
                    return null;
                }

                Console.WriteLine($"✓ Semantic analysis: {semanticAnalyzer.Errors.Count} errors");

                // Phase 4: IR Generation
                var irBuilder = new IRBuilder(semanticAnalyzer);
                var irModule = irBuilder.Build(ast, className);

                Console.WriteLine($"✓ IR generation: {irModule.Functions.Count} functions");

                // Phase 5: Optimization
                var optimizer = new OptimizationPipeline();
                optimizer.AddStandardPasses();

                var optimizationResult = optimizer.Run(irModule);

                Console.WriteLine($"✓ Optimization: {optimizationResult.TotalModifications} improvements");

                // Phase 6: C# Code Generation
                var codeGenOptions = new CodeGenOptions
                {
                    Namespace = "GeneratedCode",
                    ClassName = className,
                    GenerateMainMethod = true
                };

                var csharpGenerator = new CSharpCodeGenerator(codeGenOptions);
                var csharpCode = csharpGenerator.Generate(irModule);

                Console.WriteLine($"✓ C# code generation: {csharpCode.Length} characters");

                return csharpCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Compilation failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Compile BasicLang source to C++
        /// </summary>
        static string CompileToCpp(string source, string className)
        {
            try
            {
                // Phase 1: Lexical Analysis
                var lexer = new Lexer(source);
                var tokens = lexer.Tokenize();

                Console.WriteLine($"✓ Lexical analysis: {tokens.Count} tokens");

                // Phase 2: Parsing
                var parser = new Parser(tokens);
                var ast = parser.Parse();

                Console.WriteLine($"✓ Parsing: AST with {ast.Declarations.Count} declarations");

                // Phase 3: Semantic Analysis
                var semanticAnalyzer = new SemanticAnalyzer();
                bool semanticSuccess = semanticAnalyzer.Analyze(ast);

                if (!semanticSuccess)
                {
                    Console.WriteLine("✗ Semantic analysis failed:");
                    foreach (var error in semanticAnalyzer.Errors)
                    {
                        Console.WriteLine($"  {error}");
                    }
                    return null;
                }

                Console.WriteLine($"✓ Semantic analysis: {semanticAnalyzer.Errors.Count} errors");

                // Phase 4: IR Generation
                var irBuilder = new IRBuilder(semanticAnalyzer);
                var irModule = irBuilder.Build(ast, className);

                Console.WriteLine($"✓ IR generation: {irModule.Functions.Count} functions");

                // Phase 5: Optimization
                var optimizer = new OptimizationPipeline();
                optimizer.AddStandardPasses();

                var optimizationResult = optimizer.Run(irModule);

                Console.WriteLine($"✓ Optimization: {optimizationResult.TotalModifications} improvements");

                // Phase 6: C++ Code Generation
                var cppOptions = new CppCodeGenOptions
                {
                    GenerateComments = true,
                    GenerateMainFunction = true
                };

                var cppGenerator = new CppCodeGenerator(cppOptions);
                var cppCode = cppGenerator.Generate(irModule);

                Console.WriteLine($"✓ C++ code generation: {cppCode.Length} characters");

                return cppCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Compilation failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Generic compile method using BackendRegistry
        /// </summary>
        static string Compile(string source, string className, TargetPlatform target)
        {
            try
            {
                // Phase 1: Lexical Analysis
                var lexer = new Lexer(source);
                var tokens = lexer.Tokenize();

                // Phase 2: Parsing
                var parser = new Parser(tokens);
                var ast = parser.Parse();

                // Phase 3: Semantic Analysis
                var semanticAnalyzer = new SemanticAnalyzer();
                if (!semanticAnalyzer.Analyze(ast))
                {
                    return null;
                }

                // Phase 4: IR Generation
                var irBuilder = new IRBuilder(semanticAnalyzer);
                var irModule = irBuilder.Build(ast, className);

                // Phase 5: Optimization
                var optimizer = new OptimizationPipeline();
                optimizer.AddStandardPasses();
                optimizer.Run(irModule);

                // Phase 6: Code Generation using BackendRegistry
                var options = new CodeGenOptions
                {
                    Namespace = "GeneratedCode",
                    ClassName = className,
                    GenerateMainMethod = true,
                    TargetBackend = target
                };

                var generator = BackendRegistry.Create(target, options);
                return generator.Generate(irModule);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Compilation failed: {ex.Message}");
                return null;
            }
        }

        static void SaveToFile(string filename, string content)
        {
            if (content == null) return;
            
            try
            {
                string outputPath = Path.Combine("GeneratedCode", filename);
                Directory.CreateDirectory("GeneratedCode");
                File.WriteAllText(outputPath, content);
                Console.WriteLine($"✓ Saved to: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to save file: {ex.Message}");
            }
        }
    }
}
