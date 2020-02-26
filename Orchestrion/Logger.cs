using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchestrion
{
    public static class Logger
    {
        public static event Action<object> OnDebug;
        public static event Action<object> OnInfo;
        public static event Action<object> OnWarning;
        public static event Action<object> OnError;

        public static void Debug(string text)
        {
            ConsoleOutput(text,"Debug");
            OnDebug?.Invoke(text);
        }
        public static void Info(string text)
        {
            ConsoleOutput(text,"Info");
            OnInfo?.Invoke(text);
        }
        public static void Warning(string text)
        {
            ConsoleOutput(text,"Warning");
            OnWarning?.Invoke(text);
        }
        public static void Error(string text)
        {
            ConsoleOutput(text,"Error");
            OnError?.Invoke(text);
        }

        static void ConsoleOutput(string text,string type)
        {
            DateTime time = DateTime.Now;
            Console.WriteLine($"[{time.ToString("HH:mm:ss")}] - {type}: {text}");
        }
    }
}
