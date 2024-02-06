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

namespace SlfClient
{
    internal class MatchClient
    {
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
        public event EventHandler<MatchRound> OnRoundResults;

        /// <summary>
        /// If a round is running, contains the letter for that round. Otherwise null.
        /// </summary>
        public string? CurrentLetter { get; private set; }

        public List<MatchRound> FinishedRounds { get; } = new();

        public Guid Identity { get; }

        private readonly NetworkingClient networkingClient;

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

        public void SubmitWords(string city, string country, string river)
        {
            SubmitWordsPacket packet = new(Identity, city, country, river);

            networkingClient.SendOrderedReliableToGroup(packet);
        }

        private void ReceiveNetworkingMessages()
        {
            while (true)
            {
                (IPAddress sender, SlfPacketBase packet) = networkingClient.Receive();

                if (packet is MatchAssignmentPacket matchAssignmentPacket)
                {
                    Console.WriteLine("Client has been assigned a match on server " + matchAssignmentPacket.MatchServerIp);
                    matchServerId = matchAssignmentPacket.MatchServerId;
                    matchServerIp = IPAddress.Parse(matchAssignmentPacket.MatchServerIp);

                    MatchJoinPacket matchJoinPacket = new(Identity);
                    networkingClient.SendOneOff(matchJoinPacket, matchServerIp);
                }
                else if (packet is RoundStartPacket roundStartPacket)
                {
                    CurrentLetter = roundStartPacket.Letter;
                    OnRoundStarted?.Invoke(this, EventArgs.Empty);
                }
                else if (packet is RoundFinishPacket roundFinishPacket)
                {
                    CurrentLetter = null;
                    OnRoundFinished?.Invoke(this, EventArgs.Empty);
                }
                else if (packet is RoundResultPacket roundResultPacket)
                {
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
                else
                {
                    Console.WriteLine("Ignored received packet of type " + packet.GetType().Name);
                }
            }
        }

        public void FinishRound()
        {
            RoundFinishPacket packet = new(Identity);
            networkingClient.SendOrderedReliableToGroup(packet);
        }

        
    }
}
