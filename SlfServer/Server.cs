using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SlfServer.Networking.Packets;
using Timer = System.Timers.Timer;

namespace SlfServer
{
    internal class Server
    {
        public Guid ServerId = Guid.NewGuid();

        private readonly UdpClient udpClient;

        private ServerState State = ServerState.STARTING;

        private (Guid guid, IPAddress ip)? leader = null;

        /// <summary>
        /// For the leader, this timer regularly sends out the heartbeat.
        /// For a non-leader, this timer is used to check if the heartbeat of the leader is received regularly.
        /// </summary>
        private Timer? heartbeatTimer = null;

        public Server(UdpClient udpClient)
        {
            this.udpClient = udpClient;
        }

        public async Task Start()
        {
            udpClient.BeginReceive(OnUdpClientReceive, null);

        }

        public async Task StartElection()
        {
            StartElectionPacket startElectionPacket = new(ServerId);

            State = ServerState.ELECTION_STARTED;

            // multicast send start election packets to other servers and wait for a response
            await udpClient.SendAsync(startElectionPacket.ToBytes());

            await Task.Delay(1000);

            if (leader == null)
            {
                await MakeMyselfLeader();
            }
        }

        private async Task MakeMyselfLeader()
        {
            leader = (ServerId, IPAddress.Parse("127.0.0.1"));

            LeaderAnnouncementPacket leaderAnnouncementPacket = new(ServerId);

            await udpClient.SendAsync(leaderAnnouncementPacket.ToBytes());

            State = ServerState.RUNNING;

            // start heartbeat
            heartbeatTimer?.Dispose();
            heartbeatTimer = new Timer(2000)
            {
                AutoReset = true
            };

            heartbeatTimer.Elapsed += async (sender, args) =>
            {
                await udpClient.SendAsync(new HeartbeatPacket(ServerId).ToBytes());
            };

            heartbeatTimer.Start();
        }

        private void OnUdpClientReceive(IAsyncResult res)
        {
            IPEndPoint? remoteIpEndPoint = new(IPAddress.Any, 0);
            byte[] receivedBytes = udpClient.EndReceive(res, ref remoteIpEndPoint);
            udpClient.BeginReceive(OnUdpClientReceive, null);

            SlfPacketBase receivedPacket = SlfPacketBase.FromBytes(receivedBytes.Cast<byte>().GetEnumerator());
           
            if (receivedPacket.GetPacketTypeId() == StartElectionPacket.PacketTypeId)
            {
                // ignore election packets when we have a lower id than the sender
                if (ServerId < receivedPacket.SenderId)
                    return;

                ElectionResponsePacket electionResponsePacket = new(ServerId);
                // send a response to the server the start election packet came from
                udpClient.Send(electionResponsePacket.ToBytes(), remoteIpEndPoint);

                StartElection().RunSynchronously();
                State = ServerState.ELECTION_STARTED;
            }
            else if (receivedPacket.GetPacketTypeId() == ElectionResponsePacket.PacketTypeId)
            {
                // set leader value (this isn't the final leader, but we are done here and have found a server with a higher id than us)
                leader = (receivedPacket.SenderId, remoteIpEndPoint.Address);
            }
            else if (receivedPacket.GetPacketTypeId() == LeaderAnnouncementPacket.PacketTypeId)
            {
                // start a new election if we have a higher id than the announced leader (something has gone wrong!)
                if (receivedPacket.SenderId < ServerId)
                {
                    StartElection().Start();
                    return;
                }

                leader = (receivedPacket.SenderId, remoteIpEndPoint.Address);

                heartbeatTimer?.Dispose();
                // start the timer which checks for regular heartbeats of the leader
                heartbeatTimer = new Timer(5000)
                {
                    AutoReset = true
                };
                
                heartbeatTimer.Elapsed += async (sender, args) => await StartElection();

                heartbeatTimer.Start();

            }
            else if (receivedPacket.GetPacketTypeId() == HeartbeatPacket.PacketTypeId)
            {
                if (!leader.HasValue || receivedPacket.SenderId != leader.Value.guid || heartbeatTimer == null)
                {
                    StartElection().Start();
                    return;
                }

                // received a heartbeat from the leader, so reset the timer
                heartbeatTimer.Stop();
                heartbeatTimer.Start();

                HeartbeatResponsePacket heartbeatResponse = new(ServerId)
                {
                    HasGameRunning = false, // TODO: actual game data needed here
                };

                // TODO: Change this to a singlecast
                udpClient.Send(heartbeatResponse.ToBytes());
            }
        }

        /// <summary>
        /// Enum containing the states of the Server state machine.
        /// </summary>
        private enum ServerState
        {
            STARTING,
            ELECTION_STARTED,
            RUNNING
        }
    }
}
