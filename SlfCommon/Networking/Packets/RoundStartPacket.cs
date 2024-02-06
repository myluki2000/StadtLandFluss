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
        /// Letter with which the words have to start this round.
        /// </summary>
        public readonly string Letter;

        public RoundStartPacket(Guid senderId, string letter) : base(senderId)
        {
            Letter = letter;
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
