using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace RockBLOCK9603ConsoleApp
{
    class Program
    {
        private static Task ReceiverTask;
        private static SerialPort SerialPort;
        private static AutoResetEvent AutoEvent;
        private static ManualResetEvent ManualEvent;
        private static CancellationTokenSource TokenSource;


        private static void Configuration(string[] args)
        {
            if (args.Length != 1)
            {
                throw new Exception("Failed to start correctly");
            }
            AutoEvent = new AutoResetEvent(false);
            ManualEvent = new ManualResetEvent(false);
            TokenSource = new CancellationTokenSource();
            SerialPort = new SerialPort()
            {
                PortName = args[0],
                BaudRate = 19200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One
            };
            SerialPort.Open();
            return;
        }

        #region Reader
        private static void Reader(CancellationToken token)
        {
            string response = null;
            string str = null;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                response = VERBOSE_READ(out str);

                StandardIOWrapper.WriteLine(
                    "ISU>  " + response);

                if (IS_OK(str) ||
                    IS_ERROR(str))
                {
                    AutoEvent.Set();
                }
                else if (IS_READY(str))
                {
                    ManualEvent.Set();
                    AutoEvent.Set();
                }
                else if (ManualEvent.WaitOne(0) &&
                    IS_SBD_WRITE_TIMEOUT(str))
                {
                    ManualEvent.Reset();
                    AutoEvent.Set();
                }
            }
        }
        private static string VERBOSE_READ(out string str)
        {
            int readTimeout = -1;
            string response = string.Empty;
            while (true)
            {
                string tmp = SerialPort.GetString(1, readTimeout);
                if (tmp == "\r")
                {
                    tmp = SerialPort.ReadTo("\r\n");
                    tmp = tmp.Replace("\n", "");
                    response += tmp;
                    response += " ";
                }
                else if (char.IsLetterOrDigit(tmp[0]) || 
                    char.IsSymbol(tmp[0]))
                {
                    tmp += SerialPort.ReadTo("\r");
                    readTimeout = 30000;
                    response += tmp;
                    response += " ";
                }

                if (IS_OK(tmp) ||
                    IS_ERROR(tmp) ||
                    IS_READY(tmp) ||
                    IS_SBD_WRITE_TIMEOUT(tmp) ||
                    IS_SBDRING(tmp) ||
                    IS_CIEV(tmp) ||
                    IS_AREG(tmp))
                {
                    str = tmp;
                    return response;
                }
            }
        }
        private static bool IS_OK(string str)
        {
            return str == "OK";
        }
        private static bool IS_ERROR(string str)
        {
            return str == "ERROR";
        }
        private static bool IS_READY(string str)
        {
            return str == "READY";
        }
        private static bool IS_SBD_WRITE_TIMEOUT(string str)
        {
            return str == "1";
        }
        private static bool IS_SBDRING(string str)
        {
            return str == "SBDRING";
        }
        private static bool IS_CIEV(string str)
        {
            return str.StartsWith("+CIEV");
        }
        private static bool IS_AREG(string str)
        {
            return str.StartsWith("+AREG");
        }
        #endregion

        #region Writer
        private static void Writer()
        {
            int waitTimeout = 35000;
            string cmd = null;
            while (true)
            {
                Console.Write("DTE>  ");
                cmd = StandardIOWrapper.ReadLine() ?? "EXIT";

                if (!ManualEvent.WaitOne(0) &&
                    IS_AT_COMMAND(cmd))
                {
                    AutoEvent.Reset();
                    WRITE_AT_COMMAND(cmd);
                    AutoEvent.WaitOne(waitTimeout);
                }
                else if (cmd.Trim().ToUpper() == "EXIT")
                {
                    return;
                }
                else if (ManualEvent.WaitOne(0))
                {
                    WRITE_AT_COMMAND(cmd);
                    AutoEvent.WaitOne(waitTimeout);
                    ManualEvent.Reset();
                }
            }
        }
        private static void WRITE_AT_COMMAND(string cmd)
        {
            byte[] buf = new byte[cmd.Length + 1];
            buf[buf.Length - 1] = 0x0D;

            Encoding.ASCII.GetBytes(cmd, 0,
                cmd.Length, buf, 0);

            SerialPort.Write(buf, 0, buf.Length);
            return;
        }
        private static bool IS_AT_COMMAND(string cmd)
        {
            try
            {
                return cmd.TrimStart().Substring(0, 2).
                    ToUpper() == "AT";
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Shutdown
        private static void Shutdown(Exception e = null)
        {
            if (ReceiverTask != null)
            {
                if ((e == null) && ReceiverTask.IsFaulted)
                {
                    e = ReceiverTask.Exception.Flatten().InnerException;
                }
                else if (!ReceiverTask.IsCompleted)
                {
                    ShutdownReceiver();
                }               
            }
            SerialPort?.Dispose();

            if (e != null)
            {
                StandardIOWrapper.WriteLine(
                    "Error Message: " + e.Message);
            }
            return;
        }
        private static void ShutdownReceiver()
        {
            TokenSource.Cancel();
            SerialPort.ReadTimeout = 0;
            try
            {
                ReceiverTask.Wait();
            }
            catch { }
            return;
        }
        #endregion

        static void Main(string[] args)
        {
            try
            {
                Configuration(args);
                Console.WriteLine();
                ReceiverTask = Task.Delay(1000).ContinueWith(
                    (antecedent) =>
                    {
                        Reader(TokenSource.Token);
                    },
                    TaskContinuationOptions.LongRunning).ContinueWith(
                    (antecedent) =>
                    {
                        StandardIOWrapper.AbortReadLine = true;
                        throw antecedent.Exception;
                    },
                    TokenSource.Token);
                Writer();
                Shutdown();
            }
            catch (Exception e)
            {
                Shutdown(e);
            }
        }
    }
}
