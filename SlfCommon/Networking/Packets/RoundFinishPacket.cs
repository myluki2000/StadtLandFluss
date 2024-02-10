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
        /// ID of the match this packet is related to.
        /// </summary>
        public Guid MatchId;
        /// <summary>
        /// Which round this packet references. Starts at 1.
        /// </summary>
        public int RoundNumber;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public RoundFinishPacket() {}

        public RoundFinishPacket(Guid senderId, Guid matchId, int roundNumber) : base(senderId)
        {
            MatchId = matchId;
            RoundNumber = roundNumber;
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
