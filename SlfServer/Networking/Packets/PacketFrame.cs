using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Networking.Packets
{
    public class PacketFrame
    {
        public readonly Guid SenderId;
        public readonly long SequenceNumber;

        public readonly SlfPacketBase Payload;

        public PacketFrame(Guid senderId, long sequenceNumber, SlfPacketBase payload)
        {
            SenderId = senderId;
            SequenceNumber = sequenceNumber;
            Payload = payload;
        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(SenderId.ToByteArray(true));
            bytes.AddRange(SequenceNumber.ToBytes());

            bytes.AddRange(Payload.ToBytes());

            return bytes.ToArray();
        }

        public static PacketFrame FromBytes(IEnumerator<byte> bytes)
        {
            return new PacketFrame(
                bytes.TakeGuid(),
                bytes.TakeLong(),
                SlfPacketBase.FromBytes(bytes)
            );
        }
    }
}
