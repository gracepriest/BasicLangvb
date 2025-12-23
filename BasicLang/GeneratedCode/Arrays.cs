using System;
using System.Collections.Generic;

namespace GeneratedCode
{
    public class Arrays
    {
        public static int FindMax(int[] arr)
        {
            int max = default(int);
            int i = default(int);

            max = arr[0];
            i = 1;
            while (i <= 9)
            {
                if (arr[i] > max)
                {
                    max = arr[i];
                }
                i = i + 1;
            }
            return max;
        }

        public static void Main()
        {
            int[] numbers = default(int[]);
            int maximum = default(int);

            maximum = FindMax(numbers);
            return;
        }

    }
}
