using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadTest.SharedBase.Helpers
{
    public static class DisplayHelper
    {
        public static void WriteLine(string message, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            var originalColor = Console.ForegroundColor;

            Console.ForegroundColor = consoleColor;

            Console.WriteLine(message);

            Console.ForegroundColor = originalColor;
        }
    }
}
