using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        /// <summary>
        /// Deserializes an primitive-type object (or some supported struct-type objects) of the provided type by taking the necessary
        /// amount of bytes from the provided byte-enumerator. Assumes data is in big-endian format.
        /// </summary>
        /// <param name="type">Type of the object which should be deserialized.</param>
        /// <param name="bytes">Enumerator of bytes containing the data. Only the necessary amount of bytes to deserialize the object
        /// will be read from the enumerator.</param>
        /// <returns>Returns an object of the specified type.</returns>
        /// <exception cref="SerializationException">Thrown when the object has a type which is not supported by the deserializer.</exception>
        public static object PrimitiveFromBytes(Type type, IEnumerator<byte> bytes)
        {
            if (type == typeof(bool))
            {
                bool value = bytes.TakeBool();
            }
            else if (type == typeof(sbyte))
            {
                sbyte value = bytes.TakeSByte();
            }
            else if (type == typeof(byte))
            {
                byte value = bytes.TakeByte();
            }
            else if (type == typeof(short))
            {
                short value = bytes.TakeShort();
            }
            else if (type == typeof(ushort))
            {
                ushort value = bytes.TakeUShort();
            }
            else if (type == typeof(int))
            {
                int value = bytes.TakeInt();
            }
            else if (type == typeof(long))
            {
                long value = bytes.TakeLong();
            }
            else if (type == typeof(string))
            {
                string value = bytes.TakeString();
            }
            else if (type == typeof(double))
            {
                double value = bytes.TakeDouble();
            }
            else if (type == typeof(float))
            {
                float value = bytes.TakeSingle();
            }
            else if (type == typeof(Guid))
            {
                // GUIDs have a size of 16 bytes
                byte[] b = bytes.TakeBytes(16);
                // stored in big-endian format
                return new Guid(b, true);
            }
            else if (type.IsArray)
            {
                int count = bytes.TakeInt();

                // type of the elements in the array
                Type elementType = type.GetElementType()!;

                Array arr = Array.CreateInstance(elementType, count);

                for (int i = 0; i < count; i++)
                {
                    object ele = PrimitiveFromBytes(elementType, bytes);
                    arr.SetValue(ele, i);
                }

                return arr;
            }
            else if (type == typeof(Guid[]))
            {
                int count = bytes.TakeInt();

                Guid[] arr = new Guid[count];

                for (int i = 0; i < count; i++)
                {
                    // GUIDs have a size of 16 bytes
                    byte[] b = bytes.TakeBytes(16);
                    // stored in big-endian format
                    arr[i] = new Guid(b, true);
                }
            }
            else if (type == typeof(int[]))
            {
                int count = bytes.TakeInt();

                int[] arr = new int[count];

                for (int i = 0; i < count; i++)
                {
                    arr[i] = bytes.TakeInt();
                }
            }
            else if (type == typeof(string[]))
            {
                int count = bytes.TakeInt();

                string[] arr = new string[count];

                for (int i = 0; i < count; i++)
                {
                    arr[i] = bytes.TakeString();
                }
            }
            else
            {
                throw new SerializationException("Encountered field with unsupported type " + type.Name);
            }
        }

        /// <summary>
        /// Converts a primitive-type object (and some select struct-type objects) into a byte array (big-endian).
        /// </summary>
        /// <param name="o">Object to convert.</param>
        /// <returns>Byte array containing the data in big-endian byte ordering.</returns>
        /// <exception cref="SerializationException">Thrown when the object has a type which is not supported by the serializer.</exception>
        public static byte[] PrimitiveToBytes(object o)
        {
            List<byte> data = new();

            Type type = o.GetType();

            if (type == typeof(bool))
            {
                data.Add((bool)o ? (byte)0x1 : (byte)0x0);
            }
            else if (type == typeof(sbyte))
            {
                data.Add((byte)o);
            }
            else if (type == typeof(byte))
            {
                data.Add((byte)o);
            }
            else if (type == typeof(short))
            {
                byte[] bytes = BitConverter.GetBytes((short)o);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);

                data.AddRange(bytes);
            }
            else if (type == typeof(ushort))
            {
                byte[] bytes = BitConverter.GetBytes((ushort)o);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);

                data.AddRange(bytes);
            }
            else if (type == typeof(int))
            {
                data.AddRange(((int)o).ToBytes());
            }
            else if (type == typeof(long))
            {
                data.AddRange(((long)o).ToBytes());
            }
            else if (type == typeof(string))
            {
                data.AddRange(Utility.WriteString((string)o));
            }
            else if (type == typeof(double))
            {
                byte[] bytes = BitConverter.GetBytes((double)o);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                data.AddRange(bytes);
            }
            else if (type == typeof(float))
            {
                byte[] bytes = BitConverter.GetBytes((float)o);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                data.AddRange(bytes);
            }
            else if (type == typeof(Guid))
            {
                Guid guid = (Guid)o;
                // store in big-endian format
                byte[] bytes = guid.ToByteArray(true);
                data.AddRange(bytes);
            }
            else if (type.IsArray)
            {
                Array arr = (Array)o;
                data.AddRange(arr.Length.ToBytes());

                foreach (object ele in arr)
                {
                    data.AddRange(Utility.PrimitiveToBytes(ele));
                }
            }
            else
            {
                throw new SerializationException("Encountered field with unsupported type " + type.Name);
            }

            return data.ToArray();
        }
    }
}
