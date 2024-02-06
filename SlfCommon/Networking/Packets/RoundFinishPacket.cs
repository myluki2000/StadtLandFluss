using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    public class RoundFinishPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 8;

        public required string[] answersCity;
        public required string[] answersCountry;
        public required string[] answersRiver;

        public RoundFinishPacket(Guid senderId, string[] answersCity, string[] answersCountry, string[] answersRiver) : base(senderId)
        {
            this.answersCity = answersCity;
            this.answersCountry = answersCountry;
            this.answersRiver = answersRiver;
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
