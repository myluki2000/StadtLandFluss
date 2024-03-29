﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    public class MatchJoinResponsePacket : SlfPacketBase
    {
        public const byte PacketTypeId = 5;

        public readonly bool Accepted;

        public readonly string MatchMulticastIp;
        public readonly Guid MatchId;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public MatchJoinResponsePacket() {}

        public MatchJoinResponsePacket(Guid senderId, bool accepted, string? matchMulticastIp = null, Guid? matchId = null) : base(senderId)
        {
            MatchMulticastIp = "";
            MatchId = Guid.Empty;
            Accepted = accepted;

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
