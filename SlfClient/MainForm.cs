namespace SlfClient
{
    public partial class MainForm : Form
    {
        private readonly MatchClient matchClient;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnFinish_Click(object sender, EventArgs e)
        {
            matchClient.FinishRound();
            matchClient.SubmitWords(tbCity.Text, tbCountry.Text, tbRiver.Text);
        }
    }
}
