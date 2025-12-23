using System;
using System.Collections.Generic;

namespace GeneratedCode
{
    public class Fibonacci
    {
        public static int Fibonacci(int n)
        {
            if (n <= 1)
            {
                return n;
            }
            else
            {
                return Fibonacci(n - 1) + Fibonacci(n - 2);
            }
        }

        public static void Main()
        {
            int result = default(int);

            result = Fibonacci(10);
            return;
        }

    }
}
