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

        /// <summary>
        /// ID of the match this packet is related to.
        /// </summary>
        public readonly Guid MatchId;

        /// <summary>
        /// String which contains a human-readable text about the match results (i.e. players' scores).
        /// </summary>
        public string MatchResultInformation;

        public MatchEndPacket(Guid senderId, Guid matchId, string matchResultInformation) : base(senderId)
        {
            MatchId = matchId;
            MatchResultInformation = matchResultInformation;
        }

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
