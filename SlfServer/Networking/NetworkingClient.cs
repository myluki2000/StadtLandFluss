using System;
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

        public event EventHandler? OnPacketReceived;

        private int sequenceNumber = 0;

        private readonly Dictionary<Guid, int> remoteSequenceNumbers = new();

        public NetworkingClient()
        {
            udpClient = new UdpClient(1337);
            
            // may need explicit network adapter to work
            udpClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.137"));
        }

        public void Send(SlfPacketBase packet)
        {
            sequenceNumber++;

            byte[] bytes = ;

            packet.ToBytes()

            udpClient.Send(bytes);
        }

        public void Dispose()
        {
            udpClient.Dispose();
        }
    }
}
