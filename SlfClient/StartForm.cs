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

        private void MatchClientOnMatchEnd(object? sender, string matchInformation)
        {
            Invoke(() => EndMatch(matchInformation));
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
        private void EndMatch(string? additionalInformation = null)
        {
            frmMain?.Close();
            frmMain?.Dispose();
            frmMain = null;

            StringBuilder sb = new();

            
            sb.Append("The match has ended.");
            
            if(additionalInformation != null)
            {
                sb.Append(" Additional information:\n");
                sb.Append(additionalInformation);
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
