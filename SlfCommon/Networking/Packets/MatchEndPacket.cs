using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    /// <summary>
    /// Multicasted from match server to players partaking in a match to signify that the match has ended.
    /// </summary>
    public class MatchEndPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 188;

        public MatchEndPacket(Guid senderId) : base(senderId) { }

        /// <summary>
        /// Should only be used by reflection to create new object of this type.
        /// </summary>
        public MatchEndPacket()
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
