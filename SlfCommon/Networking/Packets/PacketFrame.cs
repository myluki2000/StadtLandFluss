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

        public readonly Acknowledgement[] PiggybackAcknowledgements;

        public readonly SlfPacketBase Payload;

        public PacketFrame(int sequenceNumber, Acknowledgement[] piggybackAcknowledgements, SlfPacketBase payload)
        {
            SequenceNumber = sequenceNumber;
            PiggybackAcknowledgements = piggybackAcknowledgements;
            Payload = payload;
        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new();

            bytes.AddRange(SequenceNumber.ToBytes());

            bytes.AddRange(PiggybackAcknowledgements.Length.ToBytes());

            foreach (Acknowledgement acknowledgement in PiggybackAcknowledgements)
            {
                bytes.AddRange(acknowledgement.RemoteEndpoint.GetAddressBytes());
                bytes.AddRange(acknowledgement.SequenceNumber.ToBytes());
            }

            bytes.AddRange(Payload.ToBytes());

            return bytes.ToArray();
        }

        public static PacketFrame FromBytes(IEnumerator<byte> bytes)
        {
            int sequenceNumber = bytes.TakeInt();

            int acknowledgementCount = bytes.TakeInt();
            Acknowledgement[] piggybackAcknowledgements = new Acknowledgement[acknowledgementCount];

            for (int i = 0; i < acknowledgementCount; i++)
            {
                piggybackAcknowledgements[i] = new Acknowledgement(
                    new IPAddress(bytes.TakeBytes(4)),
                    bytes.TakeInt()
                );
            }

            SlfPacketBase payload = SlfPacketBase.FromBytes(bytes);

            return new PacketFrame(
                sequenceNumber,
                piggybackAcknowledgements,
                payload
            );
        }

        public struct Acknowledgement
        {
            public IPAddress RemoteEndpoint;
            public int SequenceNumber;

            public Acknowledgement(IPAddress remoteEndpoint, int sequenceNumber)
            {
                RemoteEndpoint = remoteEndpoint;
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
