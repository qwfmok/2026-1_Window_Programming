using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CardChess.Models;
using CardChess.Core;
using CardChess.Pieces;
using CardChess.Input;

namespace CardChess
{
    public partial class MainForm : Form
    {
        // --- master 브랜치의 UI 변수들 ---
        // --- GameState 대신 GameManager와 InputController로 교체 했습니다. (현빈)---

        private GameManager gameManager;
        private InputController inputController;
        private Label lblTurn;
        private Panel pnlBoard;
        private Button btnRestart;
        private ListBox logbox;
        private Button[,] boardButtons = new Button[8, 8];

        public MainForm()
        {
            InitializeComponent();

            this.Width = 1600;
            this.Height = 900;

            gameManager = new GameManager(); //따라서 여기도
            inputController = new InputController(gameManager, PlayerType.Player1);
            inputController.OnLogMessage += (sender, msg) => { logbox.Items.Add(msg); };

            CreateBoard();
            RefreshBoard();
        }

        // --- feat1 브랜치에서 추가된 그래픽 그리기 로직(병합 완료) 오류투성이라 일단 잠시 주석처리함---
        //private void Assets(object sender, PaintEventArgs e)
        //{
        //    e.Graphics.DrawImage(chboard.Boardimage, chboard.X, chboard.Y, CardChess.Core.BoardManager.BOARD_WIDTH, CardChess.Core.BoardManager.BOARD_HEIGHT);
        //    int cellSize = (int)(CardChess.Core.BoardManager.BOARD_WIDTH / CardChess.Core.BoardManager.MAX_COL);
        //}

        // --- master 브랜치의 보드 생성 로직 ---

        private void CreateBoard()
        {
            int cellSize = 70;

            pnlBoard.Controls.Clear();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Button btn = new Button();

                    btn.Width = cellSize;
                    btn.Height = cellSize;
                    btn.Left = col * cellSize;
                    btn.Top = row * cellSize;

                    btn.FlatStyle = FlatStyle.Flat;
                    btn.Font = new Font("맑은 고딕", 9, FontStyle.Bold);

                    if ((row + col) % 2 == 0)
                        btn.BackColor = Color.White;
                    else
                        btn.BackColor = Color.Gray;

                    Position position = new Position(row, col);
                    btn.Tag = position;

                    btn.Click += BoardButton_Click;

                    pnlBoard.Controls.Add(btn);
                    boardButtons[row, col] = btn;
                }
            }
        }

        private void RefreshBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Position position = new Position(row, col);
                    IPiece piece = gameManager.State.GetPieceAt(position);

                    Button btn = boardButtons[row, col];

                    if (piece == null)
                    {
                        btn.Text = "";
                    }
                    else
                    {
                        // 임시 글자 "말" 대신 실제 주인과 기물 종류 표시 (예: Player1 Pawn)
                        btn.Text = $"{piece.Owner}\n{piece.Type}";
                    }
                }
            }

            lblTurn.Text = $"현재 턴: {gameManager.CurrentTurn}";
        }

        private void BoardButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;

            if (clickedButton == null)
                return;

            Position position = (Position)clickedButton.Tag;

            // --- 알림창을 지우고 컨트롤러에 좌표 전달 ---
            inputController.OnBoardClicked(position);

            // 컨트롤러가 이동 로직을 끝냈으니 화면을 다시 그려줌
            RefreshBoard();
        }

        private void InitializeComponent()
        {
            this.lblTurn = new System.Windows.Forms.Label();
            this.pnlBoard = new System.Windows.Forms.Panel();
            this.btnRestart = new System.Windows.Forms.Button();
            this.logbox = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // lblTurn
            // 
            this.lblTurn.AutoSize = true;
            this.lblTurn.Font = new System.Drawing.Font("굴림", 16.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblTurn.Location = new System.Drawing.Point(20, 20);
            this.lblTurn.Name = "lblTurn";
            this.lblTurn.Size = new System.Drawing.Size(175, 22);
            this.lblTurn.TabIndex = 0;
            this.lblTurn.Text = "현재 턴: Player1";
            // 
            // pnlBoard
            // 
            this.pnlBoard.Location = new System.Drawing.Point(20, 60);
            this.pnlBoard.Name = "pnlBoard";
            this.pnlBoard.Size = new System.Drawing.Size(560, 560);
            this.pnlBoard.TabIndex = 1;
            // 
            // btnRestart
            // 
            this.btnRestart.Location = new System.Drawing.Point(610, 580);
            this.btnRestart.Name = "btnRestart";
            this.btnRestart.Size = new System.Drawing.Size(120, 40);
            this.btnRestart.TabIndex = 2;
            this.btnRestart.Text = "button1";
            this.btnRestart.UseVisualStyleBackColor = true;
            this.btnRestart.Click += new System.EventHandler(this.btnRestart_Click);
            // 
            // logbox
            // 
            this.logbox.FormattingEnabled = true;
            this.logbox.ItemHeight = 12;
            this.logbox.Location = new System.Drawing.Point(598, 60);
            this.logbox.Name = "logbox";
            this.logbox.Size = new System.Drawing.Size(251, 160);
            this.logbox.TabIndex = 3;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(882, 653);
            this.Controls.Add(this.logbox);
            this.Controls.Add(this.btnRestart);
            this.Controls.Add(this.pnlBoard);
            this.Controls.Add(this.lblTurn);
            this.Name = "MainForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void btnRestart_Click(object sender, EventArgs e)
        {

        }
    }
}