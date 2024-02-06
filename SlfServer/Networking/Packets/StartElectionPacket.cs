using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlfCommon.Networking.Packets;

namespace SlfServer.Networking.Packets
{
    internal class StartElectionPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 100;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public StartElectionPacket()
        {
        }

        public StartElectionPacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
