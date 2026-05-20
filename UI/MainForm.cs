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
        private Panel pnlBoard;
        private ListBox logbox;
        private Panel pnlOpponentHand;
        private Panel pnlPlayerHand;
        private Panel pnlOpponentDeck;
        private Panel pnlPlayerDeck;
        private Panel pnlDrawArea;
        private Panel pnlPlayArea;
        private Button[,] boardButtons = new Button[8, 8];

        public MainForm()
        {
            InitializeComponent();

            gameManager = new GameManager(); //따라서 여기도
            inputController = new InputController(gameManager, PlayerType.Player1);
            inputController.OnLogMessage += (sender, msg) => { logbox.Items.Add(msg); };

            CreateBoard();
            RefreshBoard();
        }

        // --- feat1 브랜치에서 추가된 그래픽 그리기 로직 (병합 완료) 오류투성이라 일단 잠시 주석처리함---
        //private void Assets(object sender, PaintEventArgs e)
        //{
        //    e.Graphics.DrawImage(chboard.Boardimage, chboard.X, chboard.Y, CardChess.Core.BoardManager.BOARD_WIDTH, CardChess.Core.BoardManager.BOARD_HEIGHT);
        //    int cellSize = (int)(CardChess.Core.BoardManager.BOARD_WIDTH / CardChess.Core.BoardManager.MAX_COL);
        //}

        // --- master 브랜치의 보드 생성 로직 ---
        private void CreateBoard()
        {
            int cellSize = 90;

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

            //lblTurn.Text = $"현재 턴: {gameManager.CurrentTurn}";
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
            this.pnlBoard = new System.Windows.Forms.Panel();
            this.logbox = new System.Windows.Forms.ListBox();
            this.pnlOpponentHand = new System.Windows.Forms.Panel();
            this.pnlPlayerHand = new System.Windows.Forms.Panel();
            this.pnlOpponentDeck = new System.Windows.Forms.Panel();
            this.pnlPlayerDeck = new System.Windows.Forms.Panel();
            this.pnlDrawArea = new System.Windows.Forms.Panel();
            this.pnlPlayArea = new System.Windows.Forms.Panel();
            this.pnlOpponentHand.SuspendLayout();
            this.pnlPlayerHand.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlBoard
            // 
            this.pnlBoard.Location = new System.Drawing.Point(30, 30);
            this.pnlBoard.Name = "pnlBoard";
            this.pnlBoard.Size = new System.Drawing.Size(720, 720);
            this.pnlBoard.TabIndex = 1;
            // 
            // logbox
            // 
            this.logbox.FormattingEnabled = true;
            this.logbox.ItemHeight = 15;
            this.logbox.Location = new System.Drawing.Point(780, 240);
            this.logbox.Name = "logbox";
            this.logbox.Size = new System.Drawing.Size(334, 184);
            this.logbox.TabIndex = 3;
            // 
            // pnlOpponentHand
            // 
            this.pnlOpponentHand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlOpponentHand.Controls.Add(this.pnlOpponentDeck);
            this.pnlOpponentHand.Location = new System.Drawing.Point(780, 30);
            this.pnlOpponentHand.Name = "pnlOpponentHand";
            this.pnlOpponentHand.Size = new System.Drawing.Size(590, 190);
            this.pnlOpponentHand.TabIndex = 4;
            // 
            // pnlPlayerHand
            // 
            this.pnlPlayerHand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPlayerHand.Controls.Add(this.pnlPlayerDeck);
            this.pnlPlayerHand.Location = new System.Drawing.Point(780, 440);
            this.pnlPlayerHand.Name = "pnlPlayerHand";
            this.pnlPlayerHand.Size = new System.Drawing.Size(590, 280);
            this.pnlPlayerHand.TabIndex = 5;
            // 
            // pnlOpponentDeck
            // 
            this.pnlOpponentDeck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlOpponentDeck.Location = new System.Drawing.Point(465, 3);
            this.pnlOpponentDeck.Name = "pnlOpponentDeck";
            this.pnlOpponentDeck.Size = new System.Drawing.Size(120, 180);
            this.pnlOpponentDeck.TabIndex = 6;
            // 
            // pnlPlayerDeck
            // 
            this.pnlPlayerDeck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPlayerDeck.Location = new System.Drawing.Point(465, 95);
            this.pnlPlayerDeck.Name = "pnlPlayerDeck";
            this.pnlPlayerDeck.Size = new System.Drawing.Size(120, 180);
            this.pnlPlayerDeck.TabIndex = 7;
            // 
            // pnlDrawArea
            // 
            this.pnlDrawArea.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlDrawArea.Location = new System.Drawing.Point(1120, 240);
            this.pnlDrawArea.Name = "pnlDrawArea";
            this.pnlDrawArea.Size = new System.Drawing.Size(120, 180);
            this.pnlDrawArea.TabIndex = 8;
            // 
            // pnlPlayArea
            // 
            this.pnlPlayArea.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPlayArea.Location = new System.Drawing.Point(1246, 240);
            this.pnlPlayArea.Name = "pnlPlayArea";
            this.pnlPlayArea.Size = new System.Drawing.Size(120, 180);
            this.pnlPlayArea.TabIndex = 9;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1382, 753);
            this.Controls.Add(this.pnlDrawArea);
            this.Controls.Add(this.pnlPlayArea);
            this.Controls.Add(this.pnlPlayerHand);
            this.Controls.Add(this.pnlOpponentHand);
            this.Controls.Add(this.logbox);
            this.Controls.Add(this.pnlBoard);
            this.Name = "MainForm";
            this.pnlOpponentHand.ResumeLayout(false);
            this.pnlPlayerHand.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}