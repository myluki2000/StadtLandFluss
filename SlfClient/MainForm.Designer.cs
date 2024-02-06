namespace SlfClient
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            tbCity = new TextBox();
            label2 = new Label();
            label3 = new Label();
            tbRiver = new TextBox();
            tbCountry = new TextBox();
            btnFinish = new Button();
            lblSelectedLetter = new Label();
            lblOutput = new RichTextBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(34, 30);
            label1.Name = "label1";
            label1.Size = new Size(31, 15);
            label1.TabIndex = 0;
            label1.Text = "City:";
            // 
            // tbCity
            // 
            tbCity.Location = new Point(71, 27);
            tbCity.Name = "tbCity";
            tbCity.Size = new Size(204, 23);
            tbCity.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 59);
            label2.Name = "label2";
            label2.Size = new Size(53, 15);
            label2.TabIndex = 2;
            label2.Text = "Country:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(29, 88);
            label3.Name = "label3";
            label3.Size = new Size(36, 15);
            label3.TabIndex = 3;
            label3.Text = "River:";
            // 
            // tbRiver
            // 
            tbRiver.Location = new Point(71, 85);
            tbRiver.Name = "tbRiver";
            tbRiver.Size = new Size(204, 23);
            tbRiver.TabIndex = 4;
            // 
            // tbCountry
            // 
            tbCountry.Location = new Point(71, 56);
            tbCountry.Name = "tbCountry";
            tbCountry.Size = new Size(204, 23);
            tbCountry.TabIndex = 5;
            // 
            // btnFinish
            // 
            btnFinish.Location = new Point(200, 114);
            btnFinish.Name = "btnFinish";
            btnFinish.Size = new Size(75, 23);
            btnFinish.TabIndex = 7;
            btnFinish.Text = "Finish!";
            btnFinish.UseVisualStyleBackColor = true;
            btnFinish.Click += btnFinish_Click;
            // 
            // lblSelectedLetter
            // 
            lblSelectedLetter.AutoSize = true;
            lblSelectedLetter.Location = new Point(12, 9);
            lblSelectedLetter.Name = "lblSelectedLetter";
            lblSelectedLetter.Size = new Size(181, 15);
            lblSelectedLetter.TabIndex = 8;
            lblSelectedLetter.Text = "The selected letter this round is: -";
            // 
            // lblOutput
            // 
            lblOutput.BackColor = SystemColors.Control;
            lblOutput.BorderStyle = BorderStyle.None;
            lblOutput.Location = new Point(281, 6);
            lblOutput.Name = "lblOutput";
            lblOutput.ReadOnly = true;
            lblOutput.Size = new Size(346, 432);
            lblOutput.TabIndex = 9;
            lblOutput.Text = "";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(639, 450);
            Controls.Add(lblOutput);
            Controls.Add(lblSelectedLetter);
            Controls.Add(btnFinish);
            Controls.Add(tbCountry);
            Controls.Add(tbRiver);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(tbCity);
            Controls.Add(label1);
            Name = "MainForm";
            Text = "MainForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox tbCity;
        private Label label2;
        private Label label3;
        private TextBox tbRiver;
        private TextBox tbCountry;
        private Button btnFinish;
        private Label lblSelectedLetter;
        private RichTextBox lblOutput;
    }
}
