using SlfCommon.Networking;
using SlfCommon.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SlfCommon;
using System.Text.RegularExpressions;
using Timer = System.Timers.Timer;

namespace SlfClient
{
    /// <summary>
    /// Class used by the Stadt Land Fluss game client to connect to and play on the Stadt Land Fluss server network.
    /// </summary>
    public class MatchClient : IDisposable
    {
        /// <summary>
        /// Event which is raised when the client joins a match. Argument indicates whether join was accepted or not.
        /// </summary>
        public event EventHandler<bool>? OnMatchJoinResponse;
        /// <summary>
        /// Event which is raised when the match server signals a new round beginning.
        /// </summary>
        public event EventHandler? OnRoundStarted;
        /// <summary>
        /// Event which is raised when a player signals that they are finishing the round.
        /// </summary>
        public event EventHandler? OnRoundFinished;
        /// <summary>
        /// Event which is raised when the client receives the final results of a round from the server.
        /// </summary>
        public event EventHandler<MatchRound>? OnRoundResults;
        /// <summary>
        /// Event which is raised when the game server signals that the match has ended.
        /// </summary>
        public event EventHandler? OnMatchEnd;
        /// <summary>
        /// Event which is raised when connection to the server is lost (i.e. heartbeat packets do not arrive for a while).
        /// </summary>
        public event EventHandler? OnServerConnectionLost;

        /// <summary>
        /// If a round is running, contains the letter for that round. Otherwise null.
        /// </summary>
        public string? CurrentLetter { get; private set; }

        /// <summary>
        /// List containing data about the rounds we have played in this match.
        /// </summary>
        public List<MatchRound> FinishedRounds { get; } = new();

        /// <summary>
        /// Our Player ID.
        /// </summary>
        public Guid Identity { get; }

        /// <summary>
        /// Networking Client used to communicate with servers.
        /// </summary>
        private NetworkingClient networkingClient;

        /// <summary>
        /// Thread which processes messages received by the networkingClient.
        /// </summary>
        private Thread receiveThread;

        /// <summary>
        /// ID of the server we are playing on. NULL if a server to play on hasn't been selected yet.
        /// </summary>
        private Guid? matchServerId = null;
        /// <summary>
        /// IP of the server we are playing on. NULL if a server to play on hasn't been selected yet.
        /// NULL if we currently aren't in a match.
        /// </summary>
        private IPAddress? matchServerIp = null;
        /// <summary>
        /// Multicast IP-Address used to communicate during a match with players and the match server.
        /// NULL if we currently aren't in a match.
        /// </summary>
        private IPAddress? matchMulticastIp = null;
        /// <summary>
        /// ID of the match we are playing in. NULL if we are not currently partaking in a match.
        /// </summary>
        public Guid? MatchId = null;

        /// <summary>
        /// True if the client is connected to a match, false otherwise.
        /// </summary>
        public bool IsInMatch => MatchId != null;

        /// <summary>
        /// Timer used to check if the heartbeat of the game server is still there.
        /// </summary>
        private readonly Timer heartbeatTimer;

        public MatchClient(Guid identity)
        {
            Identity = identity;
            networkingClient = new(identity);

            receiveThread = new Thread(ReceiveNetworkingMessages);
            receiveThread.Start();

            heartbeatTimer = new Timer(2000);
            heartbeatTimer.Elapsed += (sender, args) =>
            {
                // if this timer elapses it means we haven't received a heartbeat from the server for more than the timer duration
                // let's raise an event for that!
                OnServerConnectionLost?.Invoke(this, EventArgs.Empty);
            };
        }

        /// <summary>
        /// Send a request to join a new game (get assigned a server to play on).
        /// </summary>
        public void JoinNewGame()
        {
            RequestMatchAssignmentPacket packet = new(Identity);

            Console.WriteLine("Sending RequestMatchAssignmentPacket...");

            // send match request packet to the multicast group of the servers
            networkingClient.SendOneOff(packet, IPAddress.Parse("239.0.0.1"));
        }

        /// <summary>
        /// Submit the specified words. Should only be called after we have confirmed that the round has finished
        /// (i.e. RoundFinishPacket has been received), otherwise the submitted words will not be considered by
        /// the server.
        /// </summary>
        public void SubmitWords(string city, string country, string river)
        {
            if (MatchId == null)
                throw new Exception(
                    "Tried to submit words even though MatchClient doesn't have a MatchID (are we even part of a match?)");

            SubmitWordsPacket packet = new(Identity, MatchId.Value, city, country, river);

            networkingClient.SendOrderedReliableToGroup(packet);
        }

