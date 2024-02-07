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

        private readonly MatchClient client;

        private MainForm? frmMain;

        public StartForm(Config config)
        {
            Console.WriteLine("Started.");

            this.config = config;
            InitializeComponent();

            lblPlayerId.Text = "Player ID: " + config.PlayerId;

            client = new(config.PlayerId);
        }

        private void btnJoinGame_Click(object sender, EventArgs e)
        {
            client.JoinNewGame();

            lblLoading.Text = "Searching game...";
            lblLoading.Visible = true;

            new Thread(() =>
            {
                // give it 6 seconds to connect to a game
                Thread.Sleep(6000);

                // hide the loading screen
                lblLoading.Visible = false;

                // check if we actually connected
                if (client.IsInMatch)
                {
                    // if we did, open the main window to start playing
                    Console.WriteLine("AAAAAAAAAAAAAAAAAAAAA");
                    frmMain?.Dispose();
                    frmMain = new MainForm(client);
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
