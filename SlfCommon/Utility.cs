using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon
{
    public static class Utility
    {
        public static IEnumerable<byte> WriteString(string s)
        {
            List<byte> result = new List<byte>();

            byte[] sBytes = Encoding.UTF8.GetBytes(s);

            result.AddRange(BitConverter.IsLittleEndian
                ? BitConverter.GetBytes(sBytes.Length).Reverse()
                : BitConverter.GetBytes(sBytes.Length));

            result.AddRange(sBytes);

            return result;
        }

        public static string ReadString(IEnumerator<byte> bytes)
        {
            int byteCount = bytes.TakeInt();
            byte[] stringBytes = bytes.TakeBytes(byteCount);
            return Encoding.UTF8.GetString(stringBytes);
        }
    }
}
