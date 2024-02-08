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
        /// If a round is running, contains the letter for that round. Otherwise null.
        /// </summary>
        public string? CurrentLetter { get; private set; }

        public List<MatchRound> FinishedRounds { get; } = new();

        public Guid Identity { get; }

        private NetworkingClient networkingClient;

        private Thread receiveThread;

        private Guid? matchServerId = null;
        private IPAddress? matchServerIp = null;

        private IPAddress? matchMulticastIp = null;

        public Guid? MatchId = null;

        /// <summary>
        /// True if the client is connected to a match, false otherwise.
        /// </summary>
        public bool IsInMatch => MatchId != null;

        public MatchClient(Guid identity)
        {
            Identity = identity;
            networkingClient = new(identity);

            receiveThread = new Thread(ReceiveNetworkingMessages);
            receiveThread.Start();
        }

        public void JoinNewGame()
        {
            RequestMatchAssignmentPacket packet = new(Identity);

            Console.WriteLine("Sending RequestMatchAssignmentPacket...");

            // send match request packet to the multicast group of the servers
            networkingClient.SendOneOff(packet, IPAddress.Parse("239.0.0.1"));
        }

        public void SubmitWords(string city, string country, string river)
        {
            if (MatchId == null)
                throw new Exception(
                    "Tried to submit words even though MatchClient doesn't have a MatchID (are we even part of a match?)");

            SubmitWordsPacket packet = new(Identity, MatchId.Value, city, country, river);

            networkingClient.SendOrderedReliableToGroup(packet);
        }

        private void ReceiveNetworkingMessages()
        {
            while (true)
            {
                (IPAddress sender, SlfPacketBase packet) = networkingClient.Receive();

                Console.WriteLine("Received something");

                if (packet is MatchAssignmentPacket matchAssignmentPacket)
                {
                    Console.WriteLine("Client has been assigned a match on server " + matchAssignmentPacket.MatchServerIp);
                    matchServerId = matchAssignmentPacket.MatchServerId;
                    matchServerIp = matchAssignmentPacket.MatchServerIp == "127.0.0.1" 
                        ? sender 
                        : IPAddress.Parse(matchAssignmentPacket.MatchServerIp);

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

                        // create a new network client which joins the game server's match multicast group instead of the
                        // server group multicast group
                        networkingClient.Dispose();
                        Console.WriteLine("Joining match server's multicast group " + matchMulticastIp);
                        networkingClient = new NetworkingClient(Identity, matchMulticastIp, 1338);
                    }
                    else
                    {
                        Console.WriteLine("Client has been denied to join match on server " + matchServerIp);
                        matchMulticastIp = null;
                        MatchId = null;
                        matchServerIp = null;
                        matchServerId = null;
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

                    OnMatchEnd?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Console.WriteLine("Ignored received packet of type " + packet.GetType().Name);
                }
            }
        }

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
        }
    }
}
