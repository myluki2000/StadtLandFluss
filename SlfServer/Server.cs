using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SlfCommon;
using SlfCommon.Networking;
using SlfCommon.Networking.Packets;
using SlfServer.Game;
using SlfServer.Networking.Packets;
using Timer = System.Timers.Timer;

namespace SlfServer
{
    internal class Server
    {
        public Guid ServerId = Guid.NewGuid();

        private readonly NetworkingClient networkingClient;
        private readonly NetworkingClient matchNetworkingClient;

        private ServerState State = ServerState.STARTING;

        private Thread receiveThread;
        private Thread matchReceiveThread;

        private (Guid guid, IPAddress ip)? _leader = null;
        private (Guid guid, IPAddress ip)? Leader
        {
            get => _leader;
            set
            {
                // if leader is set, and new value is different than old one, clear the server statuses dictionary, so
                // that, if this server will become a leader again, there is no old, outdated status information in the dict
                if (_leader != value)
                {
                    serverStatuses.Clear();
                    serverTimers.Clear();

                }

                _leader = value;
                if (_leader != null)
                    Console.WriteLine("Leader set to " + _leader.Value.guid + " with IP " + _leader.Value.ip);
                else
                    Console.WriteLine("Leader unset.");
            }
        }

        /// <summary>
        /// For the leader, this timer regularly sends out the heartbeat.
        /// For a non-leader, this timer is used to check if the heartbeat of the leader is received regularly.
        /// </summary>
        private Timer? heartbeatTimer = null;

        public const byte MAX_PLAYER_COUNT = 2;
        private readonly HashSet<Guid> players = new();

        private MatchRound? currentRound = null;

        /// <summary>
        /// Match ID of the currently running match or NULL if no match is currently running.
        /// </summary>
        private Guid? matchId = null;

        private readonly WordValidator wordValidator;

        /// <summary>
        /// Boolean which indicates whether the server is currently accepting player answers. This is necessary because
        /// if a player answer to a round arrives after timeout then it should not be considered!
        /// </summary>
        private bool currentlyWaitingForPlayerAnswers = false;

        /// <summary>
        /// If this server is the leader server, this dictionary will store the latest status message the leader has
        /// received from each server.
        /// </summary>
        private readonly Dictionary<Guid, (IPAddress ipAddress, HeartbeatResponsePacket status)> serverStatuses = new();
        private readonly Dictionary<Guid, Timer?> serverTimers = new();

        public Server()
        {
            Console.WriteLine("Starting server...");

            Console.WriteLine("Loading valid words from stadt.txt, land.txt, and fluss.txt...");
            wordValidator = new();
            Console.WriteLine("Word loading complete.");

            networkingClient = new NetworkingClient(IPAddress.Parse("239.0.0.1"));

            receiveThread = new Thread(ReceiveNetworkingMessages);
            receiveThread.Start();

            matchReceiveThread = new Thread(ReceiveMatchNetworkingMessages);
            matchReceiveThread.Start();

            // generate a multicast IP for the match run by this server
            // initiate random with a seed derived from a GUID. It's a good seed source, trust me!
            IPAddress matchMulticastAddress;
            while (true)
            {
                // generate a random multicast IP
                Random rand = new(Guid.NewGuid().GetHashCode());
                byte secondByte = (byte)(rand.Next(254) + 1);
                byte thirdByte = (byte)(rand.Next(254) + 1);
                byte fourthByte = (byte)(rand.Next(254) + 1);
                matchMulticastAddress = new IPAddress(new byte[] { 239, secondByte, thirdByte, fourthByte });

                // don't accept 239.0.0.1 if it is generated, because that is the multicast IP of our server group
                if (!Equals(matchMulticastAddress, IPAddress.Parse("239.0.0.1")))
                    break;
            }

            matchNetworkingClient = new(matchMulticastAddress, 1338);

            Console.WriteLine("Server startup complete! Server running!");
        }

        public async void StartElection()
        {
            Leader = null;

            StartElectionPacket startElectionPacket = new(ServerId);

            State = ServerState.ELECTION_STARTED;

            // TODO: Change election messages to TCP? (except first one after startup whebn server doesn't know other servers)
            // multicast send start election packets to other servers and wait for a response
            networkingClient.SendOneOffToGroup(startElectionPacket);

            await Task.Delay(1000);

            if (Leader == null)
            {
                MakeMyselfLeader();
            }
        }

