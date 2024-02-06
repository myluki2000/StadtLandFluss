using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    public class MigrateMatchRequestPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 2;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public MigrateMatchRequestPacket() {}

        public MigrateMatchRequestPacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
