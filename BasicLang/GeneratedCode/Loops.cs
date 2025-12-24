using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeneratedCode
{
    public class Loops
    {
        public static int SumNumbers(int n)
        {
            int sum = 0;
            int i = 0;

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
            int total = 0;

            total = SumNumbers(100);
        }

    }
}

