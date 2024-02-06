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

        public readonly string City;
        public readonly string Country;
        public readonly string River;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public SubmitWordsPacket() {}

        public SubmitWordsPacket(Guid senderId, string city, string country, string river) : base(senderId)
        {
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
