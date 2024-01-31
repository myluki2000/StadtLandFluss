using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    /// <summary>
    /// Sent by the server to all players in a match (multicast) to notify them of a new player joining.
    /// </summary>
    public class PlayerJoinAnnouncementPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 6;

        public PlayerJoinAnnouncementPacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
