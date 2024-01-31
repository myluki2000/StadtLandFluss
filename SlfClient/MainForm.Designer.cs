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
            lblOutput = new Label();
            btnFinish = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(34, 9);
            label1.Name = "label1";
            label1.Size = new Size(31, 15);
            label1.TabIndex = 0;
            label1.Text = "City:";
            // 
            // tbCity
            // 
            tbCity.Location = new Point(71, 6);
            tbCity.Name = "tbCity";
            tbCity.Size = new Size(204, 23);
            tbCity.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 38);
            label2.Name = "label2";
            label2.Size = new Size(53, 15);
            label2.TabIndex = 2;
            label2.Text = "Country:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(29, 67);
            label3.Name = "label3";
            label3.Size = new Size(36, 15);
            label3.TabIndex = 3;
            label3.Text = "River:";
            // 
            // tbRiver
            // 
            tbRiver.Location = new Point(71, 64);
            tbRiver.Name = "tbRiver";
            tbRiver.Size = new Size(204, 23);
            tbRiver.TabIndex = 4;
            // 
            // tbCountry
            // 
            tbCountry.Location = new Point(71, 35);
            tbCountry.Name = "tbCountry";
            tbCountry.Size = new Size(204, 23);
            tbCountry.TabIndex = 5;
            // 
            // lblOutput
            // 
            lblOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblOutput.Location = new Point(281, 6);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new Size(346, 435);
            lblOutput.TabIndex = 6;
            lblOutput.Text = "label4";
            // 
            // btnFinish
            // 
            btnFinish.Location = new Point(200, 93);
            btnFinish.Name = "btnFinish";
            btnFinish.Size = new Size(75, 23);
            btnFinish.TabIndex = 7;
            btnFinish.Text = "Finish!";
            btnFinish.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(639, 450);
            Controls.Add(btnFinish);
            Controls.Add(lblOutput);
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
        private Label lblOutput;
        private Button btnFinish;
    }
}
