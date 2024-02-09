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
            frmMain?.Close();
            frmMain?.Dispose();
            frmMain = null;

            StringBuilder sb = new("The match has ended. Final scores:");

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

                    playerScores[playerId] += cityScore + countryScore + riverScore;
                }
            }

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


            if (matchClient != null)
                matchClient.OnMatchEnd -= MatchClientOnMatchEnd;
            matchClient?.Dispose();
            matchClient = null;


            MessageBox.Show(sb.ToString());
        }

        private void btnJoinGame_Click(object sender, EventArgs e)
        {
            matchClient ??= new(config.PlayerId);
            matchClient.OnMatchEnd += MatchClientOnMatchEnd;

            matchClient.JoinNewGame();

            lblLoading.Text = "Searching game...";
            lblLoading.Visible = true;

            new Thread(() =>
            {
                // give it 6 seconds to connect to a game
                Thread.Sleep(6000);

                // hide the loading screen
                lblLoading.Visible = false;

                // check if we actually connected
                if (matchClient.IsInMatch)
                {
                    // if we did, open the main window to start playing
                    Console.WriteLine("AAAAAAAAAAAAAAAAAAAAA");
                    frmMain?.Dispose();
                    frmMain = new MainForm(matchClient);
                    frmMain.ShowDialog();
                }
                else
                {
                    // otherwise, show an error message
                    MessageBox.Show("Error while connecting to game. Please try again.");
                }
            }).Start();
        }
    }
}
