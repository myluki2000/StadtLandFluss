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
        /// <summary>
        /// Event which is raised every time this client receives a heartbeat in its multicast group.
        /// </summary>
        public event EventHandler<IPEndPoint>? OnMulticastHeartbeatReceived;

        /// <summary>
        /// UdpClient we use to communicate.
        /// </summary>
        private readonly UdpClient udpClient;

        /// <summary>
        /// Sequence number of the last packet this client has sent
        /// </summary>
        private int sequenceNumber = 0;

        /// <summary>
        /// Dictionary keeping track of the sequence number of the last packet received from a particular
        /// client, which this client has delivered (i.e. has put into the delivery queue)
        /// </summary>
        private readonly Dictionary<(IPAddress ipAddress, Guid id), int> remoteSequenceNumbers = new();

        private readonly Thread receiveThread;

        private readonly List<(IPAddress senderIp, PacketFrame frame)> holdbackList = new();
        private readonly BlockingCollection<(IPAddress senderIp, PacketFrame frame)> deliveryQueue = new(new ConcurrentQueue<(IPAddress sender, PacketFrame frame)>());

        /// <summary>
        /// Stores packets received via ordered reliable multicast so they can be sent to another participant in case they are missing them.
        /// This could be optimized to some kind of dictionary to quickly look up received packets from a specific sender if necessary.
        /// </summary>
        private readonly List<PacketFrame> receivedPackets = new();

        /// <summary>
        /// Stores packets sent via ordered reliable multicast so they can be sent to another participant in case they are missing them. The key of the dictionary
        /// is the sequence number of the packet.
        /// </summary>
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
        /// <summary>
        /// Magic byte identifying a heartbeat packet.
        /// </summary>
        private const byte MAGIC_BYTE_HEARTBEAT = 0xC0;

        public int Port { get; }

        public IPAddress? MulticastAddress { get; private set; } = null;
        public bool InMulticastGroup => MulticastAddress != null;
        public Guid Identity { get; }

        public NetworkingClient(Guid identity, IPAddress? multicastAddress = null, int port = 1337)
        {
            Identity = identity;
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

        public void SendHeartbeatToGroup()
        {
            if (!InMulticastGroup)
                throw new Exception(
                    "Called SendOrderedReliableToGroup() even though NetworkingClient isn't in any multicast group.");

            PacketFrame.Acknowledgement[] piggybackAcknowledgements =
                remoteSequenceNumbers
                    .Select(x => new PacketFrame.Acknowledgement(x.Key.ipAddress, x.Key.id, x.Value))
                    .ToArray();

            // A heartbeat is basically a regular PacketFrame, BUT THE SEQUENCE NUMBER IS NOT INCREMENTED! It has no payload,
            // but the piggyback acknowledgements are transmitted as with a regular packet
            PacketFrame frame = new(false, null, sequenceNumber, Identity, piggybackAcknowledgements, null);

            List<byte> bytes = new();

            bytes.Add(MAGIC_BYTE_HEARTBEAT);
            bytes.AddRange(frame.ToBytes());

            udpClient.Send(bytes.ToArray(), new IPEndPoint(MulticastAddress!, Port));

            Console.WriteLine("Sent UDP Multicast Heartbeat to my group " + MulticastAddress);
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
                remoteSequenceNumbers.Select(x => new PacketFrame.Acknowledgement(x.Key.ipAddress, x.Key.id, x.Value)).ToArray();

            PacketFrame frame = new(
                false,
                null,
                sequenceNumber,
                Identity,
                piggybackAcknowledgements,
                packet
            );

            List<byte> bytes = new();

            // magic byte identifying a regular frame
            bytes.Add(MAGIC_BYTE_PACKET_FRAME);
            bytes.AddRange(frame.ToBytes());

            sentPackets.Add(frame.SequenceNumber, frame.Payload);

            if (!drop)
            {
                udpClient.Send(bytes.ToArray(), new IPEndPoint(MulticastAddress!, Port));
                Console.WriteLine("Sent ordered reliable packet of type " + packet.GetType().Name + " to " + MulticastAddress);
            }
            else
            {
                Console.WriteLine("Dropped ordered reliable packet of type " + packet.GetType().Name + " to " + MulticastAddress);
            }
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
        public void SendOneOff(SlfPacketBase packet, IPAddress targetAddress, int? port = null)
        {
            List<byte> bytes = new();

            bytes.Add(MAGIC_BYTE_ONE_OFF_PACKET);
            bytes.AddRange(packet.ToBytes());

            udpClient.Send(bytes.ToArray(), new IPEndPoint(targetAddress, port ?? Port));

            Console.WriteLine("Sent one-off packet of type " + packet.GetType().Name + " to " + targetAddress);
        }

        /// <summary>
        /// Multicasts a negative ack to the networking client's multicast group to retrieve missing packets for the
        /// specified target sender.
        /// </summary>
        /// <param name="targetIp">The sender's IP of which we are missing a packet.</param>
        /// <param name="targetId">The sender's ID of which we are missing a packet</param>
        /// <param name="actualSequenceNumber">The sequence number we know the sender has already passed.</param>
        /// <param name="expectedSequenceNumber">The sequence number we thought the sender was at.</param>
        private void SendNegativeAck(IPAddress targetIp, Guid targetId, int actualSequenceNumber, int expectedSequenceNumber)
        {
            List<byte> bytes = new();

            // magic byte identifying a NACK
            bytes.Add(MAGIC_BYTE_NACK);

            // ip address of the participant we are missing packets from
            bytes.AddRange(Utility.WriteString(targetIp.ToString()));
            // id of the participant we are missing packets from
            bytes.AddRange(targetId.ToByteArray(true));

            bytes.AddRange(actualSequenceNumber.ToBytes());
            bytes.AddRange(expectedSequenceNumber.ToBytes());

            Console.WriteLine("Missing packets. Sending NACK...");

            if(!InMulticastGroup)
                Console.WriteLine("Tried to send NACK even though I'm not in a multicast group! This should never happen!");

            // multicast NACK to our group
            udpClient.Send(bytes.ToArray(), new IPEndPoint(MulticastAddress!, Port));
        }

        /// <summary>
        /// When called by the object which uses the NetworkingClient, yields the next item in the delivery queue or blocks until an
        /// item is put into the delivery queue if the queue is empty at time of calling.
        /// </summary>
        /// <returns>Returns IP address of the sender of the packet and the packet.</returns>
        public (IPAddress sender, SlfPacketBase packet) Receive()
        {
            (IPAddress sender, PacketFrame frame) = deliveryQueue.Take();
            return (sender, frame.Payload);
        }

        private void ReceiveMessages()
        {
            while (true)
            {
                IPEndPoint? remoteEndpoint = null;

                byte[] data;
                try
                {
                    data = udpClient.Receive(ref remoteEndpoint);
                }
                catch (SocketException)
                {
                    Console.WriteLine("SocketException during data receive (this is normal if a NetworkClient is disposed of). Stopping receive loop.");
                    return;
                }

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
                else if (magicByte == MAGIC_BYTE_HEARTBEAT)
                {
                    ReceiveHeartbeat(remoteEndpoint, dataEnumerator);
                }
                else
                {
                    // otherwise, if magic byte is missing, this udp data we received seems to not be related to our software
                    Console.WriteLine("Received some unknown UDP data.");
                }
            }
        }

        private void ReceiveOneOffPacket(IPEndPoint remoteEndpoint, IEnumerator<byte> data)
        {
            SlfPacketBase packet = SlfPacketBase.FromBytes(data);

            Console.WriteLine("Received data over UDP (unreliable one-off, sender=" + packet.SenderId + "(" +  remoteEndpoint.Address
                + "), packet type=" + packet.GetType().Name + ").");

            // add packet to the delivery queue with a dummy frame
            deliveryQueue.Add((remoteEndpoint.Address, new PacketFrame(false, null, -1, Guid.Empty, Array.Empty<PacketFrame.Acknowledgement>(), packet)));
        }

        private void ReceiveHeartbeat(IPEndPoint remoteEndpoint, IEnumerator<byte> data)
        {
            PacketFrame frame = PacketFrame.FromBytes(data);
            Console.WriteLine("Received ordered reliable multicast heartbeat.");

            // if we don't have any sequence number for this remote endpoint yet, assume we have sequence number 0 stored
            if (!remoteSequenceNumbers.TryGetValue((remoteEndpoint.Address, frame.SenderId), out int storedSequenceNumber))
            {
                storedSequenceNumber = 0;
                remoteSequenceNumbers[(remoteEndpoint.Address, frame.SenderId)] = 0;
            }

            // check if sequence number is the sequence number we expect
            if (frame.SequenceNumber > storedSequenceNumber)
            {
                // we are missing some packets, send a NACK
                SendNegativeAck(remoteEndpoint.Address, frame.SenderId, frame.SequenceNumber, storedSequenceNumber);
            }

            foreach (PacketFrame.Acknowledgement acknowledgement in frame.PiggybackAcknowledgements)
            {
                
                if (acknowledgement.RemoteEndpointId == Identity)
                {
                    // if this acknowledgement is about ourselves, use our own sequence number as reference
                    if (acknowledgement.SequenceNumber > sequenceNumber)
                    {
                        SendNegativeAck(acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId, acknowledgement.SequenceNumber, sequenceNumber);
                    }
                }
                else
                {
                    // otherwise retrieve the sequence number we have stored for the specified remote host
                    if (!remoteSequenceNumbers.ContainsKey((acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId)))
                    {
                        // we haven't received any packet from this endpoint yet, so treat it as if it was seq number 0
                        SendNegativeAck(acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId, acknowledgement.SequenceNumber, 0);
                    }
                    else if (acknowledgement.SequenceNumber > remoteSequenceNumbers[(acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId)])
                    {
                        // we have missed a packet from this endpoint
                        SendNegativeAck(acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId,
                            acknowledgement.SequenceNumber, remoteSequenceNumbers[(acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId)]);
                    }
                }
            }

            OnMulticastHeartbeatReceived?.Invoke(this, remoteEndpoint);
        }

        private void ReceiveRegularFrameMessage(IPEndPoint remoteEndpoint, IEnumerator<byte> data)
        {
            PacketFrame frame = PacketFrame.FromBytes(data);

            bool myOwnPacket = frame.SenderId == Identity;

            IPAddress originalSenderAddress = remoteEndpoint.Address;

            // if the frame was retransmitted by a group participant who is not the original sender of the packet, then we 
            // have to retrieve the IP address of the sender from the frame data instead of getting it from our UDP client
            if (frame.IsRetransmitByOtherSender)
                originalSenderAddress = frame.OriginalSenderIp!;

            // in case we receive our own packet (which we lost after a crash) by the means of a NACK retransmit, we add it to our sent
            // packets again as if nothing happened
            if(frame.SenderId == Identity)
                sentPackets.TryAdd(frame.SequenceNumber, frame.Payload!);
            else if(!receivedPackets.Any(x => x.SenderId == frame.SenderId && x.SequenceNumber == frame.SequenceNumber))
                receivedPackets.Add(frame);

            Console.WriteLine("Received data over UDP (ordered reliable frame, sender=" + frame.SenderId
                + ", seq no=" + frame.SequenceNumber
                + ", packet type=" + (frame.Payload?.GetType().Name ?? "null") + ").");

            int storedSequenceNumber = 0;

            if (myOwnPacket)
            {
                storedSequenceNumber = sequenceNumber;
            }
            else
            {
                // if we don't have any sequence number for this remote endpoint yet, assume we have sequence number 0 stored
                if (!remoteSequenceNumbers.TryGetValue((originalSenderAddress, frame.SenderId), out storedSequenceNumber))
                {
                    storedSequenceNumber = 0;
                    remoteSequenceNumbers[(originalSenderAddress, frame.SenderId)] = 0;
                }
            }

            if (frame.SequenceNumber == storedSequenceNumber + 1)
            {
                // if sequence number is exactly the next number after the packet we have last delivered from this source, we
                // can deliver this packet too
                deliveryQueue.Add((originalSenderAddress, frame));

                if (myOwnPacket)
                    sequenceNumber++;
                else
                    remoteSequenceNumbers[(originalSenderAddress, frame.SenderId)]++;
            }
            else if (frame.SequenceNumber > storedSequenceNumber + 1)
            {
                // if sequence number of this packet is not the next number following after the one we delivered last, we put the
                // packet in the holdback queue to wait for the rest of the packets to come in

                if(!holdbackList.Contains((originalSenderAddress, frame)))
                    holdbackList.Add((originalSenderAddress, frame));

                SendNegativeAck(originalSenderAddress, frame.SenderId, frame.SequenceNumber, storedSequenceNumber);
            }
            // else drop the packet, we already have it

            // handle piggyback acknowledgements
            foreach (PacketFrame.Acknowledgement acknowledgement in frame.PiggybackAcknowledgements)
            {
                if (acknowledgement.RemoteEndpointId == Identity)
                {
                    // if this acknowledgement is about ourselves, use our own sequence number as reference
                    if (acknowledgement.SequenceNumber > sequenceNumber)
                    {
                        SendNegativeAck(acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId, acknowledgement.SequenceNumber, sequenceNumber);
                    }
                }
                else
                {
                    // otherwise use the sequence number of the remote endpoint we have stored
                    if (!remoteSequenceNumbers.ContainsKey((acknowledgement.RemoteEndpointIp,
                            acknowledgement.RemoteEndpointId)))
                    {
                        // we haven't received any packet from this endpoint yet, so treat it as if it was seq number 0
                        SendNegativeAck(acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId,
                            acknowledgement.SequenceNumber, 0);
                    }
                    else if (acknowledgement.SequenceNumber >
                             remoteSequenceNumbers
                                 [(acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId)])
                    {
                        // we have missed a packet from this endpoint
                        SendNegativeAck(acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId,
                            acknowledgement.SequenceNumber,
                            remoteSequenceNumbers[
                                (acknowledgement.RemoteEndpointIp, acknowledgement.RemoteEndpointId)]);
                    }
                }
            }

            // check if we can deliver more packets from the holdback queue
            bool anotherPacketDelivered = true;
            // loop until we don't find any more packets to deliver from the holdback queue
            while (anotherPacketDelivered)
            {
                anotherPacketDelivered = false;

                foreach ((IPAddress senderIp, PacketFrame frame) heldbackFrame in holdbackList)
                {
                    myOwnPacket = frame.SenderId == Identity;

                    int expectedSequenceNumber = (myOwnPacket
                        ? sequenceNumber
                        : remoteSequenceNumbers[(heldbackFrame.senderIp, heldbackFrame.frame.SenderId)]) + 1;

                    if (heldbackFrame.frame.SequenceNumber == expectedSequenceNumber)
                    {
                        deliveryQueue.Add(heldbackFrame);
                        holdbackList.Remove(heldbackFrame);

                        if (myOwnPacket)
                            sequenceNumber++;
                        else
                            remoteSequenceNumbers[(heldbackFrame.senderIp, heldbackFrame.frame.SenderId)]++;

                        anotherPacketDelivered = true;
                        break;
                    } 
                    else if (heldbackFrame.frame.SequenceNumber < expectedSequenceNumber)
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
            // ip address of the endpoint of which the sender of the NACK is missing packets
            IPAddress originalSenderIp = IPAddress.Parse(data.TakeString());

            // id of the endpoint of which the sender of the NACK is missing packets
            Guid originalSenderId = data.TakeGuid();

            bool weAreOriginalSender = originalSenderId == Identity;

            // sequence number of packet which exists. The remote is missing packets with sequence numbers up to this
            int actualSequenceNumber = data.TakeInt();
            // the sequence number of the last packet the remote endpoint which has sent the NACK has received
            int expectedSequenceNumber = data.TakeInt();

            for (int i = expectedSequenceNumber + 1; i <= actualSequenceNumber; i++)
            {
                SlfPacketBase? packet;

                if (weAreOriginalSender)
                {
                    // if we are the original sender of the packet, we can take it straight from our sentPackets dictionary (the key is the sequence number)
                    packet = sentPackets[i];
                }
                else
                {
                    // otherwise we have to search through the packets we have received from other peers
                    packet = receivedPackets.First(x => x.SenderId == originalSenderId && x.SequenceNumber == i).Payload;
                }

                PacketFrame frame = new(
                    !weAreOriginalSender,
                    weAreOriginalSender ? null : originalSenderIp,
                    i,
                    originalSenderId,
                    Array.Empty<PacketFrame.Acknowledgement>(), // don't send any piggyback acknowledgements for retransmitted packets
                    packet
                );

                List<byte> bytes = new();

                // magic byte identifying a regular frame
                bytes.Add(MAGIC_BYTE_PACKET_FRAME);
                bytes.AddRange(frame.ToBytes());

                udpClient.Send(bytes.ToArray(), new IPEndPoint(remoteEndpoint.Address, Port));
            }
        }

        /// <summary>
        /// Resets the network client's state for ordered reliable mutlicast. I.e. resets sequence numbers, deletes all sent and received messages etc.
        /// </summary>
        public void Reset()
        {
            holdbackList.Clear();
            while (deliveryQueue.Count > 0)
                deliveryQueue.Take();
            remoteSequenceNumbers.Clear();
            sequenceNumber = 0;
            sentPackets.Clear();
            receivedPackets.Clear();
        }

        public void Dispose()
        {
            if(InMulticastGroup)
                udpClient.DropMulticastGroup(MulticastAddress!);

            udpClient.Close();
            udpClient.Dispose();
        }
    }
}
