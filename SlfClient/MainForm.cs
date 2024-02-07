using System.Text;
using SlfCommon;

namespace SlfClient
{
    public partial class MainForm : Form
    {
        private readonly MatchClient matchClient;

        public MainForm(MatchClient matchClient)
        {
            this.matchClient = matchClient;

            Console.WriteLine("Opening game window...");

            InitializeComponent();

            matchClient.OnRoundStarted += MatchClientOnRoundStarted;
            matchClient.OnRoundFinished += MatchClientOnRoundFinished;
            matchClient.OnRoundResults += MatchClientOnRoundResults;

            lblSelectedLetter.Text = "The selected letter this round is: " + matchClient.CurrentLetter ?? "-";
        }

        private void MatchClientOnRoundFinished(object? sender, EventArgs e)
        {
            SubmitWords();
        }

        private void MatchClientOnRoundStarted(object? sender, EventArgs e)
        {
            lblSelectedLetter.Text = "The selected letter this round is: " + matchClient.CurrentLetter;

            tbCity.Enabled = true;
            tbCountry.Enabled = true;
            tbRiver.Enabled = true;
            btnFinish.Enabled = true;
        }

        private void MatchClientOnRoundResults(object? sender, MatchRound round)
        {
            lblOutput.AppendText("----- Round -----\n");

            lblOutput.AppendText("Starting Letter: ");
            lblOutput.AppendText(round.Letter);
            lblOutput.AppendText("\n\n");

            lblOutput.AppendText("Results:\n");

            foreach (KeyValuePair<Guid, MatchRound.Answers> pair in round.PlayerAnswers)
            {
                lblOutput.AppendText("- ");
                lblOutput.AppendText(pair.Key.ToString());
                lblOutput.AppendText(" -\n");

                lblOutput.AppendText("City: ");
                lblOutput.SelectionColor = pair.Value.City.Accepted ? Color.Green : Color.Red;
                lblOutput.AppendText(pair.Value.City.Text);
                lblOutput.AppendText(pair.Value.City.Accepted ? " \u2714\n" : "\u2718\n");
                lblOutput.SelectionColor = SystemColors.WindowText;

                lblOutput.AppendText("Country: ");
                lblOutput.SelectionColor = pair.Value.Country.Accepted ? Color.Green : Color.Red;
                lblOutput.AppendText(pair.Value.Country.Text);
                lblOutput.AppendText(pair.Value.Country.Accepted ? " \u2714\n" : "\u2718\n");
                lblOutput.SelectionColor = SystemColors.WindowText;

                lblOutput.AppendText("River: ");
                lblOutput.SelectionColor = pair.Value.River.Accepted ? Color.Green : Color.Red;
                lblOutput.AppendText(pair.Value.River.Text);
                lblOutput.AppendText(pair.Value.River.Accepted ? " \u2714\n" : "\u2718\n");
                lblOutput.SelectionColor = SystemColors.WindowText;

                lblOutput.AppendText("\n");
            }
        }

        private void btnFinish_Click(object sender, EventArgs e)
        {
            matchClient.FinishRound();
            SubmitWords();
        }

        private void SubmitWords()
        {
            matchClient.SubmitWords(tbCity.Text, tbCountry.Text, tbRiver.Text);
            tbCity.Enabled = false;
            tbCountry.Enabled = false;
            tbRiver.Enabled = false;
            btnFinish.Enabled = false;
        }
    }
}
