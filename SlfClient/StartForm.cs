using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SlfCommon;

namespace SlfClient
{
    public partial class StartForm : Form
    {
        /// <summary>
        /// Loaded player client config.
        /// </summary>
        private readonly Config config;

        private MatchClient? matchClient;

        private MainForm? frmMain;

        public StartForm(Config config)
        {
            Console.WriteLine("Started.");

            this.config = config;
            InitializeComponent();

            lblPlayerId.Text = "Player ID: " + config.PlayerId;
        }

        private void MatchClientOnMatchEnd(object? sender, EventArgs e)
        {
            Invoke(() => EndMatch());
        }

        private void MatchClientOnServerConnectionLost(object? sender, EventArgs e)
        {
            Invoke(() => EndMatch("Connection to server was lost."));
        }

        /// <summary>
        /// Helper method to call when a match has ended for whatever reason to display final results and afterwards bring
        /// the client back into a state where it can connect to a new match. Optionally, a reason for the match ending can
        /// be provided which will be displayed to the user.
        /// </summary>
        private void EndMatch(string? reason = null)
        {
            frmMain?.Close();
            frmMain?.Dispose();
            frmMain = null;

            StringBuilder sb = new();

            if (reason == null)
            {
                sb.Append("The match has ended.");
            }
            else
            {
                sb.Append("The match has ended for the following reason: ");
                sb.Append(reason);
            }

            sb.AppendLine("Final scores:");

            // score calculation as described in the game rules in our report
            Dictionary<Guid, int> playerScores = new();
            foreach (MatchRound round in matchClient.FinishedRounds)
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
            foreach ((Guid playerId, int score) in playerScores)
            {
                sb.Append("\n");
                sb.Append(playerId);
                if (playerId == matchClient.Identity)
                    sb.Append(" (You!)");
                sb.Append(": ");
                sb.Append(score);
                sb.Append(" Points");
            }

            // clean-up before we dispose of the match client
            if (matchClient != null)
            {
                matchClient.OnMatchEnd -= MatchClientOnMatchEnd;
                matchClient.OnServerConnectionLost -= MatchClientOnServerConnectionLost;
            }

            matchClient?.Dispose();
            matchClient = null;

            // Show the message box with our generated printout
            MessageBox.Show(sb.ToString());
        }

        private void btnJoinGame_Click(object sender, EventArgs e)
        {
            matchClient ??= new(config.PlayerId);
            matchClient.OnMatchEnd += MatchClientOnMatchEnd;
            matchClient.OnServerConnectionLost += MatchClientOnServerConnectionLost;

            matchClient.JoinNewGame();

            lblLoading.Text = "Searching game...";
            lblLoading.Visible = true;

            new Thread(() =>
            {
                // give it 6 seconds to connect to a game
                Thread.Sleep(6000);

                Invoke(() =>
                {
                    // hide the loading screen
                    lblLoading.Visible = false;

                    // check if we actually connected
                    if (matchClient.IsInMatch)
                    {
                        // if we did, open the main window to start playing
                        frmMain?.Dispose();
                        frmMain = new MainForm(matchClient);
                        frmMain.ShowDialog();
                    }
                    else
                    {
                        // otherwise, show an error message
                        MessageBox.Show("Error while connecting to game. Please try again.");
                    }
                });
            }).Start();
        }
    }
}
