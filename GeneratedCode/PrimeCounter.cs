using System;
using System.Collections.Generic;

namespace GeneratedCode
{
    public class PrimeCounter
    {
        public static bool IsPrime(int n)
        {
            int i = default(int);

            if (n <= 1)
            {
                return false;
            }
            if (n <= 3)
            {
                return true;
            }
            i = 2;
            while (i <= (n / 2))
            {
                if ((n % i) == 0)
                {
                    return false;
                }
                i = i + 1;
            }
            return true;
        }

        public static int CountPrimes(int max)
        {
            int count = default(int);
            int i = default(int);

            count = 0;
            i = 2;
            while (i <= max)
            {
                if (IsPrime(i))
                {
                    count = count + 1;
                }
                i = i + 1;
            }
            return count;
        }

        public static void Main()
        {
            int primeCount = default(int);

            primeCount = CountPrimes(100);
            return;
        }

    }
}
