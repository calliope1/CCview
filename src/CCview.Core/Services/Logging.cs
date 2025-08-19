using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCview.Core.Services
{
    public class Logging
    {
        private static readonly bool IsDebugEnabled = true; // Set to false in production
        public static void LogDebug(string message)
        {
            if (IsDebugEnabled)
            {
                Console.WriteLine($"DEBUG: {message}");
            }
        }
    }
}
