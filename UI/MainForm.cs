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
using System.IO;
using CardChess.Cards; // 카드를 인식하기 위해 추가!

namespace CardChess
{
    public partial class MainForm : Form
    {
        // --- master 브랜치의 UI 변수들 ---
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
        private Button btnPassTurn;
        private Button ghostCard = null;
        private Button originalCardButton = null;
        private bool gameEndMessageShown = false;

        public MainForm()
        {
            InitializeComponent();

            this.Width = 1600;
            this.Height = 900;

            gameManager = new GameManager(); 
            inputController = new InputController(gameManager, PlayerType.Player1);
            inputController.OnLogMessage += (sender, msg) =>
            {
                AddLog(msg);
            };

            CreateBoard();
            RefreshBoard();
            RefreshHand();
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

                    btn.AllowDrop = true;
                    btn.DragEnter += BoardButton_DragEnter;
                    btn.DragDrop += BoardButton_DragDrop;

                    pnlBoard.Controls.Add(btn);
                    boardButtons[row, col] = btn;
                }
            }
        }

        // 마우스가 카드를 끌고 보드판 위에 올라왔을 때 (허락해주는 역할)
        private void BoardButton_DragEnter(object sender, DragEventArgs e)
        {
            // 끌고 온 데이터가 ActiveSkillCard가 맞으면 마우스 커서를 '이동' 모양으로 바꿔줌
            if (e.Data.GetDataPresent(typeof(ActiveSkillCard)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        // 보드판 위에서 마우스 클릭을 탁! 놨을 때 (스킬 발동 역할)
        private void BoardButton_DragDrop(object sender, DragEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            // 드롭된 위치 좌표 확인
            Position position = (Position)btn.Tag;

            // 이미 마우스를 누를 때 inputController.OnCardClicked()가 실행된 상태니까,
            // 여기서는 깔끔하게 그 위치에 클릭했다는 신호만 넘겨주면 스킬 발동 끝!
            inputController.OnBoardClicked(position);

            // 화면 갱신
            RefreshBoard();
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
                        btn.BackgroundImage = null; // 빈칸은 이미지 지우기
                    }
                    else
                    {
                        btn.Text = ""; // 이미지를 넣을 거니까 텍스트는 싹 지워줌!

                        // 🌟 이미지 파일 이름 조합 (예: "Player1_Pawn.png")
                        string fileName = $"{piece.Owner}_{piece.Type}.png";
                        string imgPath = Path.Combine(Application.StartupPath, "Assets", fileName);

                        if (File.Exists(imgPath))
                        {
                            btn.BackgroundImage = Image.FromFile(imgPath);
                            btn.BackgroundImageLayout = ImageLayout.Zoom; // 버튼 크기에 꽉 차게 비율 조정
                        }
                        else
                        {
                            // 만약 해당 이름의 이미지가 폴더에 없다면 임시로 글자라도 띄워줌
                            btn.Text = $"{piece.Owner}\n{piece.Type}";
                        }
                    }
                }
            }
            // 보드를 새로고침 할 때, 손패(Hand)도 같이 새로고침!
            RefreshHand();
        }

        private void BoardButton_Click(object sender, EventArgs e)
        {
            if (gameManager.State.IsGameOver)
            {
                ShowGameEndMessageIfNeeded();
                return;
            }

            Button clickedButton = sender as Button;

            if (clickedButton == null)
                return;

            Position position = (Position)clickedButton.Tag;

            inputController.OnBoardClicked(position);

            RefreshBoard();

            ShowGameEndMessageIfNeeded();
        }

        // ==========================================================
        //  내 손패(Hand) UI 띄우기 및 연결 이거 쮸댄 하기 시러
        // ==========================================================

        private void RefreshHand()
        {
            pnlPlayerHand.Controls.Clear(); // 👈 카드를 새로 그리기 전에 기존 카드 싹 지우기!

            int cardWidth = 80;
            int cardHeight = 120;
            int spacing = 10;
            int startX = 10;
            int startY = 10;

            for (int i = 0; i < gameManager.State.Hands[PlayerType.Player1].Count; i++)
            {
                // 👇 여기도 수정!
                ICard card = gameManager.State.Hands[PlayerType.Player1][i];
                Button btnCard = new Button();

                btnCard.Width = cardWidth;
                btnCard.Height = cardHeight;
                btnCard.Left = startX + (cardWidth + spacing) * i;
                btnCard.Top = startY;

                btnCard.FlatStyle = FlatStyle.Flat;
                btnCard.BackColor = Color.LightGoldenrodYellow;
                btnCard.Font = new Font("맑은 고딕", 10, FontStyle.Bold);
                btnCard.Text = card.Name;
                btnCard.Tag = card;

                // 마우스 드래그 이벤트 연결
                btnCard.MouseDown += CardButton_MouseDown;

                pnlPlayerHand.Controls.Add(btnCard);
            }
        }

        private void CardButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                originalCardButton = sender as Button;
                ICard clickedCard = (ICard)originalCardButton.Tag;

                // 1. 컨트롤러에 카드 선택 알림
                inputController.OnCardClicked(clickedCard);

                // 2. 마우스를 따라다닐 '가짜 카드(분신)' 생성
                ghostCard = new Button();
                ghostCard.Width = originalCardButton.Width;
                ghostCard.Height = originalCardButton.Height;
                ghostCard.Text = originalCardButton.Text;
                ghostCard.Font = originalCardButton.Font;
                ghostCard.BackColor = originalCardButton.BackColor;
                ghostCard.FlatStyle = FlatStyle.Flat;

                // 3. 분신을 메인 폼에 추가하고 맨 앞으로 가져오기
                this.Controls.Add(ghostCard);
                ghostCard.BringToFront();

                // 4. 원래 카드는 드래그하는 동안 안 보이게 숨김
                originalCardButton.Visible = false;

                // 5. 마우스 움직임과 놓음 이벤트를 분신 카드에 연결!
                ghostCard.MouseMove += GhostCard_MouseMove;
                ghostCard.MouseUp += GhostCard_MouseUp;

                // 6. 마우스가 폼 밖으로 나가도 이 카드가 마우스를 꽉 쥐고 있게 설정
                ghostCard.Capture = true;
            }
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
            this.btnPassTurn = new System.Windows.Forms.Button();
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
            // btnPassTurn
            // 
            this.btnPassTurn.Location = new System.Drawing.Point(1120, 430);
            this.btnPassTurn.Name = "btnPassTurn";
            this.btnPassTurn.Size = new System.Drawing.Size(246, 40);
            this.btnPassTurn.TabIndex = 10;
            this.btnPassTurn.Text = "턴 넘기기";
            this.btnPassTurn.UseVisualStyleBackColor = true;
            this.btnPassTurn.Click += new System.EventHandler(this.BtnPassTurn_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1382, 753);
            this.Controls.Add(this.btnPassTurn);
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
        // 마우스를 드래그할 때 카드가 통째로 따라다니는 로직
        private void GhostCard_MouseMove(object sender, MouseEventArgs e)
        {
            if (ghostCard != null && e.Button == MouseButtons.Left)
            {
                // 마우스 커서의 현재 위치를 가져와서 카드의 정중앙이 마우스에 오도록 설정
                Point mousePos = this.PointToClient(Cursor.Position);
                ghostCard.Location = new Point(mousePos.X - (ghostCard.Width / 2), mousePos.Y - (ghostCard.Height / 2));
            }
        }
        


        // 드래그를 끝내고 마우스 왼쪽 버튼을 놨을 때 (스킬 발동!)
        private void GhostCard_MouseUp(object sender, MouseEventArgs e)
        {
            if (ghostCard != null)
            {
                ghostCard.Capture = false;

                // 1. 어떤 보드판 버튼 위에서 마우스를 놨는지 추적
                Button droppedBoardBtn = null;
                for (int row = 0; row < 8; row++)
                {
                    for (int col = 0; col < 8; col++)
                    {
                        Button b = boardButtons[row, col];
                        // 보드 버튼의 모니터 화면상 좌표를 계산
                        Rectangle screenBounds = b.RectangleToScreen(b.ClientRectangle);

                        // 마우스를 놓은 위치가 해당 보드 버튼 영역 안이라면?
                        if (screenBounds.Contains(Cursor.Position))
                        {
                            droppedBoardBtn = b;
                            break;
                        }
                    }
                    if (droppedBoardBtn != null) break;
                }

                // 2. 보드판 위에 제대로 놨다면 스킬 발동!
                if (droppedBoardBtn != null)
                {
                    Position targetPos = (Position)droppedBoardBtn.Tag;
                    inputController.OnBoardClicked(targetPos);
                    RefreshBoard();
                    ShowGameEndMessageIfNeeded();
                }
                else
                {
                    // 보드판 밖(허공)에 버렸다면 선택 취소
                    inputController.CancelSelection();
                }

                // 3. 작업이 끝났으니 분신 카드는 삭제하고 원래 카드를 다시 보여줌
                this.Controls.Remove(ghostCard);
                ghostCard.Dispose();
                ghostCard = null;

                originalCardButton.Visible = true;
                originalCardButton = null;
            }
        }

        private void BtnPassTurn_Click(object sender, EventArgs e)
        {
            gameManager.PassTurn();

            AddLog($"턴을 넘겼습니다. 현재 턴: {gameManager.CurrentTurn}");

            RefreshBoard();
            RefreshHand();
        }

        private void ShowGameEndMessageIfNeeded()
        {
            if (gameEndMessageShown)
                return;

            if (!gameManager.State.IsGameOver)
                return;

            string message;

            if (gameManager.State.Winner.HasValue)
                message = $"{gameManager.State.Winner.Value} 승리!";
            else
                message = "게임이 종료되었습니다.";

            logbox.Items.Add(message);
            gameEndMessageShown = true;

            MessageBox.Show(message, "게임 종료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddLog(string message)
        {
            logbox.Items.Add(message);

            if (logbox.Items.Count > 0)
            {
                logbox.TopIndex = logbox.Items.Count - 1;
            }
        }
    }
}