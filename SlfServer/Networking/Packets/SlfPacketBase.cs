using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Networking.Packets
{
    internal abstract class SlfPacketBase
    {
        public Guid SenderId;

        private static readonly Type[] packetTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false, Namespace: "SlfServer.Packets" })
            .ToArray();

        protected SlfPacketBase(Guid senderId)
        {
            SenderId = senderId;
        }

        public abstract byte GetPacketTypeId();

        public static SlfPacketBase FromBytes(IEnumerator<byte> bytes)
        {
            byte packetId = bytes.TakeByte();

            SlfPacketBase packetPrototype = packetTypes.Select(x => (SlfPacketBase)Activator.CreateInstance(x)!)
                .First(x => x.GetPacketTypeId() == packetId);

            return (SlfPacketBase)packetPrototype.GetType().GetMethod("FromBytesInternal").Invoke(null, new[] { bytes });
        }

        public byte[] ToBytes()
        {
            List<byte> data = new();

            // add datagram type id
            data.Add(GetPacketTypeId());

            // add datagram sender id
            data.AddRange(SenderId.ToByteArray(true));

            // use reflection to iterate over fields of types inheriting from SlfPacketBase, and serialize the fields to the byte array
            foreach (FieldInfo field in GetType().GetFields())
            {
                object? value = field.GetValue(this);

                if (value == null) throw new Exception();

                if (field.FieldType == typeof(bool))
                {
                    data.Add((bool)value ? (byte)0x1 : (byte)0x0);
                }
                else if (field.FieldType == typeof(sbyte))
                {
                    data.Add((byte)value);
                }
                else if (field.FieldType == typeof(byte))
                {
                    data.Add((byte)value);
                }
                else if (field.FieldType == typeof(short))
                {
                    byte[] bytes = BitConverter.GetBytes((short)value);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    data.AddRange(bytes);
                }
                else if (field.FieldType == typeof(ushort))
                {
                    byte[] bytes = BitConverter.GetBytes((ushort)value);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    data.AddRange(bytes);
                }
                else if (field.FieldType == typeof(int))
                {

                    byte[] bytes = BitConverter.GetBytes((int)value);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    data.AddRange(bytes);
                }
                else if (field.FieldType == typeof(long))
                {
                    byte[] bytes = BitConverter.GetBytes((long)value);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    data.AddRange(bytes);
                }
                else if (field.FieldType == typeof(string))
                {
                    data.AddRange(Utility.WriteString((string)value));
                }
                else if (field.FieldType == typeof(double))
                {
                    byte[] bytes = BitConverter.GetBytes((double)value);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    data.AddRange(bytes);
                }
                else if (field.FieldType == typeof(float))
                {
                    byte[] bytes = BitConverter.GetBytes((float)value);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    data.AddRange(bytes);
                }
                else
                {
                    throw new Exception("Encountered field with unsupported type " + field.FieldType.Name);
                }
            }

            return data.ToArray();
        }
    }
}
