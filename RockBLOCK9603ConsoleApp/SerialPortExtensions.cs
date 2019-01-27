using System.Text;
using System.IO.Ports;


namespace RockBLOCK9603ConsoleApp
{
    static class SerialPortExtensions
    {
        public static string GetString(this SerialPort obj, int length, int readTimeout)
        {
            int tmp = obj.ReadTimeout;
            try
            {
                int count = 0;
                byte[] buf = new byte[length];
                obj.ReadTimeout = readTimeout;
                while (count < length)
                {
                    count += obj.Read(buf, count, length - count);
                }
                return Encoding.ASCII.GetString(buf);
            }
            finally
            {
                obj.ReadTimeout = tmp;
            }
        }
    }
}
