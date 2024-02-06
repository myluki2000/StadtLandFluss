using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SlfCommon.Networking.Packets;

namespace SlfCommon.Networking
{
    public class NetworkingClient : IDisposable
    {
        private readonly UdpClient udpClient;

        /// <summary>
        /// Sequence number of the last packet this client has sent
        /// </summary>
        private int sequenceNumber = 0;

        /// <summary>
        /// Dictionary keeping track of the sequence number of the last packet received from a particular
        /// client, which this client has delivered (i.e. has put into the delivery queue)
        /// </summary>
        private readonly Dictionary<IPAddress, int> remoteSequenceNumbers = new();

        private readonly Thread receiveThread;

        private readonly List<(IPAddress sender, PacketFrame frame)> holdbackList = new();
        private readonly BlockingCollection<(IPAddress sender, PacketFrame frame)> deliveryQueue = new(new ConcurrentQueue<(IPAddress sender, PacketFrame frame)>());

        private readonly Dictionary<int, SlfPacketBase> sentPackets = new();

        /// <summary>
        /// Magic byte identifying a one-off packet which is sent with basic multicast (non-ordered, non-reliable).
        /// </summary>
        private const byte MAGIC_BYTE_ONE_OFF_PACKET = 0xBD;
        /// <summary>
        /// Magic byte identifying a multicast packet sent via ordered reliable multicast.
        /// </summary>
        private const byte MAGIC_BYTE_PACKET_FRAME = 0xBE;
        /// <summary>
        /// Magic byte identifying a NACK used to invoke retransmitting a packet (needed for reliable multicast).
        /// </summary>
        private const byte MAGIC_BYTE_NACK = 0xBF;

        public int Port { get; }

        public IPAddress? MulticastAddress { get; private set; } = null;
        public bool InMulticastGroup => MulticastAddress != null;

        public NetworkingClient(IPAddress? multicastAddress = null, int port = 1337)
        {
            Port = port;
            udpClient = new UdpClient(Port);
            udpClient.MulticastLoopback = false;

            // join multicast group if multicast address specified in constructor
            if (multicastAddress != null)
            {
                // may need explicit network adapter to work
                udpClient.JoinMulticastGroup(multicastAddress);
                this.MulticastAddress = multicastAddress;
            }

            receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();
        }

        public void JoinMulticastGroup(IPAddress multicastAddress)
        {
            if (InMulticastGroup)
            {
                throw new Exception("NetworkingClient only supports joining one multicast group at the same time!");
            }

            udpClient.JoinMulticastGroup(multicastAddress);
            this.MulticastAddress = multicastAddress;
        }

        /// <summary>
        /// Sends the specified packet to the multicast group this NetworkingClient is in (if it is in one).
        /// This method also ensures eventual delivery of the packet at the remote endpoints, through
        /// ordered reliable multicast.
        /// 
        /// Throws an exception if called when the NetworkingClient isn't in a multicast group.
        /// </summary>
        public void SendOrderedReliableToGroup(SlfPacketBase packet, bool drop = false)
        {
            if (!InMulticastGroup)
                throw new Exception(
                    "Called SendOrderedReliableToGroup() even though NetworkingClient isn't in any multicast group.");

            sequenceNumber++;

            PacketFrame.Acknowledgement[] piggybackAcknowledgements =
                remoteSequenceNumbers.Select(x => new PacketFrame.Acknowledgement(x.Key, x.Value)).ToArray();

            PacketFrame frame = new(
                sequenceNumber,
                piggybackAcknowledgements,
                packet
            );

            List<byte> bytes = new();

            // magic byte identifying a regular frame
            bytes.Add(MAGIC_BYTE_PACKET_FRAME);
            bytes.AddRange(frame.ToBytes());

            sentPackets.Add(frame.SequenceNumber, frame.Payload);

            if(!drop)
                udpClient.Send(bytes.ToArray(), new IPEndPoint(MulticastAddress!, Port));
        }

