using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    internal static class Utility
    {
        public static int Time => (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        public static byte[] EncodeString(string str) => Encoding.UTF8.GetBytes(str);
        public static string DecodeString(byte[] data) => Encoding.UTF8.GetString(data);
    }
}
