using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Packets
{
    internal class LeaderAnnouncementPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 101;

        public LeaderAnnouncementPacket(Guid senderId) : base(senderId)
        {
        }

        public static SlfPacketBase FromBytesInternal(IEnumerator<byte> e)
        {
            return new LeaderAnnouncementPacket(e.TakeGuid());
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
