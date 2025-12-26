using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GeneratedCode
{
    public class Stack<T>
    {
        private T items;
        private int count;

        public Stack()
        {
            count = 0;
        }

        public void Push(T item)
        {
        }

        public T Pop()
        {
            return items(count - 1);
        }

        public bool IsEmpty()
        {
            return count == 0;
        }

    }

    public class AdvancedFeatures
    {
        public static T Max<T>(T a, T b)
        {
            if (a > b)
            {
                return a;
            }
            else
            {
                return b;
            }
        }

        public static void ProcessData(int value)
        {
            try
            {
                if (value < 0)
                {
                    // Throw exception
                }
                Str(value);
                Console.WriteLine("Processing: " + Str(value));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                Console.WriteLine("Cleanup complete");
            }
        }

        public static void Main()
        {
            Stack intStack = default!;
            int maxVal = 0;

            Console.WriteLine("=== Generics Demo ===");
            intStack = new Stack();
            intStack.Push(10);
            intStack.Push(20);
            intStack.Push(30);
            Console.WriteLine("Popped: " + Str(intStack.Pop()));
            maxVal = Math.Max(42, 17);
            Console.WriteLine("Max value: " + Str(maxVal));
            Console.WriteLine("");
            Console.WriteLine("=== Exception Handling Demo ===");
            ProcessData(100);
            ProcessData(-5);
            Console.WriteLine("");
            Console.WriteLine("=== Lambda Demo ===");
            Console.WriteLine("Lambda syntax supported: Function(x) x * 2");
        }

    }
}

