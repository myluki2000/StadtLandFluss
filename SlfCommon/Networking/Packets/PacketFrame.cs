using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon.Networking.Packets
{
    public class PacketFrame
    {
        public readonly int SequenceNumber;
        public readonly Guid SenderId;

        public readonly Acknowledgement[] PiggybackAcknowledgements;

        public readonly SlfPacketBase? Payload;

        public PacketFrame(int sequenceNumber, Guid senderId, Acknowledgement[] piggybackAcknowledgements, SlfPacketBase? payload)
        {
            SequenceNumber = sequenceNumber;
            SenderId = senderId;
            PiggybackAcknowledgements = piggybackAcknowledgements;
            Payload = payload;
        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(SequenceNumber.ToBytes());
            bytes.AddRange(SenderId.ToByteArray(true));

            bytes.AddRange(PiggybackAcknowledgements.Length.ToBytes());

            foreach (Acknowledgement acknowledgement in PiggybackAcknowledgements)
            {
                bytes.AddRange(acknowledgement.RemoteEndpointIp.GetAddressBytes());
                bytes.AddRange(acknowledgement.RemoteEndpointId.ToByteArray(true));
                bytes.AddRange(acknowledgement.SequenceNumber.ToBytes());
            }

            // Boolean indicating whether frame has a payload
            bytes.Add((Payload != null) ? (byte)1 : (byte)0);

            if(Payload != null)
                bytes.AddRange(Payload.ToBytes());

            return bytes.ToArray();
        }

        public static PacketFrame FromBytes(IEnumerator<byte> bytes)
        {
            int sequenceNumber = bytes.TakeInt();
            Guid senderId = bytes.TakeGuid();

            int acknowledgementCount = bytes.TakeInt();
            Acknowledgement[] piggybackAcknowledgements = new Acknowledgement[acknowledgementCount];

            for (int i = 0; i < acknowledgementCount; i++)
            {
                piggybackAcknowledgements[i] = new Acknowledgement(
                    new IPAddress(bytes.TakeBytes(4)),
                    bytes.TakeGuid(),
                    bytes.TakeInt()
                );
            }

            bool hasPayload = bytes.TakeBool();

            SlfPacketBase? payload = hasPayload ? SlfPacketBase.FromBytes(bytes) : null;

            return new PacketFrame(
                sequenceNumber,
                senderId,
                piggybackAcknowledgements,
                payload
            );
        }

        public struct Acknowledgement
        {
            public IPAddress RemoteEndpointIp;
            public Guid RemoteEndpointId;
            public int SequenceNumber;

            public Acknowledgement(IPAddress remoteEndpointIp, Guid remoteEndpointId, int sequenceNumber)
            {
                RemoteEndpointIp = remoteEndpointIp;
                RemoteEndpointId = remoteEndpointId;
                SequenceNumber = sequenceNumber;
            }
        }

        protected bool Equals(PacketFrame other)
        {
            return SequenceNumber == other.SequenceNumber;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PacketFrame)obj);
        }

        public override int GetHashCode()
        {
            return SequenceNumber.GetHashCode();
        }

        public static bool operator ==(PacketFrame? left, PacketFrame? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PacketFrame? left, PacketFrame? right)
        {
            return !Equals(left, right);
        }
    }
}