        private void MakeMyselfLeader()
        {
            Leader = (ServerId, IPAddress.Parse("127.0.0.1"));

            LeaderAnnouncementPacket leaderAnnouncementPacket = new(ServerId);

            networkingClient.SendOneOffToGroup(leaderAnnouncementPacket);

            State = ServerState.RUNNING;

            // start heartbeat
            heartbeatTimer?.Dispose();
            heartbeatTimer = new Timer(500)
            {
                AutoReset = true
            };

            heartbeatTimer.Elapsed += (sender, args) =>
            {
                networkingClient.SendOneOffToGroup(new HeartbeatPacket(ServerId));
            };

            heartbeatTimer.Start();
        }

        /// <summary>
        /// Receive loop for messages received by the networkingClient (which is part of the game server multicast group).
        /// </summary>
        private void ReceiveNetworkingMessages()
        {
            while (true)
            {
                (IPAddress sender, SlfPacketBase packet) = networkingClient.Receive();

                Console.WriteLine("Received a packet of type " + packet.GetType().Name);

                if (packet is StartElectionPacket)
                {
                    // ignore election packets when we have a lower id than the sender
                    if (ServerId < packet.SenderId)
                        continue;

                    ElectionResponsePacket electionResponsePacket = new(ServerId);
                    // send a response to the server the start election packet came from
                    networkingClient.SendOneOff(electionResponsePacket, sender);

                    Console.WriteLine("Starting leader election because we received a StartElectionPacket...");
                    StartElection();
                    State = ServerState.ELECTION_STARTED;
                }
                else if (packet is ElectionResponsePacket)
                {
                    Console.WriteLine("Received an election response packet, so I don't need to do anything further!");
                    // set leader value (this isn't the final leader, but we are done here and have found a server with a higher id than us)
                    Leader = (packet.SenderId, sender);
                }
                else if (packet is LeaderAnnouncementPacket)
                {
                    // start a new election if we have a higher id than the announced leader (something has gone wrong!)
                    if (packet.SenderId < ServerId)
                    {
                        Console.WriteLine("Starting leader election because the leader that just announced itself has a lower ID than me!");
                        StartElection();
                        continue;
                    }

                    Leader = (packet.SenderId, sender);

                    heartbeatTimer?.Dispose();
                    // start the timer which checks for regular heartbeats of the leader
                    heartbeatTimer = new Timer(2000)
                    {
                        AutoReset = true
                    };

                    heartbeatTimer.Elapsed += (sender, args) =>
                    {
                        Console.WriteLine("Heartbeat timeout elapsed without a heartbeat message from the leader!");
                        ((Timer)sender).Stop();
                        Console.WriteLine("Starting leader election because of missing leader heartbeat...");
                        StartElection();
                    };

                    heartbeatTimer.Start();

                }
                else if (packet is HeartbeatPacket)
                {
                    if (!Leader.HasValue || packet.SenderId != Leader.Value.guid || heartbeatTimer == null)
                    {
                        Console.WriteLine("Starting leader election because leader who sent a heartbeat message has a lower ID than me...");
                        StartElection();
                        return;
                    }

                    // received a heartbeat from the leader, so reset the timer
                    heartbeatTimer.Stop();
                    heartbeatTimer.Start();

                    HeartbeatResponsePacket heartbeatResponse = new(ServerId)
                    {
                        HasMatchRunning = matchId != null,
                        MatchId = matchId ?? Guid.Empty, // matchId if it is not null, otherwise empty guid
                        MaxPlayerCount = MAX_PLAYER_COUNT,
                        CurrentPlayers = players.ToArray(),
                    };

                    networkingClient.SendOneOff(heartbeatResponse, Leader.Value.ip);
                }
                else if (packet is HeartbeatResponsePacket heartbeatResponsePacket)
                {
                    // update this server's (the server the heartbeat response came from) status in the serverStatuses dictionary
                    // and reset the timer for this server, or create a new timer if it doesn't exist yet
                    // in case of a timeout, the server will be removed from the dictionary
                    serverStatuses[heartbeatResponsePacket.SenderId] = (sender, heartbeatResponsePacket);
                    if (!serverTimers.ContainsKey(heartbeatResponsePacket.SenderId))
                    {
                        serverTimers[heartbeatResponsePacket.SenderId]?.Dispose();
                        serverTimers[heartbeatResponsePacket.SenderId] = new Timer(2000)
                        {
                            AutoReset = true
                        };
                        serverTimers[heartbeatResponsePacket.SenderId].Elapsed += (sender, args) =>
                        {
                            Console.WriteLine("Server with ID " + heartbeatResponsePacket.SenderId + " has timed out!");
                            serverStatuses.Remove(heartbeatResponsePacket.SenderId);
                            serverTimers.Remove(heartbeatResponsePacket.SenderId);
                        };
                    }
                    else
                    {
                        serverTimers[heartbeatResponsePacket.SenderId].Stop();
                        serverTimers[heartbeatResponsePacket.SenderId].Start();
                    }
                }
                else if (packet is MatchJoinPacket matchJoinPacket)
                {
                    bool accept = players.Count < MAX_PLAYER_COUNT;

                    // if the server, for some reason, does not have a match multicast group, then reject the player
                    if (!matchNetworkingClient.InMulticastGroup)
                        accept = false;

                    // add player to our players list
                    players.Add(matchJoinPacket.SenderId);

                    // if there are at least 2 players waiting for a match, we can start a match
                    if(players.Count > 2)
                        StartMatch();

                    MatchJoinResponsePacket response = new(ServerId, accept, matchNetworkingClient.MulticastAddress?.ToString(), matchId);
                    networkingClient.SendOneOff(response, sender);
                }
                else if (packet is RequestMatchAssignmentPacket requestMatchAssignmentPacket)
                {
                    // only the leader should handle this packet, because the leader assigns players to game servers
                    if(Leader == null || Leader.Value.guid != ServerId)
                        continue;

                    Guid playerId = requestMatchAssignmentPacket.SenderId;

                    // firstly, check if the player is already part of a running match on a server, and redirect them to that server if
                    // that is the case (This can happen e.g. if the player crashes during the match and they restart their client
                    // and want to continue playing that same match)
                    (Guid id, IPAddress ip) matchServer = serverStatuses
                        .Where(x => x.Value.status.CurrentPlayers.Contains(playerId))
                        .Select(x => (x.Key, x.Value.ipAddress))
                        .FirstOrDefault();

                    if (matchServer == default((Guid id, IPAddress ip)))
                    {
                        // if we don't have a server where the player was already part of the match, find a server with a free slot
                        matchServer = serverStatuses
                            .Where(x => x.Value.status.CurrentPlayers.Length < x.Value.status.MaxPlayerCount)
                            .Select(x => (x.Key, x.Value.ipAddress))
                            .FirstOrDefault();
                    }

                    if (matchServer == default((Guid id, IPAddress ip)))
                    {
                        // TODO: Send a response to the client, denying their match assignment request, instead of just doing nothing
                        continue;
                    }

                    // send response to the client requesting a match assignment
                    MatchAssignmentPacket responsePacket = new(ServerId, matchServer.id, matchServer.ip.ToString());
                    networkingClient.SendOneOff(responsePacket, sender);
                }
            }
        }

