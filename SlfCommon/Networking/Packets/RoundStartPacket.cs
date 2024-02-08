using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    /// <summary>
    /// Multicast by the server to the players to notify them of a new round starting.
    /// </summary>
    public class RoundStartPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 7;

        /// <summary>
        /// ID of the match this packet is related to.
        /// </summary>
        public readonly Guid MatchId;

        /// <summary>
        /// Letter with which the words have to start this round.
        /// </summary>
        public readonly string Letter;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public RoundStartPacket() {}

        public RoundStartPacket(Guid senderId, Guid matchId, string letter) : base(senderId)
        {
            MatchId = matchId;
            Letter = letter;
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
