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

        public readonly bool Successful;
        public readonly string ErrorMessage;

        public readonly Guid MatchServerId;
        public readonly string MatchServerIp;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public MatchAssignmentPacket()
        {
        }

        /// <summary>
        /// Create a new instance of this packet.
        /// </summary>
        /// <param name="senderId">ID of the sender of this packet.</param>
        /// <param name="successful">Boolean indicating whether a match could successfully be assigned for the player.</param>
        /// <param name="errorMessage">If match assignment was not successful, this string contains an error message. Otherwise, value is undefined.</param>
        /// <param name="matchServerId">If match assignment was successful, this contains the ID of the server the player has been assigned to. Otherwise, this value is undefined.</param>
        /// <param name="matchServerIp">If match assignment was successful, this contains the IP of the server the player has been assigned to. Otherwise, this value is undefined.</param>
        public MatchAssignmentPacket(Guid senderId, bool successful, string errorMessage, Guid matchServerId, string matchServerIp) : base(senderId)
        {
            MatchServerId = matchServerId;
            MatchServerIp = matchServerIp;
            Successful = successful;
            ErrorMessage = errorMessage;
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
