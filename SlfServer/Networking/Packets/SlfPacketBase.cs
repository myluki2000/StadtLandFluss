using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Networking.Packets
{
    public abstract class SlfPacketBase
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

            foreach (FieldInfo field in packetPrototype.GetType().GetFields())
            {
                if (field.FieldType == typeof(bool))
                {
                    bool value = bytes.TakeBool();
                    field.SetValue(packetPrototype, value);
                } 
                else if (field.FieldType == typeof(sbyte))
                {
                    sbyte value = bytes.TakeSByte();
                    field.SetValue(packetPrototype, value);
                }
                else if (field.FieldType == typeof(byte))
                {
                    byte value = bytes.TakeByte();
                    field.SetValue(packetPrototype, value);
                }
                else if (field.FieldType == typeof(short))
                {
                    short value = bytes.TakeShort();
                    field.SetValue(packetPrototype, value);
                }
                else if (field.FieldType == typeof(ushort))
                {
                    ushort value = bytes.TakeUShort();
                    field.SetValue(packetPrototype, value);
                }
                else if (field.FieldType == typeof(int))
                {
                    int value = bytes.TakeInt();
                    field.SetValue(packetPrototype, value);
                } 
                else if (field.FieldType == typeof(long))
                {
                    long value = bytes.TakeLong();
                    field.SetValue(packetPrototype, value);
                }
                else if (field.FieldType == typeof(string))
                {
                    string value = bytes.TakeString();
                    field.SetValue(packetPrototype, value);
                }
                else if (field.FieldType == typeof(double))
                {
                    double value = bytes.TakeDouble();
                    field.SetValue(packetPrototype, value);
                }
                else if (field.FieldType == typeof(float))
                {
                    float value = bytes.TakeSingle();
                    field.SetValue(packetPrototype, value);
                }
                else
                {
                    throw new Exception("Encountered field with unsupported type " + field.FieldType.Name);
                }
            }

            return packetPrototype;
        }

        public byte[] ToBytes()
        {
            List<byte> data = new();

            // add datagram type id
            data.Add(GetPacketTypeId());

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
