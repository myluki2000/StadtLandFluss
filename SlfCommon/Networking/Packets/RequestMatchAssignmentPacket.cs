using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    /// <summary>
    /// Sent by a client to the server multicast group to request the leader to give the
    /// client a match to join.
    /// </summary>
    public class RequestMatchAssignmentPacket : SlfPacketBase
    {
        public const byte PacketTypeId = 1;

        /// <summary>
        /// Empty constructor used by reflection.
        /// </summary>
        public RequestMatchAssignmentPacket() {}

        public RequestMatchAssignmentPacket(Guid senderId) : base(senderId)
        {
        }

        public override byte GetPacketTypeId()
        {
            return PacketTypeId;
        }
    }
}
