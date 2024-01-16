using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Packets
{
    internal class ElectionResponsePacket : SlfPacketBase
    {
        public const byte PacketTypeId = 102;

        public ElectionResponsePacket(Guid senderId) : base(senderId)
        {
        }

        public static SlfPacketBase FromBytesInternal(IEnumerator<byte> bytes)
        {
            return new ElectionResponsePacket(bytes.TakeGuid());
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
