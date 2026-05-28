namespace CardChess
{
    partial class MainManual
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
            this.btnPieceManual = new System.Windows.Forms.Button();
            this.btnCardManual = new System.Windows.Forms.Button();
            this.btnGameManual = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnPieceManual
            // 
            this.btnPieceManual.Location = new System.Drawing.Point(600, 150);
            this.btnPieceManual.Name = "btnPieceManual";
            this.btnPieceManual.Size = new System.Drawing.Size(400, 100);
            this.btnPieceManual.TabIndex = 0;
            this.btnPieceManual.Text = "기물 설명";
            this.btnPieceManual.UseVisualStyleBackColor = true;
            this.btnPieceManual.Click += new System.EventHandler(this.btnPieceManual_Click);
            // 
            // btnCardManual
            // 
            this.btnCardManual.Location = new System.Drawing.Point(600, 350);
            this.btnCardManual.Name = "btnCardManual";
            this.btnCardManual.Size = new System.Drawing.Size(400, 100);
            this.btnCardManual.TabIndex = 1;
            this.btnCardManual.Text = "카드 설명";
            this.btnCardManual.UseVisualStyleBackColor = true;
            this.btnCardManual.Click += new System.EventHandler(this.btnCardManual_Click);
            // 
            // btnGameManual
            // 
            this.btnGameManual.Location = new System.Drawing.Point(600, 550);
            this.btnGameManual.Name = "btnGameManual";
            this.btnGameManual.Size = new System.Drawing.Size(400, 100);
            this.btnGameManual.TabIndex = 2;
            this.btnGameManual.Text = "게임 설명";
            this.btnGameManual.UseVisualStyleBackColor = true;
            this.btnGameManual.Click += new System.EventHandler(this.btnGameManual_Click);
            // 
            // btnBack
            // 
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Location = new System.Drawing.Point(1441, 47);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(90, 40);
            this.btnBack.TabIndex = 26;
            this.btnBack.Text = "뒤로가기";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // MainManual
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1578, 844);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnGameManual);
            this.Controls.Add(this.btnCardManual);
            this.Controls.Add(this.btnPieceManual);
            this.Name = "MainManual";
            this.Text = "게임 방법";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnPieceManual;
        private System.Windows.Forms.Button btnCardManual;
        private System.Windows.Forms.Button btnGameManual;
        private System.Windows.Forms.Button btnBack;
    }
}