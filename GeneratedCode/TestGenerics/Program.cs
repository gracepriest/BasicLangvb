using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeneratedCode
{
    public class Box<T>
    {
        public T Value;

        public T GetValue()
        {
            return Value;
        }

    }

    public class TestGenerics
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            T temp = null;

            temp = a;
            a = b;
            b = temp;
        }

        public static void Main()
        {
            Console.WriteLine("Generics test");
        }

    }
}
