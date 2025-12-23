using System;
using System.Collections.Generic;

namespace GeneratedCode
{
    public class Loops
    {
        public static int SumNumbers(int n)
        {
            int sum = default(int);
            int i = default(int);

            sum = 0;
            i = 1;
            while (i <= n)
            {
                sum = sum + i;
                i = i + 1;
            }
            return sum;
        }

        public static void Main()
        {
            int total = default(int);

            total = SumNumbers(100);
            return;
        }

    }
}
