using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

            if(matchClient != null)
                matchClient.OnMatchEnd -= MatchClientOnMatchEnd;
            matchClient?.Dispose();
            matchClient = null;

            MessageBox.Show("The match has ended.");
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
