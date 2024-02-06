using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlfCommon.Networking.Packets;

namespace SlfServer.Networking.Packets
{
    /// <summary>
    /// Sent from a game server to the leader game server in response to a heartbeat packet emitted by the leader game server.
    /// </summary>
    public class HeartbeatResponsePacket : SlfPacketBase
    {
        public const byte PacketTypeId = 251;

        /// <summary>
        /// True if server currently has a match running, false otherwise.
        /// </summary>
        public bool HasMatchRunning;
        /// <summary>
        /// If a match is running, contains the GUID identifying this match. If no match is running, this value is undefined.
        /// </summary>
        public Guid MatchId;

        /// <summary>
        /// Maximum number of players this server can simultaneously let partake in the match running on it.
        /// </summary>
        public int MaxPlayerCount;
        /// <summary>
        /// Array containing the Player IDs of the players currently partaking in the match running on this server. If no match
        /// is currently running, this array is empty.
        /// </summary>
        public Guid[] CurrentPlayers;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public HeartbeatResponsePacket()
        {
        }

        public HeartbeatResponsePacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
