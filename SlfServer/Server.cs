using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SlfCommon.Networking;
using SlfCommon.Networking.Packets;
using SlfServer.Networking.Packets;
using Timer = System.Timers.Timer;

namespace SlfServer
{
    internal class Server
    {
        public Guid ServerId = Guid.NewGuid();

        private readonly NetworkingClient networkingClient;

        private ServerState State = ServerState.STARTING;

        private (Guid guid, IPAddress ip)? leader = null;

        /// <summary>
        /// For the leader, this timer regularly sends out the heartbeat.
        /// For a non-leader, this timer is used to check if the heartbeat of the leader is received regularly.
        /// </summary>
        private Timer? heartbeatTimer = null;

        public const byte MAX_PLAYER_COUNT = 2;
        private readonly HashSet<Guid> players = new();

        public Server()
        {
            networkingClient = new NetworkingClient(IPAddress.Parse("239.0.0.1"));
        }

        public async Task StartElection()
        {
            StartElectionPacket startElectionPacket = new(ServerId);

            State = ServerState.ELECTION_STARTED;

            // multicast send start election packets to other servers and wait for a response
            networkingClient.SendToMyGroup(startElectionPacket);

            await Task.Delay(1000);

            if (leader == null)
            {
                MakeMyselfLeader();
            }
        }

        private void MakeMyselfLeader()
        {
            leader = (ServerId, IPAddress.Parse("127.0.0.1"));

            LeaderAnnouncementPacket leaderAnnouncementPacket = new(ServerId);

            networkingClient.SendToMyGroup(leaderAnnouncementPacket);

            State = ServerState.RUNNING;

            // start heartbeat
            heartbeatTimer?.Dispose();
            heartbeatTimer = new Timer(2000)
            {
                AutoReset = true
            };

            heartbeatTimer.Elapsed += (sender, args) =>
            {
                networkingClient.SendToMyGroup(new HeartbeatPacket(ServerId));
            };

            heartbeatTimer.Start();
        }

        private void ReceiveNetworkingMessages()
        {
            (IPAddress sender, SlfPacketBase packet) = networkingClient.Receive();

            if (packet is StartElectionPacket)
            {
                // ignore election packets when we have a lower id than the sender
                if (ServerId < packet.SenderId)
                    return;

                ElectionResponsePacket electionResponsePacket = new(ServerId);
                // send a response to the server the start election packet came from
                networkingClient.SendOneOff(electionResponsePacket, sender);

                StartElection().RunSynchronously();
                State = ServerState.ELECTION_STARTED;
            }
            else if (packet is ElectionResponsePacket)
            {
                // set leader value (this isn't the final leader, but we are done here and have found a server with a higher id than us)
                leader = (packet.SenderId, sender);
            }
            else if (packet is LeaderAnnouncementPacket)
            {
                // start a new election if we have a higher id than the announced leader (something has gone wrong!)
                if (packet.SenderId < ServerId)
                {
                    StartElection().Start();
                    return;
                }

                leader = (packet.SenderId, sender);

                heartbeatTimer?.Dispose();
                // start the timer which checks for regular heartbeats of the leader
                heartbeatTimer = new Timer(5000)
                {
                    AutoReset = true
                };
                
                heartbeatTimer.Elapsed += async (sender, args) => await StartElection();

                heartbeatTimer.Start();

            }
            else if (packet is HeartbeatPacket)
            {
                if (!leader.HasValue || packet.SenderId != leader.Value.guid || heartbeatTimer == null)
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

                networkingClient.SendOneOff(heartbeatResponse, leader.Value.ip);
            }
            else if (packet is MatchJoinPacket matchJoinPacket)
            {
                MatchJoinResponsePacket response = new(ServerId);
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
