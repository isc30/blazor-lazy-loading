using System;

namespace MyLogger
{
    public static class Logger
    {
        public static void Log(string text)
        {
            Console.WriteLine($"[LOGGER] {text}");
        }
    }
}
