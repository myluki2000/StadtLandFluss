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

        /// <summary>
        /// ID of the match this packet is related to.
        /// </summary>
        public readonly Guid MatchId;

        /// <summary>
        /// Letter with which the words had to start this round.
        /// </summary>
        public readonly string Letter;

        /// <summary>
        /// Array containing the PlayerIDs of the players who took part in this round.
        /// </summary>
        public readonly Guid[] Players;

        /// <summary>
        /// Array containing the answers for the "cities" category the players have submitted. In the same order as the "Players" array.
        /// </summary>
        public readonly string[] Cities;
        /// <summary>
        /// Array containing the answers for the "countries" category the players have submitted. In the same order as the "Players" array.
        /// </summary>
        public readonly string[] Countries;
        /// <summary>
        /// Array containing the answers for the "rivers" category the players have submitted. In the same order as the "Players" array.
        /// </summary>
        public readonly string[] Rivers;

        /// <summary>
        /// Boolean array indicating which of the answers were accepted by the server and which weren't.
        /// </summary>
        public readonly bool[] CitiesAccepted;
        /// <summary>
        /// Boolean array indicating which of the answers were accepted by the server and which weren't.
        /// </summary>
        public readonly bool[] CountriesAccepted;
        /// <summary>
        /// Boolean array indicating which of the answers were accepted by the server and which weren't.
        /// </summary>
        public readonly bool[] RiversAccepted;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public RoundResultPacket() { }

        public RoundResultPacket(Guid senderId, Guid matchId, string letter, Guid[] players, string[] cities, string[] countries, string[] rivers) : base(senderId)
        {
            MatchId = matchId;
            Letter = letter;
            Players = players;
            Cities = cities;
            Countries = countries;
            Rivers = rivers;
        }

        public RoundResultPacket(Guid senderId, Guid matchId, string letter, List<(Guid player, MatchRound.Answers answers)> playerAnswers) : base(senderId)
        {
            MatchId = matchId;
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
