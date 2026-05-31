namespace CardChess
{
    partial class GameManual
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
            this.rtbDescription = new System.Windows.Forms.RichTextBox();
            this.picTop = new System.Windows.Forms.PictureBox();
            this.picBottom = new System.Windows.Forms.PictureBox();
            this.btnRuleControl = new System.Windows.Forms.Button();
            this.btnRuleTurn = new System.Windows.Forms.Button();
            this.btnRuleBasic = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picBottom)).BeginInit();
            this.SuspendLayout();
            // 
            // rtbDescription
            // 
            this.rtbDescription.BackColor = System.Drawing.SystemColors.InfoText;
            this.rtbDescription.Location = new System.Drawing.Point(726, 132);
            this.rtbDescription.Name = "rtbDescription";
            this.rtbDescription.Size = new System.Drawing.Size(608, 650);
            this.rtbDescription.TabIndex = 27;
            this.rtbDescription.Text = "";
            // 
            // picTop
            // 
            this.picTop.BackColor = System.Drawing.Color.Transparent;
            this.picTop.Location = new System.Drawing.Point(235, 132);
            this.picTop.Name = "picTop";
            this.picTop.Size = new System.Drawing.Size(457, 322);
            this.picTop.TabIndex = 28;
            this.picTop.TabStop = false;
            // 
            // picBottom
            // 
            this.picBottom.BackColor = System.Drawing.Color.Transparent;
            this.picBottom.Location = new System.Drawing.Point(235, 460);
            this.picBottom.Name = "picBottom";
            this.picBottom.Size = new System.Drawing.Size(457, 322);
            this.picBottom.TabIndex = 29;
            this.picBottom.TabStop = false;
            // 
            // btnRuleControl
            // 
            this.btnRuleControl.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRuleControl.Location = new System.Drawing.Point(635, 47);
            this.btnRuleControl.Name = "btnRuleControl";
            this.btnRuleControl.Size = new System.Drawing.Size(150, 40);
            this.btnRuleControl.TabIndex = 32;
            this.btnRuleControl.Text = "조작법 및 화면";
            this.btnRuleControl.UseVisualStyleBackColor = true;
            this.btnRuleControl.Click += new System.EventHandler(this.btnRuleControl_Click);
            // 
            // btnRuleTurn
            // 
            this.btnRuleTurn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRuleTurn.Location = new System.Drawing.Point(435, 47);
            this.btnRuleTurn.Name = "btnRuleTurn";
            this.btnRuleTurn.Size = new System.Drawing.Size(150, 40);
            this.btnRuleTurn.TabIndex = 31;
            this.btnRuleTurn.Text = "턴 진행 방식";
            this.btnRuleTurn.UseVisualStyleBackColor = true;
            this.btnRuleTurn.Click += new System.EventHandler(this.btnRuleTurn_Click);
            // 
            // btnRuleBasic
            // 
            this.btnRuleBasic.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRuleBasic.Location = new System.Drawing.Point(235, 47);
            this.btnRuleBasic.Name = "btnRuleBasic";
            this.btnRuleBasic.Size = new System.Drawing.Size(150, 40);
            this.btnRuleBasic.TabIndex = 30;
            this.btnRuleBasic.Text = "룰 및 승리조건";
            this.btnRuleBasic.UseVisualStyleBackColor = true;
            this.btnRuleBasic.Click += new System.EventHandler(this.btnRuleBasic_Click);
            // 
            // btnBack
            // 
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Location = new System.Drawing.Point(1447, 45);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(90, 40);
            this.btnBack.TabIndex = 33;
            this.btnBack.Text = "뒤로가기";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // GameManual
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1578, 844);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnRuleControl);
            this.Controls.Add(this.btnRuleTurn);
            this.Controls.Add(this.btnRuleBasic);
            this.Controls.Add(this.picBottom);
            this.Controls.Add(this.picTop);
            this.Controls.Add(this.rtbDescription);
            this.Name = "GameManual";
            this.Text = "게임 설명";
            ((System.ComponentModel.ISupportInitialize)(this.picTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picBottom)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.RichTextBox rtbDescription;
        private System.Windows.Forms.PictureBox picTop;
        private System.Windows.Forms.PictureBox picBottom;
        private System.Windows.Forms.Button btnRuleControl;
        private System.Windows.Forms.Button btnRuleTurn;
        private System.Windows.Forms.Button btnRuleBasic;
        private System.Windows.Forms.Button btnBack;
    }
}