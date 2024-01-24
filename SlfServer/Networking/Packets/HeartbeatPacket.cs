using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Networking.Packets
{
    internal class HeartbeatPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 250;

        public HeartbeatPacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
