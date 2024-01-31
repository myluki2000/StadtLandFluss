using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon
{
    public static class ByteEnumeratorExtensionMethods
    {
        public static byte[] TakeBytes(this IEnumerator<byte> enumerator, int count)
        {
            byte[] result = new byte[count];

            for (int i = 0; i < count; ++i)
            {
                enumerator.MoveNext();
                result[i] = enumerator.Current;
            }

            return result;
        }

        public static bool TakeBool(this IEnumerator<byte> e)
        {
            e.MoveNext();
            return e.Current != 0;
        }

        public static sbyte TakeSByte(this IEnumerator<byte> e)
        {
            e.MoveNext();
            return (sbyte)e.Current;
        }

        public static byte TakeByte(this IEnumerator<byte> e)
        {
            e.MoveNext();
            return (byte)e.Current;
        }

        public static short TakeShort(this IEnumerator<byte> e)
        {
            byte[] shortBytes = e.TakeBytes(2);
            if (BitConverter.IsLittleEndian) Array.Reverse(shortBytes);
            short s = BitConverter.ToInt16(shortBytes);
            return s;
        }

        public static ushort TakeUShort(this IEnumerator<byte> e)
        {
            byte[] shortBytes = e.TakeBytes(2);
            if (BitConverter.IsLittleEndian) Array.Reverse(shortBytes);
            ushort s = BitConverter.ToUInt16(shortBytes);
            return s;
        }

        public static int TakeInt(this IEnumerator<byte> e)
        {
            byte[] intBytes = e.TakeBytes(4);
            if (BitConverter.IsLittleEndian) Array.Reverse(intBytes);
            int i = BitConverter.ToInt32(intBytes);
            return i;
        }

        public static long TakeLong(this IEnumerator<byte> e)
        {
            byte[] longBytes = e.TakeBytes(8);
            if (BitConverter.IsLittleEndian) Array.Reverse(longBytes);
            long l = BitConverter.ToInt64(longBytes);
            return l;
        }

        public static string TakeString(this IEnumerator<byte> e)
        {
            return Utility.ReadString(e);
        }

        public static double TakeDouble(this IEnumerator<byte> e)
        {
            byte[] dBytes = e.TakeBytes(8);
            if (BitConverter.IsLittleEndian) Array.Reverse(dBytes);
            double d = BitConverter.ToDouble(dBytes);
            return d;
        }

        public static float TakeSingle(this IEnumerator<byte> e)
        {
            byte[] fBytes = e.TakeBytes(4);
            if (BitConverter.IsLittleEndian) Array.Reverse(fBytes);
            float f = BitConverter.ToSingle(fBytes);
            return f;
        }

        public static Guid TakeGuid(this IEnumerator<byte> e)
        {
            byte[] bytes = e.TakeBytes(16);
            return new Guid(bytes, true);
        }
    }
}
