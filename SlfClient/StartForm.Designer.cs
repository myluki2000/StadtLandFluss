namespace SlfClient
{
    partial class StartForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            btnJoinGame = new Button();
            lblPlayerId = new Label();
            lblLoading = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Dock = DockStyle.Top;
            label1.Font = new Font("Cascadia Code", 20F, FontStyle.Bold);
            label1.Location = new Point(0, 0);
            label1.Name = "label1";
            label1.Size = new Size(309, 107);
            label1.TabIndex = 0;
            label1.Text = "Stadt\r\n Land    The Game\r\n  Fluss";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnJoinGame
            // 
            btnJoinGame.Location = new Point(12, 134);
            btnJoinGame.Name = "btnJoinGame";
            btnJoinGame.Size = new Size(285, 39);
            btnJoinGame.TabIndex = 1;
            btnJoinGame.Text = "Join Game";
            btnJoinGame.UseVisualStyleBackColor = true;
            btnJoinGame.Click += btnJoinGame_Click;
            // 
            // lblPlayerId
            // 
            lblPlayerId.AutoSize = true;
            lblPlayerId.Location = new Point(12, 176);
            lblPlayerId.Name = "lblPlayerId";
            lblPlayerId.Size = new Size(38, 15);
            lblPlayerId.TabIndex = 2;
            lblPlayerId.Text = "label2";
            // 
            // lblLoading
            // 
            lblLoading.Dock = DockStyle.Fill;
            lblLoading.Location = new Point(0, 107);
            lblLoading.Name = "lblLoading";
            lblLoading.Size = new Size(309, 93);
            lblLoading.TabIndex = 3;
            lblLoading.Text = "label2";
            lblLoading.TextAlign = ContentAlignment.MiddleCenter;
            lblLoading.Visible = false;
            // 
            // StartForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(309, 200);
            Controls.Add(lblLoading);
            Controls.Add(lblPlayerId);
            Controls.Add(btnJoinGame);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "StartForm";
            Text = "StartForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Button btnJoinGame;
        private Label lblPlayerId;
        private Label lblLoading;
    }
}