using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.PortableExecutable;
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

        /// <summary>
        /// Ordered reliable multicast networking client used for communication between this server and other servers.
        /// </summary>
        private readonly NetworkingClient networkingClient;
        /// <summary>
        /// Ordered reliable multicast networking client used for communication between this servers and clients partaking in the match.
        /// </summary>
        private readonly NetworkingClient matchNetworkingClient;

        /// <summary>
        /// State of the server.
        /// </summary>
        private ServerState State = ServerState.STARTING;

        /// <summary>
        /// Thread used to handle received messages of the networkingClient.
        /// </summary>
        private Thread receiveThread;
        /// <summary>
        /// Thread used to handle received message of the matchNetworkingClient.
        /// </summary>
        private Thread matchReceiveThread;
        /// <summary>
        /// Thread used by the TCP listener to accept new connections.
        /// </summary>
        private Thread tcpListenerThread;

        /// <summary>
        /// ID and IP Address of the current leader or null if no leader is decided (or this server doesn't know about it!)
        /// </summary>
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
                }

                _leader = value;
                if (_leader != null)
                    Console.WriteLine("Leader set to " + _leader?.guid + " with IP " + _leader?.ip);
                else
                    Console.WriteLine("Leader unset.");
            }
        }
        private (Guid guid, IPAddress ip)? _leader = null;

        /// <summary>
        /// For the leader, this timer regularly sends out the heartbeat.
        /// For a non-leader, this timer is used to check if the heartbeat of the leader is received regularly.
        /// </summary>
        private Timer? heartbeatTimer = null;

        /// <summary>
        /// Sends regular heartbeats to the multicast group of the match.
        /// </summary>
        private readonly Timer? matchHeartbeatTimer = null;

        /// <summary>
        /// How many players can partake in a match.
        /// </summary>
        public const byte MAX_PLAYER_COUNT = 2;

        /// <summary>
        /// Set containing the PlayerIDs of all players partaking in the current match. Also includes players
        /// which are not currently connected.
        /// </summary>
        private readonly HashSet<Guid> players = new();

        /// <summary>
        /// List containing the rounds that have been played. currentRound is added to this list after it is finished.
        /// </summary>
        private readonly List<MatchRound> finishedRounds = new();
        /// <summary>
        /// Stores information about the current round.
        /// </summary>
        private MatchRound? currentRound = null;
        /// <summary>
        /// Counts the number of rounds played. Is incremented when a new round starts.
        /// </summary>
        private int roundsPlayed = 0;

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
        private readonly Dictionary<Guid, (IPAddress ipAddress, HeartbeatResponsePacket status, long lastTimestamp)> serverStatuses = new();

        /// <summary>
        /// Set which stores all servers this server knows. Used during election.
        /// </summary>
        private readonly HashSet<(Guid id, IPAddress ipAddress)> knownServers = new();

        /// <summary>
        /// Random Number Generator.
        /// </summary>
        private Random rand = new();

        public Server()
        {
            Console.WriteLine("Starting server...");

            Console.WriteLine("Loading valid words from stadt.txt, land.txt, and fluss.txt...");
            wordValidator = new();
            Console.WriteLine("Word loading complete.");

            networkingClient = new NetworkingClient(ServerId, IPAddress.Parse("239.0.0.1"));

            receiveThread = new Thread(ReceiveNetworkingMessages);
            receiveThread.Start();

            tcpListenerThread = new Thread(ListenTcp);
            tcpListenerThread.Start();

            // generate a multicast IP for the match run by this server
            IPAddress matchMulticastAddress;
            while (true)
            {
                // generate a random multicast IP
                byte secondByte = (byte)(rand.Next(254) + 1);
                byte thirdByte = (byte)(rand.Next(254) + 1);
                byte fourthByte = (byte)(rand.Next(254) + 1);
                matchMulticastAddress = new IPAddress(new byte[] { 239, secondByte, thirdByte, fourthByte });

                // don't accept 239.0.0.1 if it is generated, because that is the multicast IP of our server group
                if (!Equals(matchMulticastAddress, IPAddress.Parse("239.0.0.1")))
                    break;
            }

            // set up a networking client for communicating with players in the match
            matchNetworkingClient = new(ServerId, matchMulticastAddress, 1338);

            matchReceiveThread = new Thread(ReceiveMatchNetworkingMessages);
            matchReceiveThread.Start();

            // start match heartbeat timer
            matchHeartbeatTimer = new Timer(500)
            {
                AutoReset = true,
            };
            matchHeartbeatTimer.Elapsed += (sender, args) =>
            {
                if (matchNetworkingClient.InMulticastGroup)
                {
                    // regularly send a heartbeat to the players in the match
                    matchNetworkingClient.SendHeartbeatToGroup();
                }
            };
            matchHeartbeatTimer.Start();

            Console.WriteLine("Server startup complete! Server running!");
        }

        private void ListenTcp()
        {
            TcpListener tcpListener = new(IPAddress.Any, 1337);
            tcpListener.Start();

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                Console.WriteLine("[TCP] Accepted a TCP client.");
                ThreadPool.QueueUserWorkItem(TcpClientReceiveMessage, client);
            }
        }

        private void TcpClientReceiveMessage(object? obj)
        {
            byte[] data = null;
            IPAddress sender = null;
            using (TcpClient client = (TcpClient)obj!)
            {

                sender = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

                using NetworkStream ns = client.GetStream();

                // Read the data into a byte array
                byte[] buffer = new byte[1024];
                using MemoryStream ms = new();

                int bytesRead;
                while ((bytesRead = ns.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }

                data = ms.ToArray();
            }

            SlfPacketBase packet = SlfPacketBase.FromBytes(data.Cast<byte>().GetEnumerator());

            Console.WriteLine("[TCP] Received a packet of type " + packet.GetType().Name);

            if (packet is StartElectionPacket)
            {
                // ignore election packets when we have a lower id than the sender
                if (ServerId < packet.SenderId)
                    return;

                ElectionResponsePacket electionResponsePacket = new(ServerId);
                
                // send a response to the server the start election packet came from
                using (TcpClient tcpClient = new())
                {
                    Console.WriteLine("[TCP] Sending an election response message to " + sender);
                    tcpClient.Connect(new IPEndPoint(sender, 1337));
                    using NetworkStream ns = tcpClient.GetStream();

                    ns.Write(electionResponsePacket.ToBytes());
                    ns.Flush();
                }

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
        }

        public async void StartElection()
        {
            // the king is dead
            Leader = null;

            heartbeatTimer?.Stop();
            heartbeatTimer?.Dispose();

            StartElectionPacket startElectionPacket = new(ServerId);

            State = ServerState.ELECTION_STARTED;

            // send start election packets to servers with higher ID than ourselves and wait for a response
            foreach ((Guid id, IPAddress ipAddress) knownServer in knownServers)
            {
                // only send election messages to servers with an ID greater than our own
                if (knownServer.id <= ServerId) continue;

                new Thread(() =>
                {
                    try
                    {
                        Console.WriteLine("[TCP] Sending a start election message to " + knownServer.ipAddress);
                        using TcpClient tcpClient = new();

                        tcpClient.Connect(new IPEndPoint(knownServer.ipAddress, 1337));
                        using NetworkStream ns = tcpClient.GetStream();

                        ns.Write(startElectionPacket.ToBytes());

                        ns.Flush();
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine("SocketException while trying to reach other server during leader election. The server is probably offline...");
                    }
                }).Start();
            }

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

                Console.WriteLine("[ServerGroup-Client] Received a packet of type " + packet.GetType().Name);

                // RequestMatchAssignmentPackets are sent by clients, so these are ignored. For all other packet types, it means
                // they were sent by a server so we add that server to the knownServers set
                if (packet is not RequestMatchAssignmentPacket)
                {
                    knownServers.Add((packet.SenderId, sender));
                }


                if (packet is HeartbeatPacket)
                {
                    if (!Leader.HasValue || packet.SenderId != Leader.Value.guid || heartbeatTimer == null)
                    {
                        Console.WriteLine("Starting leader election because leader who sent the heartbeat wasn't the leader I expected!");
                        StartElection();
                        continue;
                    }

                    // received a heartbeat from the leader, so reset the timer
                    heartbeatTimer.Stop();
                    heartbeatTimer.Start();

                    // send a response to the heartbeat, containing some information about us and our current match status
                    HeartbeatResponsePacket heartbeatResponse = new(ServerId)
                    {
                        HasMatchRunning = matchId != null,
                        MatchId = matchId ?? Guid.Empty, // matchId if it is not null, otherwise empty guid
                        MaxPlayerCount = MAX_PLAYER_COUNT,
                        CurrentPlayers = players.ToArray(),
                    };

                    networkingClient.SendOneOffToGroup(heartbeatResponse);
                }
                else if (packet is HeartbeatResponsePacket heartbeatResponsePacket)
                {
                    // update this server's (the server the heartbeat response came from) status in the serverStatuses dictionary
                    serverStatuses[heartbeatResponsePacket.SenderId] = (sender, heartbeatResponsePacket, GetCurrentTimeMillis());
                }
                else if (packet is RequestMatchAssignmentPacket requestMatchAssignmentPacket)
                {
                    Console.WriteLine("Received a match assignment request.");

                    // only the leader should handle this packet, because the leader assigns players to game servers
                    if (Leader == null || Leader.Value.guid != ServerId)
                    {
                        Console.WriteLine("[Match Assignment] I'm not the leader server, so I'm not going to handle it.");
                        continue;
                    }

                    Guid playerId = requestMatchAssignmentPacket.SenderId;

                    // firstly, check if the player is already part of a running match on a server, and redirect them to that server if
                    // that is the case (This can happen e.g. if the player crashes during the match and they restart their client
                    // and want to continue playing that same match)
                    (Guid id, IPAddress ip) matchServer = serverStatuses
                        .Where(x => (GetCurrentTimeMillis() - x.Value.lastTimestamp) < 2000) // skip server if we haven't received a heartbeat for >2sec
                        .Where(x => x.Value.status.CurrentPlayers.Contains(playerId))
                        .Select(x => (x.Key, x.Value.ipAddress))
                        .FirstOrDefault();

                    // if not, check if player is part of match on THIS server
                    if (matchServer == default((Guid id, IPAddress ip)))
                    {
                        if (players.Contains(playerId))
                        {
                            matchServer = (ServerId, IPAddress.Parse("127.0.0.1"));
                        }
                    }

                    // if we don't have a server where the player was already part of the match, find a server with a free slot
                    if (matchServer == default((Guid id, IPAddress ip)))
                    {
                        Console.WriteLine("[Match Assignment] Could not find any matches the player is already part of. Finding a server with free slots...");
                        matchServer = serverStatuses
                            .Where(x => (GetCurrentTimeMillis() - x.Value.lastTimestamp) < 2000) // skip server if we haven't received a heartbeat for >2sec
                            .Where(x => x.Value.status.CurrentPlayers.Length < x.Value.status.MaxPlayerCount)
                            .Select(x => (x.Key, x.Value.ipAddress))
                            .FirstOrDefault();
                    }

                    // if no slot found on other servers, check if a slot is free on the leader server
                    if (matchServer == default((Guid id, IPAddress ip)))
                    {
                        if (players.Count < MAX_PLAYER_COUNT)
                        {
                            matchServer = (ServerId, IPAddress.Parse("127.0.0.1"));
                        }
                    }

                    // otherwise, deny the match assignment request
                    if (matchServer == default((Guid id, IPAddress ip)))
                    {
                        Console.WriteLine("[Match Assignment] Could not find any servers with free player slots. Denying match assignment request.");
                        MatchAssignmentPacket errorResponsePacket = new(ServerId, false, "No free player slots found on any server!", Guid.Empty, "");
                        networkingClient.SendOneOff(errorResponsePacket, sender);
                        continue;
                    }

                    // send response to the client requesting a match assignment
                    MatchAssignmentPacket responsePacket = new(ServerId, true, "success", matchServer.id, matchServer.ip.ToString());
                    networkingClient.SendOneOff(responsePacket, sender);
                }
                else if (packet is LeaderAnnouncementPacket)
                {
                    // start a new election if we have a higher id than the announced leader (something has gone wrong!)
                    if (packet.SenderId < ServerId)
                    {
                        Console.WriteLine("Starting leader election because the leader that just announced itself has a lower ID than me!");
                        StartElection();
                        return;
                    }

                    // long live the king
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
                (IPAddress sender, SlfPacketBase packet) = matchNetworkingClient.Receive();

                Console.WriteLine("[MatchGroup-Client] Received a packet of type " + packet.GetType().Name);

                if (packet is MatchJoinPacket matchJoinPacket)
                {
                    // either a slot has to be free for the player, or they already need to be part of the match
                    bool accept = players.Count < MAX_PLAYER_COUNT || players.Contains(matchJoinPacket.SenderId);

                    // if the server, for some reason, does not have a match multicast group, then reject the player
                    if (!matchNetworkingClient.InMulticastGroup)
                    {
                        Console.WriteLine("Had to reject a client because we are not in a multicast group! THIS SHOULD NEVER HAPPEN!");
                        accept = false;
                    }

                    // add player to our players list
                    players.Add(matchJoinPacket.SenderId);

                    // if no match is running, start one
                    if (matchId == null)
                        StartMatch();

                    MatchJoinResponsePacket response = new(ServerId, accept, matchNetworkingClient.MulticastAddress?.ToString(), matchId);
                    matchNetworkingClient.SendOneOff(response, sender, 1337);
                }
                else if (packet is RoundFinishPacket roundFinishPacket)
                {
                    // ignore this packet if it is not related to the match running on this server
                    if (roundFinishPacket.MatchId != matchId)
                        continue;

                    currentlyWaitingForPlayerAnswers = true;

                    new Thread(() =>
                    {
                        // give 2 seconds for the answers of the players to arrive, messages after timeout are not considered
                        Thread.Sleep(2000);

                        // we're not accepting any more answers!
                        currentlyWaitingForPlayerAnswers = false;

                        if (matchId == null)
                            throw new Exception(
                                "MatchID is null even though we received a RoundFinishPacket! This should never happen!");

                        if (currentRound == null)
                            throw new Exception("CurrentRound is null even though we are receiving player answer packets!");

                        // send the results packet
                        RoundResultPacket responsePacket = new(
                            ServerId,
                            matchId.Value,
                            currentRound.Letter,
                            currentRound.PlayerAnswers.Select(x => (x.Key, x.Value)).ToList()
                        );

                        matchNetworkingClient.SendOrderedReliableToGroup(responsePacket);

                        // wait another 5 seconds before starting the next round
                        Thread.Sleep(5000);

                        // only start another round if we have played fewer than 3 rounds up till now
                        if (roundsPlayed < 3)
                        {
                            StartRound();
                        }
                        else
                        {
                            EndMatch();
                        }

                    }).Start();
                }
                else if (packet is SubmitWordsPacket submitWordsPacket)
                {
                    if (currentRound == null)
                        throw new Exception("CurrentRound is null even though we are receiving player answer packets!");

                    // ignore this packet if it is not related to the match running on this server
                    if (submitWordsPacket.MatchId != matchId)
                        continue;

                    // ignore this type of packet if the server isn't currently waiting for player answers (i.e. the round has
                    // finished but the next one hasn't started yet)
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

            StartRound();
        }

        /// <summary>
        /// Ends the current match. Throws an exception if called when no match is running.
        /// </summary>
        private void EndMatch()
        {
            if (matchId == null)
                throw new Exception("Tried to end match when no match is running!");

            // add last round to finishedRounds list
            if (currentRound != null)
                finishedRounds.Add(currentRound);

            // score calculation as described in the game rules in our report
            Dictionary<Guid, int> playerScores = new();
            foreach (MatchRound round in finishedRounds)
            {
                foreach ((Guid playerId, MatchRound.Answers playerAnswers) in round.PlayerAnswers)
                {
                    int cityScore = 0;

                    // if answer wasn't accepted, we don't get any points for it
                    if (playerAnswers.City.Accepted)
                    {
                        cityScore = 5;

                        // if no one else has the same word, we get 10 points for it
                        if (round.PlayerAnswers
                            .Where(x => x.Key != playerId)
                            .All(x => x.Value.City.Text != playerAnswers.City.Text))
                            cityScore = 10;

                        // if no one else has any solution in this category, we get 20 points for it
                        if (round.PlayerAnswers
                            .Where(x => x.Key != playerId)
                            .All(x => string.IsNullOrEmpty(x.Value.City.Text)))
                            cityScore = 20;
                    }

                    int countryScore = 0;

                    // if answer wasn't accepted, we don't get any points for it
                    if (playerAnswers.Country.Accepted)
                    {
                        countryScore = 5;

                        // if no one else has the same word, we get 10 points for it
                        if (round.PlayerAnswers
                            .Where(x => x.Key != playerId)
                            .All(x => x.Value.Country.Text != playerAnswers.Country.Text))
                            countryScore = 10;

                        // if no one else has any solution in this category, we get 20 points for it
                        if (round.PlayerAnswers
                            .Where(x => x.Key != playerId)
                            .All(x => string.IsNullOrEmpty(x.Value.Country.Text)))
                            countryScore = 20;
                    }

                    int riverScore = 0;

                    // if answer wasn't accepted, we don't get any points for it
                    if (playerAnswers.River.Accepted)
                    {
                        riverScore = 5;

                        // if no one else has the same word, we get 10 points for it
                        if (round.PlayerAnswers
                            .Where(x => x.Key != playerId)
                            .All(x => x.Value.River.Text != playerAnswers.River.Text))
                            riverScore = 10;

                        // if no one else has any solution in this category, we get 20 points for it
                        if (round.PlayerAnswers
                            .Where(x => x.Key != playerId)
                            .All(x => string.IsNullOrEmpty(x.Value.River.Text)))
                            riverScore = 20;
                    }

                    // if player not yet in dictionary, initialize with 0
                    playerScores.TryAdd(playerId, 0);

                    // add scores from this round to player's total score
                    playerScores[playerId] += cityScore + countryScore + riverScore;
                }
            }

            // generate the printout displayed after the match
            StringBuilder sb = new();
            foreach ((Guid playerId, int score) in playerScores)
            {
                sb.Append("\n");
                sb.Append(playerId);
                sb.Append(": ");
                sb.Append(score);
                sb.Append(" Points");
            }

            MatchEndPacket packet = new(ServerId, matchId.Value, sb.ToString());

            // delete/reset all our match-specific data
            matchId = null;
            roundsPlayed = 0;
            currentlyWaitingForPlayerAnswers = false;
            players.Clear();
            finishedRounds.Clear();
            currentRound = null;

            // send match end packet to notify players that the match has ended
            matchNetworkingClient.SendOrderedReliableToGroup(packet);
            // reset the networking client so it's ready to take on a new match
            matchNetworkingClient.Reset();
        }

        private void StartRound()
        {
            const string allowedLetters = "abcdefghijklmnopqrstuvwxyz";

            // add last round to finishedRounds list
            if(currentRound != null)
                finishedRounds.Add(currentRound);

            currentRound = new MatchRound(allowedLetters[rand.Next(0, allowedLetters.Length)].ToString());
            currentlyWaitingForPlayerAnswers = false;

            if (matchId == null)
                throw new Exception(
                    "MatchID is null even though we want to start a round! This should never happen!");

            // send round start packet
            RoundStartPacket packet = new(ServerId, matchId.Value, currentRound.Letter, finishedRounds.Count + 1);
            matchNetworkingClient.SendOrderedReliableToGroup(packet);

            roundsPlayed++;
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

        private static long GetCurrentTimeMillis()
        {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long currentTimeMillis = (long)(DateTime.UtcNow - epochStart).TotalMilliseconds;
            return currentTimeMillis;
        }
    }
}
