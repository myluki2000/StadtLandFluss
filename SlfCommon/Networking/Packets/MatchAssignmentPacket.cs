using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    /// <summary>
    /// Sent by the leader server to a client to assign them to a match.
    /// </summary>
    public class MatchAssignmentPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 3;

        public readonly Guid MatchServerId;
        public readonly string MatchServerIp;

        public MatchAssignmentPacket(Guid senderId, Guid matchServerId, string matchServerIp) : base(senderId)
        {
            MatchServerId = matchServerId;
            MatchServerIp = matchServerIp;
        }

        public override byte GetPacketTypeId()
        {
            throw new NotImplementedException();
        }
    }
}