        /// <summary>
        /// Receive loop for messages received by the matchNetworkingClient (used for communication between this game server
        /// and clients partaking in the match running on this server).
        /// </summary>
        private void ReceiveMatchNetworkingMessages()
        {
            while (true)
            {
                (IPAddress sender, SlfPacketBase packet) = networkingClient.Receive();

                if (packet is RoundFinishPacket roundFinishPacket)
                {
                    currentlyWaitingForPlayerAnswers = true;

                    new Thread(() =>
                    {
                        // give 2 seconds for the answers of the players to arrive, messages after timeout are not considered
                        Thread.Sleep(2000);

                        // we're not accepting any more answers!
                        currentlyWaitingForPlayerAnswers = false;

                        if (currentRound == null)
                            throw new Exception("CurrentRound is null even though we are receiving player answer packets!");

                        // send the results packet
                        RoundResultPacket responsePacket = new(
                            ServerId,
                            currentRound.Letter,
                            currentRound.PlayerAnswers.Select(x => (x.Key, x.Value)).ToList()
                        );

                        matchNetworkingClient.SendOrderedReliableToGroup(responsePacket);
                    }).Start();
                }
                else if (packet is SubmitWordsPacket submitWordsPacket)
                {
                    if (currentRound == null)
                        throw new Exception("CurrentRound is null even though we are receiving player answer packets!");

                    if (!currentlyWaitingForPlayerAnswers)
                        continue;

                    string city = submitWordsPacket.City;
                    string country = submitWordsPacket.Country;
                    string river = submitWordsPacket.River;

                    string letter = currentRound.Letter;

                    MatchRound.Answers playerAnswers = new(
                        new(city, wordValidator.ValidateCity(city, letter)),
                        new(country, wordValidator.ValidateCountry(country, letter)),
                        new(river, wordValidator.ValidateRiver(river, letter))
                    );

                    currentRound.PlayerAnswers[submitWordsPacket.SenderId] = playerAnswers;
                }
            }
        }

        /// <summary>
        /// Starts a new match. Throws an exception if a match is already running.
        /// </summary>
        private void StartMatch()
        {
            if (matchId != null)
                throw new Exception("Tried to start a new match even though a match is already running!");

            matchId = Guid.NewGuid();
        }

        private void StartRound()
        {

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
