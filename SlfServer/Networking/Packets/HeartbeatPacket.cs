using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlfCommon.Networking.Packets;

namespace SlfServer.Networking.Packets
{
    internal class HeartbeatPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 250;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public HeartbeatPacket()
        {
        }

        public HeartbeatPacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
