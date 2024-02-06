using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    /// <summary>
    /// Sent by the game server to all clients to announce results of a played round.
    /// </summary>
    public class RoundResultPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 10;

        public readonly string Letter;

        public readonly Guid[] Players;

        public readonly string[] Cities;
        public readonly string[] Countries;
        public readonly string[] Rivers;

        public readonly bool[] CitiesAccepted;
        public readonly bool[] CountriesAccepted;
        public readonly bool[] RiversAccepted;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public RoundResultPacket() { }

        public RoundResultPacket(Guid senderId, string letter, Guid[] players, string[] cities, string[] countries, string[] rivers) : base(senderId)
        {
            Letter = letter;
            Players = players;
            Cities = cities;
            Countries = countries;
            Rivers = rivers;
        }

        public RoundResultPacket(Guid senderId, string letter, List<(Guid player, MatchRound.Answers answers)> playerAnswers) : base(senderId)
        {
            Letter = letter;

            Players = playerAnswers.Select(x => x.player).ToArray();
            Cities = playerAnswers.Select(x => x.answers.City.Text).ToArray();
            CitiesAccepted = playerAnswers.Select(x => x.answers.City.Accepted).ToArray();
            Countries = playerAnswers.Select(x => x.answers.Country.Text).ToArray();
            CountriesAccepted = playerAnswers.Select(x => x.answers.Country.Accepted).ToArray();
            Rivers = playerAnswers.Select(x => x.answers.River.Text).ToArray();
            RiversAccepted = playerAnswers.Select(x => x.answers.River.Accepted).ToArray();
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
