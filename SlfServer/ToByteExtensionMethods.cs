using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer
{
    public static class ToByteExtensionMethods
    {
        public static byte[] ToBytes(this long value)
        {
            byte[] bytes = BitConverter.GetBytes((long)value);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return bytes;
        }
    }
}
