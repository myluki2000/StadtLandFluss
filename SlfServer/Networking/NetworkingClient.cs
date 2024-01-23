using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Networking
{
    public class NetworkingClient
    {
        private UdpClient udpClient;

        public event EventHandler? OnPacketReceived;

        public NetworkingClient()
        {
            udpClient = new UdpClient(1337);

            // may need explicit network adapter to work
            udpClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.137"));
        }
    }
}