        /// <summary>
        /// Looping method of the networkingClient receive thread.
        /// </summary>
        private void ReceiveNetworkingMessages()
        {
            while (true)
            {
                (IPAddress sender, SlfPacketBase packet) = networkingClient.Receive();

                Console.WriteLine("[MatchClient] Processing a packet of type " + packet.GetType().Name);

                if (packet is MatchAssignmentPacket matchAssignmentPacket)
                {
                    Console.WriteLine("Client has been assigned a match on server " + matchAssignmentPacket.MatchServerIp);
                    matchServerId = matchAssignmentPacket.MatchServerId;
                    matchServerIp = matchAssignmentPacket.MatchServerIp == "127.0.0.1" 
                        ? sender 
                        : IPAddress.Parse(matchAssignmentPacket.MatchServerIp);

                    // after getting assigned to a match server by the leader, we send a MatchJoinPacket to that match server
                    // to notify it that we will now take part in the match.
                    MatchJoinPacket matchJoinPacket = new(Identity);
                    networkingClient.SendOneOff(matchJoinPacket, matchServerIp, 1338);
                }
                else if (packet is MatchJoinResponsePacket matchJoinResponsePacket)
                {
                    // if our join request was accepted, store the address and stuff we need to partake in the match
                    // otherwise reset our data so we can try to get a different server
                    if (matchJoinResponsePacket.Accepted)
                    {
                        Console.WriteLine("Client has been accepted in match on server " + matchServerIp);
                        matchMulticastIp = IPAddress.Parse(matchJoinResponsePacket.MatchMulticastIp);
                        MatchId = matchJoinResponsePacket.MatchId;

                        heartbeatTimer.Start();

                        // create a new network client which joins the game server's match multicast group instead of the
                        // server group multicast group
                        networkingClient.Dispose();
                        Console.WriteLine("Joining match server's multicast group " + matchMulticastIp);
                        networkingClient = new NetworkingClient(Identity, matchMulticastIp, 1338);
                        networkingClient.OnMulticastHeartbeatReceived += NetworkingClientOnMulticastHeartbeatReceived;
                    }
                    else
                    {
                        // reset everything if the server doesn't let us play on it :(
                        Console.WriteLine("Client has been denied to join match on server " + matchServerIp);
                        matchMulticastIp = null;
                        MatchId = null;
                        matchServerIp = null;
                        matchServerId = null;

                        heartbeatTimer.Stop();
                    }

                    OnMatchJoinResponse?.Invoke(this, matchJoinResponsePacket.Accepted);
                }
                else if (packet is RoundStartPacket roundStartPacket)
                {
                    // ignore this packet if it is not related to the match this client is partaking in
                    if (roundStartPacket.MatchId != MatchId)
                        continue;

                    CurrentLetter = roundStartPacket.Letter;
                    OnRoundStarted?.Invoke(this, EventArgs.Empty);
                }
                else if (packet is RoundFinishPacket roundFinishPacket)
                {
                    // ignore this packet if it is not related to the match this client is partaking in
                    if (roundFinishPacket.MatchId != MatchId)
                        continue;

                    CurrentLetter = null;
                    OnRoundFinished?.Invoke(this, EventArgs.Empty);
                }
                else if (packet is RoundResultPacket roundResultPacket)
                {
                    // ignore this packet if it is not related to the match this client is partaking in
                    if (roundResultPacket.MatchId != MatchId)
                        continue;

                    // construct the MatchRound data structure we use internally to store match information
                    // from the data we received in the packet
                    MatchRound round = new(roundResultPacket.Letter);

                    int playerCount = roundResultPacket.Players.Length;

                    for (int i = 0; i < playerCount; i++)
                    {
                        Guid playerId = roundResultPacket.Players[i];

                        string city = roundResultPacket.Cities[i];
                        bool cityAccepted = roundResultPacket.CitiesAccepted[i];

                        string country = roundResultPacket.Countries[i];
                        bool countryAccepted = roundResultPacket.CountriesAccepted[i];

                        string river = roundResultPacket.Rivers[i];
                        bool riverAccepted = roundResultPacket.RiversAccepted[i];

                        round.PlayerAnswers.Add(
                            playerId, 
                            new MatchRound.Answers(
                                new(city, cityAccepted), 
                                new(country, countryAccepted), 
                                new(river, riverAccepted)
                            )
                        );
                    }

                    FinishedRounds.Add(round);

                    OnRoundResults?.Invoke(this, round);
                }
                else if (packet is MatchEndPacket matchEndPacket)
                {
                    // ignore this packet if it is not related to the match this client is partaking in
                    if (matchEndPacket.MatchId != MatchId)
                        continue;

                    heartbeatTimer.Stop();

                    OnMatchEnd?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Console.WriteLine("Ignored received packet of type " + packet.GetType().Name);
                }
            }
        }

        private void NetworkingClientOnMulticastHeartbeatReceived(object? sender, IPEndPoint e)
        {
            // reset the heartbeat timeout
            heartbeatTimer.Stop();
            heartbeatTimer.Start();
        }

        /// <summary>
        /// Sends a message to all clients partaking in the match (and the server), we have ended the round.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void FinishRound()
        {
            if (MatchId == null)
                throw new Exception(
                    "Tried to finish a round even though MatchClient doesn't have a MatchID (are we even part of a match?)");
            

            RoundFinishPacket packet = new(Identity, MatchId.Value);
            networkingClient.SendOrderedReliableToGroup(packet);
        }


        public void Dispose()
        {
            networkingClient.Dispose();
            heartbeatTimer.Dispose();
        }
    }
}
