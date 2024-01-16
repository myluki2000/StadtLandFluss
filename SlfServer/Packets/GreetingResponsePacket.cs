using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Packets
{
    internal class GreetingResponsePacket : SlfPacketBase
    {
        public const byte PacketTypeId = 51;

        public GreetingResponsePacket(Guid senderId) : base(senderId)
        {
        }

        public static SlfPacketBase FromBytesInternal(IEnumerator<byte> e)
        {
            return new GreetingResponsePacket(e.TakeGuid());
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
