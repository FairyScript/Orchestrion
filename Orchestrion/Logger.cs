using Orchestrion.Components;
using System;
namespace Orchestrion
{
    public static class Logger
    {
        public static event Action<string> OnDebug;
        public static event Action<string> OnInfo;
        public static event Action<string> OnWarning;
        public static event Action<string> OnError;

        public static void Debug(string text)
        {
            ConsoleOutput(text, "Debug");
            OnDebug?.Invoke(text);
        }
        public static void Info(string text)
        {
            ConsoleOutput(text, "Info");
            OnInfo?.Invoke(text);
        }
        public static void Warning(string text)
        {
            ConsoleOutput(text, "Warning");
            OnWarning?.Invoke(text);
        }
        public static void Error(string text)
        {
            ConsoleOutput(text, "Error");
            TopmostMessageBox.Show(text);
            OnError?.Invoke(text);
        }

        static void ConsoleOutput(string text, string type)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] - {type}: {text}");
        }
    }
}
