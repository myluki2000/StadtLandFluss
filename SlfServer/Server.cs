using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SlfServer.Networking.Packets;

namespace SlfServer
{
    internal class Server
    {
        public Guid ServerId = Guid.NewGuid();

        private readonly UdpClient udpClient;

        private ServerState State = ServerState.STARTING;

        private readonly List<(IPEndPoint ip, Guid id)> otherServers = new();

        /// <summary>
        /// Guid of the leader server or null if leader not determined or unknown
        /// </summary>
        private Guid? leaderServer = null;

        public Server(UdpClient udpClient)
        {
            this.udpClient = udpClient;
        }

        public async Task Start()
        {
            udpClient.BeginReceive(OnUdpClientReceive, null);

            await DiscoverOtherServers();
        }

        public async Task DiscoverOtherServers()
        {
            GreetingPacket greetingPacket = new(ServerId);

            State = ServerState.SERVER_DISCOVERY;
            await udpClient.SendAsync(greetingPacket.ToBytes());
        }

        public async Task StartElection()
        {
            StartElectionPacket startElectionPacket = new(ServerId);

            State = ServerState.ELECTION_STARTED;

            List<(IPEndPoint ip, Guid id)> serversWithHigherIds = otherServers.Where(x => x.id > ServerId).ToList();

            // if we don't know of any servers with an id higher than us, we can annouce ourselves as a leader immediately
            if (serversWithHigherIds.Count == 0)
            {
                await AnnounceMyselfAsLeader();
                return;
            }

            // otherwise send start election packets to the servers with higher ids and wait for a response
            foreach ((IPEndPoint ip, Guid id) server in serversWithHigherIds)
            {
                await udpClient.SendAsync(startElectionPacket.ToBytes(), server.ip);
            }

            // TODO: Wait for responses
        }

        private async Task AnnounceMyselfAsLeader()
        {
            leaderServer = ServerId;

            LeaderAnnouncementPacket leaderAnnouncementPacket = new(ServerId);

            await udpClient.SendAsync(leaderAnnouncementPacket.ToBytes());
        }

        private void OnUdpClientReceive(IAsyncResult res)
        {
            IPEndPoint? remoteIpEndPoint = new(IPAddress.Any, 0);
            byte[] receivedBytes = udpClient.EndReceive(res, ref remoteIpEndPoint);
            udpClient.BeginReceive(OnUdpClientReceive, null);

            SlfPacketBase receivedPacket = SlfPacketBase.FromBytes(receivedBytes.Cast<byte>().GetEnumerator());

            if (receivedPacket.GetPacketTypeId() == GreetingPacket.PacketTypeId)
            {
                if(otherServers.All(x => x.id != receivedPacket.SenderId))
                    otherServers.Add((remoteIpEndPoint, receivedPacket.SenderId));

                // multicast a greeting response; this means that other servers can also discover us if for some reason they haven't yet
                udpClient.Send(new GreetingResponsePacket(ServerId).ToBytes());
            } 
            else if (receivedPacket.GetPacketTypeId() == GreetingResponsePacket.PacketTypeId)
            {
                if (otherServers.All(x => x.id != receivedPacket.SenderId))
                    otherServers.Add((remoteIpEndPoint, receivedPacket.SenderId));
            }
            else if (receivedPacket.GetPacketTypeId() == StartElectionPacket.PacketTypeId)
            {
                ElectionResponsePacket electionResponsePacket = new(ServerId);
                // send a response to the server the start election packet came from
                udpClient.Send(electionResponsePacket.ToBytes(), remoteIpEndPoint);

                StartElection().RunSynchronously();
                State = ServerState.ELECTION_STARTED;
            }
        }

        /// <summary>
        /// Enum containing the states of the Server state machine.
        /// </summary>
        private enum ServerState
        {
            STARTING,
            SERVER_DISCOVERY,
            ELECTION_STARTED,
        }
    }
}
