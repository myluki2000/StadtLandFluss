using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    public class RoundResultPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 10;

        public readonly string Letter;

        public readonly Guid[] Players;
        public readonly string[] Cities;
        public readonly string[] Countries;
        public readonly string[] Rivers;

        public RoundResultPacket(Guid senderId, string letter, Guid[] players, string[] cities, string[] countries, string[] rivers) : base(senderId)
        {
            Letter = letter;
            Players = players;
            Cities = cities;
            Countries = countries;
            Rivers = rivers;
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