        public void SendOneOffToGroup(SlfPacketBase packet)
        {
            if (!InMulticastGroup)
                throw new Exception(
                    "Called SendOneOffToGroup() even though NetworkingClient isn't in any multicast group.");

            SendOneOff(packet, MulticastAddress!);
        }

        /// <summary>
        /// Sends a one-off packet without any guarantees regarding ordering or reliability to the specified endpoint.
        /// </summary>
        public void SendOneOff(SlfPacketBase packet, IPAddress targetAddress)
        {
            List<byte> bytes = new();

            bytes.Add(MAGIC_BYTE_ONE_OFF_PACKET);
            bytes.AddRange(packet.ToBytes());

            udpClient.Send(bytes.ToArray(), new IPEndPoint(targetAddress, Port));
        }

        private void SendNegativeAck(IPAddress target, int actualSequenceNumber, int expectedSequenceNumber)
        {
            List<byte> bytes = new();

            // magic byte identifying a NACK
            bytes.Add(MAGIC_BYTE_NACK);

            bytes.AddRange(actualSequenceNumber.ToBytes());
            bytes.AddRange(expectedSequenceNumber.ToBytes());

            // send NACK to the endpoint of which we are missing messages
            udpClient.Send(bytes.ToArray(), new IPEndPoint(target, Port));
        }

        public (IPAddress sender, SlfPacketBase packet) Receive()
        {
            (IPAddress sender, PacketFrame frame) = deliveryQueue.Take();
            return (sender, frame.Payload);
        }

        public void Dispose()
        {
            udpClient.Close();
            udpClient.Dispose();
        }

        private void ReceiveMessages()
        {
            while (true)
            {
                IPEndPoint? remoteEndpoint = null;
                byte[] data = udpClient.Receive(ref remoteEndpoint);

                // 0-length array returned when connection is closed
                if (data.Length == 0)
                {
                    Console.WriteLine("Stopping network receive thread...");
                    return;
                }

                using IEnumerator<byte> dataEnumerator = data.Cast<byte>().GetEnumerator();

                byte magicByte = dataEnumerator.TakeByte();

                // check the magic byte to determine the type of frame we received, so we know how to process it further
                if (magicByte == MAGIC_BYTE_PACKET_FRAME)
                {
                    ReceiveRegularFrameMessage(remoteEndpoint, dataEnumerator);
                }
                else if (magicByte == MAGIC_BYTE_NACK)
                {
                    Console.WriteLine("Received data over UDP (NACK).");
                    ReceiveNackFrame(remoteEndpoint, dataEnumerator);
                } 
                else if (magicByte == MAGIC_BYTE_ONE_OFF_PACKET)
                {
                    ReceiveOneOffPacket(remoteEndpoint, dataEnumerator);
                }
                // otherwise, if magic byte is missing, this udp data we received seems to not be related to our software
            }
        }

        private void ReceiveOneOffPacket(IPEndPoint remoteEndpoint, IEnumerator<byte> data)
        {
            SlfPacketBase packet = SlfPacketBase.FromBytes(data);

            Console.WriteLine("Received data over UDP (unreliable one-off, sender=" + packet.SenderId
                + ", packet type=" + packet.GetType().Name + ").");

            // add packet to the delivery queue with a dummy frame
            deliveryQueue.Add((remoteEndpoint.Address, new PacketFrame(-1, Array.Empty<PacketFrame.Acknowledgement>(), packet)));
        }

