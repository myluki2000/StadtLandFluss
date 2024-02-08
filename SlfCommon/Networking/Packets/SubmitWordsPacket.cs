using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    /// <summary>
    /// Sent by a client to the match server to submit the words they have written in a round after that round
    /// has finished.
    /// </summary>
    public class SubmitWordsPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 9;

        /// <summary>
        /// ID of the match this packet is related to.
        /// </summary>
        public readonly Guid MatchId;

        /// <summary>
        /// Answer for the "city" category this player gave.
        /// </summary>
        public readonly string City;
        /// <summary>
        /// Answer for the "country" category this player gave.
        /// </summary>
        public readonly string Country;
        /// <summary>
        /// Answer for the "river" category this player gave.
        /// </summary>
        public readonly string River;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public SubmitWordsPacket() {}

        public SubmitWordsPacket(Guid senderId, Guid matchId, string city, string country, string river) : base(senderId)
        {
            MatchId = matchId;
            City = city;
            Country = country;
            River = river;
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
