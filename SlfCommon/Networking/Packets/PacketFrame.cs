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
        /// <summary>
        /// True if this frame was retransmitted and the sender of the retransmitted frame is not the original sender of the frame. False otherwise.
        /// </summary>
        public readonly bool IsRetransmitByOtherSender;
        /// <summary>
        /// If this frame was retransmitted by a participant who is NOT the original sender of the frame, this contains the IP address of the original
        /// sender. Otherwise this is NULL.
        /// </summary>
        public readonly IPAddress? OriginalSenderIp;

        /// <summary>
        /// Sequence number of this frame.
        /// </summary>
        public readonly int SequenceNumber;

        /// <summary>
        /// Id of the sender of the frame. If this is a retransmitted by a participant who is not the original sender of the frame, this
        /// is the ID of the original sender of the frame.
        /// </summary>
        public readonly Guid SenderId;

        /// <summary>
        /// Piggyback acknowledgements of the sender. Includes information about what the last delivered packet from each peer of the sender were.
        /// </summary>
        public readonly Acknowledgement[] PiggybackAcknowledgements;

        /// <summary>
        /// Actual payload of the frame - the packet containing some data.
        /// </summary>
        public readonly SlfPacketBase? Payload;

        public PacketFrame(bool isRetransmitByOtherSender, IPAddress? originalSenderIp, int sequenceNumber, Guid senderId, Acknowledgement[] piggybackAcknowledgements, SlfPacketBase? payload)
        {
            IsRetransmitByOtherSender = isRetransmitByOtherSender;

            if (IsRetransmitByOtherSender && originalSenderIp == null)
                throw new Exception("When IsRetransmitByOtherSender is TRUE, an OriginalSenderIp needs to be set!");

            if (!IsRetransmitByOtherSender && originalSenderIp != null)
                throw new Exception("When IsRetransmitByOtherSender is FALSE, OriginalSenderIp needs to be NULL!");

            OriginalSenderIp = originalSenderIp;

            SequenceNumber = sequenceNumber;
            SenderId = senderId;
            PiggybackAcknowledgements = piggybackAcknowledgements;
            Payload = payload;
        }

        /// <summary>
        /// Serializes the data retained in the frame into a byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            List<byte> bytes = new();

            bytes.Add(IsRetransmitByOtherSender ? (byte)1 : (byte)0);

            if(IsRetransmitByOtherSender)
                bytes.AddRange(Utility.WriteString(OriginalSenderIp!.ToString()));

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

        /// <summary>
        /// Deserializes a byte representation of a frame into a frame object including the payload data (the packet).
        /// </summary>
        /// <param name="bytes">Enumerator of the bytes</param>
        /// <returns>A frame object containing the data read from the byte input.</returns>
        public static PacketFrame FromBytes(IEnumerator<byte> bytes)
        {
            bool isRetransmitByOtherSender = bytes.TakeBool();

            IPAddress? originalSenderIp = null;
            if(isRetransmitByOtherSender)
                originalSenderIp = IPAddress.Parse(bytes.TakeString());

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
                isRetransmitByOtherSender,
                originalSenderIp,
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