        private void ReceiveRegularFrameMessage(IPEndPoint remoteEndpoint, IEnumerator<byte> data)
        {
            PacketFrame frame = PacketFrame.FromBytes(data);

            Console.WriteLine("Received data over UDP (ordered reliable frame, sender=" + frame.Payload.SenderId
                + ", seq no=" + frame.SequenceNumber
                + ", packet type=" + frame.Payload.GetType().Name + ").");

            if (!remoteSequenceNumbers.TryGetValue(remoteEndpoint.Address, out int storedSequenceNumber))
            {
                storedSequenceNumber = 0;
                remoteSequenceNumbers[remoteEndpoint.Address] = 0;
            }

            if (frame.SequenceNumber == storedSequenceNumber + 1)
            {
                // if sequence number is exactly the next number after the packet we have last delivered from this source, we
                // can deliver this packet too
                deliveryQueue.Add((remoteEndpoint.Address, frame));
                remoteSequenceNumbers[remoteEndpoint.Address]++;
            }
            else if (frame.SequenceNumber > storedSequenceNumber + 1)
            {
                // if sequence number of this packet is not the next number following after the one we delivered last, we put the
                // packet in the holdback queue to wait for the rest of the packets to come in

                if(!holdbackList.Contains((remoteEndpoint.Address, frame)))
                    holdbackList.Add((remoteEndpoint.Address, frame));

                SendNegativeAck(remoteEndpoint.Address, frame.SequenceNumber, storedSequenceNumber);
            }
            // else drop the packet, we already have it

            foreach (PacketFrame.Acknowledgement acknowledgement in frame.PiggybackAcknowledgements)
            {
                if (!remoteSequenceNumbers.ContainsKey(acknowledgement.RemoteEndpoint))
                {
                    // we haven't received any packet from this endpoint yet, so treat it as if it was seq number 0
                    SendNegativeAck(acknowledgement.RemoteEndpoint, acknowledgement.SequenceNumber, 0);
                }
                else if (acknowledgement.SequenceNumber > remoteSequenceNumbers[acknowledgement.RemoteEndpoint])
                {
                    // we have missed a packet from this endpoint
                    SendNegativeAck(acknowledgement.RemoteEndpoint, acknowledgement.SequenceNumber, remoteSequenceNumbers[acknowledgement.RemoteEndpoint]);
                }
            }

            // check if we can deliver more packets from the holdback queue
            bool anotherPacketDelivered = true;
            // loop until we don't find any more packets to deliver from the holdback queue
            while (anotherPacketDelivered)
            {
                anotherPacketDelivered = false;

                foreach ((IPAddress sender, PacketFrame frame) heldbackFrame in holdbackList)
                {
                    if (heldbackFrame.frame.SequenceNumber == remoteSequenceNumbers[heldbackFrame.sender] + 1)
                    {
                        deliveryQueue.Add(heldbackFrame);
                        holdbackList.Remove(heldbackFrame);
                        remoteSequenceNumbers[heldbackFrame.sender]++;
                        anotherPacketDelivered = true;
                        break;
                    } 
                    else if (heldbackFrame.frame.SequenceNumber < remoteSequenceNumbers[heldbackFrame.sender] + 1)
                    {
                        // if, for some reason, a message in the holdback list has a sequence number smaller than the sequence number of the last message we
                        // delivered, we can discard it
                        holdbackList.Remove(heldbackFrame);
                    }
                }
            }
        }

        private void ReceiveNackFrame(IPEndPoint remoteEndpoint, IEnumerator<byte> data)
        {
            // sequence number of packet which exists. The remote is missing packets with sequence numbers up to this
            int actualSequenceNumber = data.TakeInt();
            // the sequence number of the last packet the remote endpoint which has sent the NACK has received
            int expectedSequenceNumber = data.TakeInt();

            for (int i = expectedSequenceNumber + 1; i <= actualSequenceNumber; i++)
            {
                SlfPacketBase packet = sentPackets[i];

                PacketFrame frame = new(
                    i, 
                    Array.Empty<PacketFrame.Acknowledgement>(),
                    packet
                );

                List<byte> bytes = new();

                // magic byte identifying a regular frame
                bytes.Add(MAGIC_BYTE_PACKET_FRAME);
                bytes.AddRange(frame.ToBytes());

                udpClient.Send(bytes.ToArray(), new IPEndPoint(remoteEndpoint.Address, Port));
            }
        }
    }
}
