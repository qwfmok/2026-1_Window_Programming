namespace CardChess
{
    partial class CardManual
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
            this.btnBack = new System.Windows.Forms.Button();
            this.rtbDescription = new System.Windows.Forms.RichTextBox();
            this.picCard = new System.Windows.Forms.PictureBox();
            this.listBoxCards = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.picCard)).BeginInit();
            this.SuspendLayout();
            // 
            // btnBack
            // 
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Location = new System.Drawing.Point(1447, 45);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(90, 40);
            this.btnBack.TabIndex = 25;
            this.btnBack.Text = "뒤로가기";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // rtbDescription
            // 
            this.rtbDescription.BackColor = System.Drawing.SystemColors.InfoText;
            this.rtbDescription.Location = new System.Drawing.Point(840, 100);
            this.rtbDescription.Name = "rtbDescription";
            this.rtbDescription.Size = new System.Drawing.Size(561, 650);
            this.rtbDescription.TabIndex = 24;
            this.rtbDescription.Text = "";
            // 
            // picCard
            // 
            this.picCard.BackColor = System.Drawing.Color.Transparent;
            this.picCard.Location = new System.Drawing.Point(330, 100);
            this.picCard.Name = "picCard";
            this.picCard.Size = new System.Drawing.Size(470, 650);
            this.picCard.TabIndex = 21;
            this.picCard.TabStop = false;
            // 
            // listBoxCards
            // 
            this.listBoxCards.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.listBoxCards.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.listBoxCards.ForeColor = System.Drawing.Color.White;
            this.listBoxCards.FormattingEnabled = true;
            this.listBoxCards.ItemHeight = 38;
            this.listBoxCards.Location = new System.Drawing.Point(122, 100);
            this.listBoxCards.Name = "listBoxCards";
            this.listBoxCards.Size = new System.Drawing.Size(166, 650);
            this.listBoxCards.TabIndex = 26;
            this.listBoxCards.SelectedIndexChanged += new System.EventHandler(this.listBoxCards_SelectedIndexChanged);
            // 
            // CardManual
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1578, 844);
            this.Controls.Add(this.listBoxCards);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.rtbDescription);
            this.Controls.Add(this.picCard);
            this.Name = "CardManual";
            this.Text = "CardManual";
            ((System.ComponentModel.ISupportInitialize)(this.picCard)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.RichTextBox rtbDescription;
        private System.Windows.Forms.PictureBox picCard;
        private System.Windows.Forms.ListBox listBoxCards;
    }
}