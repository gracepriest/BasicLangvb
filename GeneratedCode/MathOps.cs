using System;
using System.Collections.Generic;

namespace GeneratedCode
{
    public class MathOps
    {
        public static void Main()
        {
            double x = 0.0;
            double y = 0.0;
            double result = 0.0;

            x = -5.5;
            y = 2;
            Console.WriteLine(Math.Abs(x));
            Console.WriteLine(Math.Sqrt(16));
            Console.WriteLine(Math.Pow(2, 8));
            Console.WriteLine(Math.Floor(3.7));
            Console.WriteLine(Math.Ceiling(3.2));
            Console.WriteLine(Math.Round(3.5));
            Console.WriteLine(Math.Min(x, y));
            Console.WriteLine(Math.Max(x, y));
            Console.WriteLine(Math.Sin(0));
            Console.WriteLine(Math.Cos(0));
            Console.WriteLine(Math.Log(2.718281828));
            Console.WriteLine(Math.Exp(1));
        }

    }
}
