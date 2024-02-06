using SlfCommon.Networking;
using SlfCommon.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SlfClient
{
    internal class MatchClient
    {
        private NetworkingClient networkingClient;

        public Guid Identity { get; }

        private Thread receiveThread;

        private Guid? matchServerId = null;
        private IPAddress? matchServerIp = null;

        public MatchClient(Guid identity)
        {
            Identity = identity;
            networkingClient = new();

            receiveThread = new Thread(ReceiveNetworkingMessages);
            receiveThread.Start();
        }

        public void JoinNewGame()
        {
            RequestMatchAssignmentPacket packet = new(Identity);

            // send match request packet to the multicast group of the servers
            networkingClient.SendOneOff(packet, IPAddress.Parse("239.0.0.1"));
        }

        private void ReceiveNetworkingMessages()
        {
            (IPAddress sender, SlfPacketBase packet) = networkingClient.Receive();

            if(packet is MatchAssignmentPacket matchAssignmentPacket)
            {
                Console.WriteLine("Client has been assigned a match on server " + matchAssignmentPacket.MatchServerIp);
                matchServerId = matchAssignmentPacket.MatchServerId;
                matchServerIp = IPAddress.Parse(matchAssignmentPacket.MatchServerIp);

                MatchJoinPacket matchJoinPacket = new(Identity);
                networkingClient.SendOneOff(matchJoinPacket, matchServerIp);
            }
        }
    }
}
