using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    public class MatchJoinResponsePacket : SlfPacketBase
    {
        public const byte PacketTypeId = 5;

        public required bool Accepted;

        public required string MatchMulticastIp;
        public required Guid MatchId;

        public MatchJoinResponsePacket(Guid senderId, bool accepted, string? matchMulticastIp = null, Guid? matchId = null) : base(senderId)
        {
            MatchMulticastIp = "";
            MatchId = Guid.Empty;

            if (accepted)
            {
                if (matchMulticastIp == null || matchId == null)
                {
                    throw new Exception(
                        "When match join request is accepted, the match multicast IP and the match-id need to be set to a non-null value!");
                }
                else
                {
                    MatchMulticastIp = matchMulticastIp;
                    MatchId = matchId.Value;
                }
            } 
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
