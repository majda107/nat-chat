using System;

namespace NatChatCore
{
    public class Logger
    {
        public bool SendDeafen = true;
        public event EventHandler<string> OnLog;


        public void Log(string msg)
        {
            Console.ResetColor();
            Console.WriteLine(msg);

            this.OnLog?.Invoke(this, msg);
        }

        public void LogError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[ERROR] {msg}");
            Console.ResetColor();
        }

        public void LogDeafen(string msg)
        {
            if (!this.SendDeafen) return;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[DEAFEN] {msg}");
            Console.ResetColor();
        }
    }
}