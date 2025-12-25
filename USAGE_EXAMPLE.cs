// Example: Using the Enhanced Parser with Error Recovery
// This example shows how to use the improved parser to handle multiple syntax errors

using System;
using BasicLang.Compiler;

namespace BasicLang.Examples
{
    public class ParserErrorRecoveryExample
    {
        public static void Main(string[] args)
        {
            // Example 1: Code with multiple syntax errors
            string sourceCode = @"
Module MathOps
    Function Add(x As Integer, y As Integer) As Integer
        If x > 0
            Return x + y
        End If
        Return 0
    ' Missing End Function

    Sub PrintResult(value As Integer
        Print(value)
    End Sub
End Module
";

            Console.WriteLine("=== Parsing Code with Multiple Errors ===\n");
            Console.WriteLine("Source Code:");
            Console.WriteLine(sourceCode);
            Console.WriteLine("\n" + new string('=', 60) + "\n");

            try
            {
                // Tokenize
                var lexer = new BasicLangLexer(sourceCode);
                var tokens = lexer.Tokenize();

                // Parse with error recovery
                var parser = new Parser(tokens);
                var ast = parser.Parse();

                // Check for parsing errors
                if (parser.Errors.Count > 0)
                {
                    Console.WriteLine($"Found {parser.Errors.Count} parsing error(s):\n");

                    foreach (var error in parser.Errors)
                    {
                        // Print formatted error
                        Console.WriteLine(error.ToString());
                        Console.WriteLine();

                        // You can also access individual properties
                        // Console.WriteLine($"Line: {error.Token.Line}");
                        // Console.WriteLine($"Column: {error.Token.Column}");
                        // Console.WriteLine($"Context: {error.Context}");
                        // Console.WriteLine($"Message: {error.Message}");
                        // Console.WriteLine($"Suggestion: {error.Suggestion}");
                    }

                    Console.WriteLine(new string('-', 60));
                    Console.WriteLine("Note: Parser recovered from errors and returned partial AST.");
                    Console.WriteLine($"AST has {ast?.Declarations.Count ?? 0} top-level declarations.");
                }
                else
                {
                    Console.WriteLine("Parsing completed successfully!");
                    Console.WriteLine($"AST has {ast.Declarations.Count} top-level declarations.");
                }
            }
            catch (TooManyErrorsException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("The code has too many syntax errors. Please fix some and try again.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }

            Console.WriteLine("\n" + new string('=', 60) + "\n");

            // Example 2: Valid code
            string validCode = @"
Module Calculator
    Function Add(x As Integer, y As Integer) As Integer
        If x > 0 Then
            Return x + y
        End If
        Return 0
    End Function

    Sub PrintResult(value As Integer)
        Print(value)
    End Sub
End Module
";

            Console.WriteLine("=== Parsing Valid Code ===\n");
            Console.WriteLine("Source Code:");
            Console.WriteLine(validCode);
            Console.WriteLine("\n" + new string('=', 60) + "\n");

            try
            {
                var lexer2 = new BasicLangLexer(validCode);
                var tokens2 = lexer2.Tokenize();
                var parser2 = new Parser(tokens2);
                var ast2 = parser2.Parse();

                if (parser2.Errors.Count > 0)
                {
                    Console.WriteLine($"Found {parser2.Errors.Count} error(s)");
                    foreach (var error in parser2.Errors)
                    {
                        Console.WriteLine(error.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("SUCCESS: No parsing errors!");
                    Console.WriteLine($"AST has {ast2.Declarations.Count} top-level declarations.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\n" + new string('=', 60) + "\n");

            // Example 3: Demonstrating IDE integration scenario
            Console.WriteLine("=== IDE Integration Example ===\n");
            DemonstrateIDEIntegration();
        }

        /// <summary>
        /// Shows how an IDE would use the error collection to display diagnostics
        /// </summary>
        public static void DemonstrateIDEIntegration()
        {
            string userCode = @"
Function Calculate(x As Integer, y As Integer
    If x > y
        Return x
    Else
        Return y
    End If
End Function
";

            Console.WriteLine("Simulating IDE scenario where user is typing code...\n");
            Console.WriteLine("Code being edited:");
            Console.WriteLine(userCode);
            Console.WriteLine("\nIDE Error List:");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine(String.Format("{0,-6} {1,-8} {2,-10} {3}", "Line", "Column", "Context", "Message"));
            Console.WriteLine(new string('-', 80));

            var lexer = new BasicLangLexer(userCode);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var ast = parser.Parse();

            foreach (var error in parser.Errors)
            {
                // Extract simpler context for IDE error list
                string shortContext = error.Context.Replace(" in ", "");
                if (shortContext.Contains(" -> "))
                {
                    // Take just the immediate context
                    var parts = shortContext.Split(new[] { " -> " }, StringSplitOptions.None);
                    shortContext = parts[0];
                }

                Console.WriteLine(String.Format(
                    "{0,-6} {1,-8} {2,-10} {3}",
                    error.Token.Line,
                    error.Token.Column,
                    shortContext,
                    error.Message
                ));

                // IDE would show suggestion on hover or as a quick fix
                if (!string.IsNullOrEmpty(error.Suggestion))
                {
                    Console.WriteLine(String.Format(
                        "{0,-6} {1,-8} {2,-10} Quick Fix: {3}",
                        "", "", "", error.Suggestion
                    ));
                }
            }

            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"\nTotal errors: {parser.Errors.Count}");
            Console.WriteLine("IDE can still provide IntelliSense and analysis on valid portions of code.");
        }

        /// <summary>
        /// Example: Processing errors programmatically
        /// </summary>
        public static void ProcessErrorsProgrammatically(Parser parser)
        {
            // Group errors by line
            var errorsByLine = parser.Errors
                .GroupBy(e => e.Token.Line)
                .OrderBy(g => g.Key);

            foreach (var lineGroup in errorsByLine)
            {
                Console.WriteLine($"Line {lineGroup.Key}:");
                foreach (var error in lineGroup)
                {
                    Console.WriteLine($"  - {error.Message}");
                    if (!string.IsNullOrEmpty(error.Suggestion))
                    {
                        Console.WriteLine($"    Fix: {error.Suggestion}");
                    }
                }
            }

            // Find most common error type
            var errorTypes = parser.Errors
                .Select(e => e.Message.Split(' ')[0]) // Get first word (e.g., "Expected")
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (errorTypes != null)
            {
                Console.WriteLine($"\nMost common error type: {errorTypes.Key} ({errorTypes.Count()} occurrences)");
            }

            // Check if errors are concentrated in specific context
            var errorContexts = parser.Errors
                .Where(e => !string.IsNullOrEmpty(e.Context))
                .Select(e => e.Context)
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (errorContexts != null)
            {
                Console.WriteLine($"Most errors in: {errorContexts.Key} ({errorContexts.Count()} errors)");
            }
        }
    }
}

/* Expected Output:

=== Parsing Code with Multiple Errors ===

Source Code:
Module MathOps
    Function Add(x As Integer, y As Integer) As Integer
        If x > 0
            Return x + y
        End If
        Return 0
    ' Missing End Function

    Sub PrintResult(value As Integer
        Print(value)
    End Sub
End Module

============================================================

Found 3 parsing error(s):

Error at line 4, column 9 in Function 'Add' -> Module 'MathOps': Expected 'Then' after If condition but found Newline
  Suggestion: Did you forget 'Then' after the If condition?

Error at line 10, column 5 in Function 'Add' -> Module 'MathOps': Expected 'End Function' but found Sub
  Suggestion: Make sure your Function is properly closed with 'End Function'.

Error at line 10, column 38 in Sub 'PrintResult' -> Module 'MathOps': Expected ')' after parameters but found Newline
  Suggestion: Did you forget a closing parenthesis ')'?

------------------------------------------------------------
Note: Parser recovered from errors and returned partial AST.
AST has 1 top-level declarations.

============================================================

=== Parsing Valid Code ===

Source Code:
Module Calculator
    Function Add(x As Integer, y As Integer) As Integer
        If x > 0 Then
            Return x + y
        End If
        Return 0
    End Function

    Sub PrintResult(value As Integer)
        Print(value)
    End Sub
End Module

============================================================

SUCCESS: No parsing errors!
AST has 1 top-level declarations.

============================================================

=== IDE Integration Example ===

Simulating IDE scenario where user is typing code...

Code being edited:
Function Calculate(x As Integer, y As Integer
    If x > y
        Return x
    Else
        Return y
    End If
End Function

IDE Error List:
--------------------------------------------------------------------------------
Line   Column   Context    Message
--------------------------------------------------------------------------------
2      47       Function 'Calculate' Expected ')' after parameters but found Newline
                           Quick Fix: Did you forget a closing parenthesis ')'?
3      10       Function 'Calculate' Expected 'Then' after If condition but found Newline
                           Quick Fix: Did you forget 'Then' after the If condition?
--------------------------------------------------------------------------------

Total errors: 2
IDE can still provide IntelliSense and analysis on valid portions of code.

*/
