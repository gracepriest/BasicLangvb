using System;
using System.Collections.Generic;

namespace GeneratedCode
{
    public class SelectCaseDemo
    {
        public static string GetDayName(int day)
        {
            string result = "";

            switch (day)
            {
                case 1:
                    result = "Monday";
                    break;
                case 2:
                    result = "Tuesday";
                    break;
                case 3:
                    result = "Wednesday";
                    break;
                case 4:
                    result = "Thursday";
                    break;
                case 5:
                    result = "Friday";
                    break;
                default:
                    result = "Weekend";
                    break;
            }
            return result;
        }

        public static void Main()
        {
            Console.WriteLine(GetDayName(1));
            Console.WriteLine(GetDayName(3));
            Console.WriteLine(GetDayName(6));
        }

    }
}
