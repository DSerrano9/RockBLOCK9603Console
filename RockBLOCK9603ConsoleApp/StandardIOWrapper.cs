using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace RockBLOCK9603ConsoleApp
{
    public static class StandardIOWrapper
    {
        private static int CursorTop;
        private static bool PendingValue;
        private static string Value;
        private const int BufferSize = 340;
        private static readonly object Sync;
        private static readonly ManualResetEvent Start;
        private static readonly ManualResetEvent Stop;
        private enum StandardIOWrapperOptions { ReadLine, WriteLine }
        public static bool AbortReadLine { get; set; }


        static StandardIOWrapper()
        {
            CursorTop = Console.CursorTop;
            Sync = new object();
            Start = new ManualResetEvent(false);
            Stop = new ManualResetEvent(false);
            AbortReadLine = false;

            Console.SetIn(new StreamReader(Console.OpenStandardInput(BufferSize),
                Console.InputEncoding, false, BufferSize + 2));
            Task.Factory.StartNew(ValueListener, TaskCreationOptions.LongRunning);
        }

        #region WriteLine
        public static void WriteLine(string str)
        {
            ConsoleSync(StandardIOWrapperOptions.WriteLine, 
                Truncate(str));
            return;
        }
        private static string Truncate(string str)
        {
            return (str.Length > BufferSize)
                ? str.Substring(0, BufferSize)
                : str;
        }
        #endregion

        #region ReadLine
        public static string ReadLine()
        {
            PendingValue = true;
            string str = GetValue();
            ConsoleSync(StandardIOWrapperOptions.ReadLine);
            PendingValue = false;
            return str;
        }
        private static string GetValue()
        {
            Start.Set();
            while (true)
            {
                if (AbortReadLine)
                {
                    return null;
                }
                else if (Stop.WaitOne(60000))
                {
                    Stop.Reset();
                    return Value;
                }
            }
        }
        private static void ValueListener()
        {
            try
            {
                while (true)
                {
                    Start.WaitOne();
                    Start.Reset();
                    Value = Console.ReadLine();
                    Stop.Set();
                }
            }
            catch { }
        }
        #endregion

        private static void ConsoleSync(StandardIOWrapperOptions option, string str = null)
        {
            lock (Sync)
            {
                switch (option)
                {
                    case StandardIOWrapperOptions.WriteLine:
                        {
                            int savedCursorLeft = Console.CursorLeft;
                            int savedCursorTop = Console.CursorTop;
                            CursorTop += 6;

                            if (PendingValue)
                            {
                                Console.SetCursorPosition(0, CursorTop);
                                Console.Write(str);
                                Console.SetCursorPosition(savedCursorLeft, savedCursorTop);
                            }
                            else
                            {
                                Console.Write(str);
                                Console.SetCursorPosition(0, CursorTop);
                            }                        
                            break;
                        }
                    case StandardIOWrapperOptions.ReadLine:
                        {
                            CursorTop += 6;
                            Console.SetCursorPosition(0, CursorTop);
                            break;
                        }
                }
            }
            return;
        }
    }
}
