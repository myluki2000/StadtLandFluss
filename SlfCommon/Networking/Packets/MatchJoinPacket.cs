using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    /// <summary>
    /// Sent by a client to a server to join the match running on the server.
    /// </summary>
    public class MatchJoinPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 4;

        public MatchJoinPacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
