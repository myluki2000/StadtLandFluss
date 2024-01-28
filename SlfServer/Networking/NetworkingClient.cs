﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SlfServer.Networking.Packets;

namespace SlfServer.Networking
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

        private const byte MAGIC_BYTE_PACKET_FRAME = 0xBE;
        private const byte MAGIC_BYTE_NACK = 0xBF;

        private const int PORT = 1337;

        public NetworkingClient()
        {
            udpClient = new UdpClient(PORT);

            // may need explicit network adapter to work
            udpClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.137"));

            receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();
        }

        public void Send(SlfPacketBase packet)
        {
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

            udpClient.Send(bytes.ToArray());
        }

        private void SendNegativeAck(IPAddress target, int actualSequenceNumber, int expectedSequenceNumber)
        {
            List<byte> bytes = new();

            // magic byte identifying a NACK
            bytes.Add(MAGIC_BYTE_NACK);

            bytes.AddRange(actualSequenceNumber.ToBytes());
            bytes.AddRange(expectedSequenceNumber.ToBytes());

            // send NACK to the endpoint of which we are missing messages
            udpClient.Send(bytes.ToArray(), new IPEndPoint(target, PORT));
        }

        public (IPAddress sender, SlfPacketBase packet) Receive()
        {
            (IPAddress sender, PacketFrame frame) = deliveryQueue.Take();
            return (sender, frame.Payload);
        }

        public void Dispose()
        {
            udpClient.Dispose();
        }

        private void ReceiveMessages()
        {
            while (true)
            {
                IPEndPoint? remoteEndpoint = null;
                byte[] data = udpClient.Receive(ref remoteEndpoint);

                using IEnumerator<byte> dataEnumerator = data.Cast<byte>().GetEnumerator();

                byte magicByte = dataEnumerator.TakeByte();

                // check the magic byte to determine the type of frame we received, so we know how to process it further
                if (magicByte == MAGIC_BYTE_PACKET_FRAME)
                {
                    ReceiveRegularFrameMessage(remoteEndpoint, dataEnumerator);
                }
                else if (magicByte == MAGIC_BYTE_NACK)
                {
                    ReceiveNackFrame(remoteEndpoint, dataEnumerator);
                }
                // otherwise, if magic byte is missing, this udp data we received seems to not be related to our software
            }
        }

        private void ReceiveRegularFrameMessage(IPEndPoint remoteEndpoint, IEnumerator<byte> data)
        {
            PacketFrame frame = PacketFrame.FromBytes(data);

            if (frame.SequenceNumber == remoteSequenceNumbers[remoteEndpoint.Address] + 1)
            {
                // if sequence number is exactly the next number after the packet we have last delivered from this source, we
                // can deliver this packet too
                deliveryQueue.Add((remoteEndpoint.Address, frame));
                remoteSequenceNumbers[remoteEndpoint.Address]++;
            }
            else if (frame.SequenceNumber > remoteSequenceNumbers[remoteEndpoint.Address] + 1)
            {
                // if sequence number of this packet is not the next number following after the one we delivered last, we put the
                // packet in the holdback queue to wait for the rest of the packets to come in

                if(!holdbackList.Contains((remoteEndpoint.Address, frame)))
                    holdbackList.Add((remoteEndpoint.Address, frame));
            }
            // else drop the packet, we already have it

            foreach (PacketFrame.Acknowledgement acknowledgement in frame.PiggybackAcknowledgements)
            {
                if (acknowledgement.SequenceNumber > remoteSequenceNumbers[acknowledgement.RemoteEndpoint])
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
            // sequence number up to which the remote endpoint has received packets
            int remoteSequenceNumber = data.TakeInt();
            // sequence number of packet which also exists. The remote is missing packets with sequence numbers
            // between these two
            int actualSequenceNumber = data.TakeInt();

            for (int i = remoteSequenceNumber + 1; i <= actualSequenceNumber; i++)
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

                udpClient.Send(bytes.ToArray(), new IPEndPoint(remoteEndpoint.Address, PORT));
            }
        }
    }
}
