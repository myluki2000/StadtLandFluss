using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    public abstract class SlfPacketBase
    {
        public Guid SenderId;

        public static Type[] RegisteredPacketTypes { get; private set; } =
            Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.BaseType == typeof(SlfPacketBase)
                        && x is { IsClass: true, IsAbstract: false, Namespace: "SlfCommon.Networking.Packets" })
                .ToArray();

        protected SlfPacketBase(Guid senderId)
        {
            SenderId = senderId;
        }

        /// <summary>
        /// Constructor used by reflection.
        /// </summary>
        protected SlfPacketBase()
        {
        }

        public abstract byte GetPacketTypeId();

        /// <summary>
        /// If your application which uses the SlfCommon class library uses custom packet classes derived from SlfPacketBase,
        /// this method should be called before the ToBytes()/FromBytes() methods are used. this is necessary because you need
        /// to tell the serializer which classes exist that derive from SlfPacketBase, so it can reconstruct them when you
        /// call FromBytes().
        /// This method can be called multiple times to register more packet types.
        /// </summary>
        public static void RegisterTypes(Type[] packetTypes)
        {
            SlfPacketBase.RegisteredPacketTypes = SlfPacketBase.RegisteredPacketTypes.Concat(packetTypes).ToArray();
        }

        public static SlfPacketBase FromBytes(IEnumerator<byte> bytes)
        {
            byte packetId = bytes.TakeByte();

            SlfPacketBase packetPrototype = RegisteredPacketTypes.Select(x => (SlfPacketBase)Activator.CreateInstance(x)!)
                .First(x => x.GetPacketTypeId() == packetId);
            
            foreach (FieldInfo field in packetPrototype.GetType().GetFields())
            {
                // filter out static and constant fields
                if (field.IsStatic || (field.IsLiteral && !field.IsInitOnly))
                    continue;

                object value = Utility.PrimitiveFromBytes(field.FieldType, bytes);
                field.SetValue(packetPrototype, value);
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
                // filter out static and constant fields
                if (field.IsStatic || (field.IsLiteral && !field.IsInitOnly))
                    continue;

                object? value = field.GetValue(this);

                if (value == null) throw new Exception();

                data.AddRange(Utility.PrimitiveToBytes(value));
            }

            return data.ToArray();
        }
    }
}
