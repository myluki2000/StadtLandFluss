using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlfCommon.Networking.Packets;

namespace SlfServer.Networking.Packets
{
    public class HeartbeatResponsePacket : SlfPacketBase
    {
        public const byte PacketTypeId = 251;

        public bool HasGameRunning;

        public HeartbeatResponsePacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
