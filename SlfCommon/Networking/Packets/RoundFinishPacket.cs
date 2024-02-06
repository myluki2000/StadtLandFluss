using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    /// <summary>
    /// Sent to the multicast group by the client who finishes first to notify the server and other clients that
    /// the round has ended.
    /// </summary>
    public class RoundFinishPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 8;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public RoundFinishPacket() {}

        public RoundFinishPacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
