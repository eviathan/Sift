using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sift.Logging
{
    public static class Logger
    {
        private const string LINE = "--------------------------------\n";

        internal static void WriteBanner(string message)
        {
            Console.WriteLine(LINE);
            Console.WriteLine(message);
            Console.WriteLine(LINE);
        }
    }
}