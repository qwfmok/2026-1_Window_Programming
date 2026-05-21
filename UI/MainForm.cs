using CardChess.Cards;
using CardChess.Core;
using CardChess.Input;
using CardChess.Models;
using CardChess.Pieces;
using CardChess.View;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CardChess
{
    public partial class MainForm : Form
    {
        // --- 핵심 제어 객체 ---
        private GameManager gameManager; // 게임 관리 객체
        private InputController inputController; // 입력 관리 객체
        private BoardView boardView; // 보드 관리 객체
        private BattleManager battleManager; // 전투 관리 객체

        // --- 폼 기본 디자인 컨트롤 ---
        private Panel pnlPlayerHand;
        private Panel pnlPlayerDeck;
        private Panel pnlBoard; // 중앙 패널보드 여기에 보드들어감 수정하다가 이거없어져서 ㅈ될뻔
        private Panel pnlDrawArea;
        private Panel pnlPlayArea;
        private ListBox logbox;
        private Panel pnlOpponentDeck;
        private Panel pnlOpponentHand;
        private Button btnPassTurn; 

        // --- 드래그 앤 드롭 카드 분신 제어 ---
        private Button ghostCard = null;
        private Button originalCardButton = null;
        private bool gameEndMessageShown = false;

        // --- 메인 메뉴인 Form1으로부터 UDP 프로토콜 연결 후 해당 통신망을 받아올 때 사용하는 변수임 ---
        private UDPprotocol udpProtocol;
        private Label lblNetworkStatus;

        // --- 엔진 타이머 ---
        private System.Windows.Forms.Timer gameLoopTimer;

        // --- 현재 플레이어 상태 나타내는 용도 ---
        private Image playerStateImg;

        // 진입점

        public MainForm(UDPprotocol connectedUdp, PlayerType assignedPlayerType)
        {
            InitializeComponent();

            this.Width = 1600;
            this.Height = 900; // MainForm(이하 메인폼)의 최초 크기 정의
            this.pnlBoard.Size = new Size(720, 720); // 패널보드의 최초 크기 정의

            // 메인폼 배경 이미지 설정 경로는 동일하게 바이너리/디버그의 에셋 폴더
            string bgPath = Path.Combine(Application.StartupPath, "Assets", "bg.png");
            if (File.Exists(bgPath))
            {
                this.BackgroundImage = Image.FromFile(bgPath);
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            this.DoubleBuffered = true; // 버퍼링 깜빡이는거 방지

            // 턴 나타내는 버튼
            string imgPath = Path.Combine(Application.StartupPath, "Assets", "button_long_player_state.png");
            if (File.Exists(imgPath))
            {
                playerStateImg = Image.FromFile(imgPath);
            }

            // 네트워크 끌고 들어온 흔적
            this.udpProtocol = connectedUdp;
            if (this.udpProtocol != null)
            {
                this.udpProtocol.OnMessage += UdpProtocol_OnMessage;
            }

            this.gameManager = new GameManager();
            this.inputController = new InputController(this.gameManager, assignedPlayerType);
            this.inputController.MyPlayerType = assignedPlayerType;

            this.gameManager.OnNetworkBroadcast += (msg) =>
            {
                if (udpProtocol != null && udpProtocol.IsConnected)
                    udpProtocol.Send(msg);
            };
            this.inputController.OnLogMessage += (sender, msg) => { AddLog(msg); };

            // 보드 뷰 참조
            this.boardView = new BoardView(pnlBoard, gameManager);
            InitCanvasBoardEvents();

            // 프레임 연산 구동용 메인 루프 타이머 가동
            gameLoopTimer = new System.Windows.Forms.Timer();
            gameLoopTimer.Interval = 20;
            gameLoopTimer.Tick += GameLoopTimer_Tick;
            gameLoopTimer.Start();

            // 배틀 매니저
            this.battleManager = new BattleManager(gameManager);
            this.battleManager.OnTurnChanged += BattleManager_OnTurnChanged;
            this.gameManager.OnTurnEndRequired += () => { battleManager.RequestTurnEnd(); };

            RefreshBoard();
            CardChess.Menu.Surrender.AddSurrenderButton(this, this.udpProtocol);

            // 이게 있어야 첫 턴이라는 개념이 생김
            _ = this.battleManager.ProcessNextPhase();
        }

        private void BattleManager_OnTurnChanged(PlayerType currentTurn)
        {
            // 이벤트 수신 시 버튼 비주얼 직접 변경 (1P면 1, 2P면 2)
            if (playerStateImg != null)
            {
                btnPassTurn.BackgroundImage = playerStateImg;
                btnPassTurn.BackgroundImageLayout = ImageLayout.Stretch;
            }

            if (currentTurn == PlayerType.Player1)
            {
                btnPassTurn.Text = "1P TURN (턴 종료)";
                btnPassTurn.ForeColor = Color.LightSkyBlue;
            }
            else
            {
                btnPassTurn.Text = "2P TURN (턴 종료)";
                btnPassTurn.ForeColor = Color.LightCoral;
            }

            RefreshBoard();
        }

        private void InitCanvasBoardEvents()
        {
            pnlBoard.Controls.Clear();
            pnlBoard.Paint += (s, e) => boardView.DrawBoard(e.Graphics);
            pnlBoard.MouseClick += PnlBoard_MouseClick;

            pnlBoard.AllowDrop = true;
            pnlBoard.DragEnter += BoardPanel_DragEnter;
            pnlBoard.DragDrop += BoardPanel_DragDrop;

            boardView.SyncPiecesWithBackend();
        }

        private void GameLoopTimer_Tick(object sender, EventArgs e)
        {
            boardView.UpdateLoopTick();
            pnlBoard.Invalidate();
        }

        private void PnlBoard_MouseClick(object sender, MouseEventArgs e)
        {
            if (!battleManager.IsPlayable)
                return;
            if (gameManager.State.IsGameOver)
            {
                ShowGameEndMessageIfNeeded();
                return;
            }
            if (gameManager.CurrentTurn != inputController.MyPlayerType)
            {
                return;
            }

            if (boardView.TryConvertPixelToPosition(e.X, e.Y, out Position position))
            {
                IPiece clickedPiece = gameManager.State.GetPieceAt(position);
                if (clickedPiece != null && boardView.PieceAnimations.ContainsKey(clickedPiece))
                {
                    boardView.PieceAnimations[clickedPiece].Onclick();
                }
                inputController.OnBoardClicked(position);
                boardView.HandleMovementAnimation();

                ShowGameEndMessageIfNeeded();
            }
        }

        private void BoardPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ActiveSkillCard)))
                e.Effect = DragDropEffects.Move;
        }

        private void BoardPanel_DragDrop(object sender, DragEventArgs e)
        {
            if (!battleManager.IsPlayable) return;

            Point clientPt = pnlBoard.PointToClient(new Point(e.X, e.Y));
            if (boardView.TryConvertPixelToPosition(clientPt.X, clientPt.Y, out Position position))
            {
                inputController.OnBoardClicked(position);
                boardView.HandleMovementAnimation();
            }
        }

        private void RefreshBoard()
        {
            pnlBoard.Invalidate();
            RefreshHand();
        }

        private void RefreshHand()
        {
            pnlPlayerHand.Controls.Clear();
            int cardWidth = 80, cardHeight = 120, spacing = 10, startX = 10, startY = 10;

            for (int i = 0; i < gameManager.State.Hands[PlayerType.Player1].Count; i++)
            {
                ICard card = gameManager.State.Hands[PlayerType.Player1][i];
                Button btnCard = new Button
                {
                    Width = cardWidth,
                    Height = cardHeight,
                    Left = startX + (cardWidth + spacing) * i,
                    Top = startY,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.LightGoldenrodYellow,
                    Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                    Text = card.Name,
                    Tag = card
                };
                btnCard.MouseDown += CardButton_MouseDown;
                pnlPlayerHand.Controls.Add(btnCard);
            }
        }

        private void CardButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (!battleManager.IsPlayable)
                return;
            if (gameManager.CurrentTurn != inputController.MyPlayerType)
                return;
            if (e.Button == MouseButtons.Left)
            {
                originalCardButton = sender as Button;
                ICard clickedCard = (ICard)originalCardButton.Tag;

                inputController.OnCardClicked(clickedCard);

                ghostCard = new Button
                {
                    Width = originalCardButton.Width,
                    Height = originalCardButton.Height,
                    Text = originalCardButton.Text,
                    Font = originalCardButton.Font,
                    BackColor = originalCardButton.BackColor,
                    FlatStyle = FlatStyle.Flat
                };

                this.Controls.Add(ghostCard);
                ghostCard.BringToFront();
                originalCardButton.Visible = false;

                ghostCard.MouseMove += GhostCard_MouseMove;
                ghostCard.MouseUp += GhostCard_MouseUp;
                ghostCard.Capture = true;
            }
        }

        private void GhostCard_MouseMove(object sender, MouseEventArgs e)
        {
            if (ghostCard != null && e.Button == MouseButtons.Left)
            {
                Point mousePos = this.PointToClient(Cursor.Position);
                ghostCard.Location = new Point(mousePos.X - (ghostCard.Width / 2), mousePos.Y - (ghostCard.Height / 2));
            }
        }

        private void GhostCard_MouseUp(object sender, MouseEventArgs e)
        {
            if (ghostCard != null)
            {
                ghostCard.Capture = false;
                Point clientPt = pnlBoard.PointToClient(Cursor.Position);

                if (boardView.TryConvertPixelToPosition(clientPt.X, clientPt.Y, out Position targetPos))
                {
                    inputController.OnBoardClicked(targetPos);
                    boardView.HandleMovementAnimation();
                    ShowGameEndMessageIfNeeded();
                }
                else
                {
                    inputController.CancelSelection();
                }

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
        }

        private void UdpProtocol_OnMessage(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UdpProtocol_OnMessage(msg)));
                return;
            }

            if (msg == "CONNECTED")
            {
                if (lblNetworkStatus != null)
                {
                    lblNetworkStatus.Text = "네트워크: 연결됨! 🟢";
                    lblNetworkStatus.ForeColor = Color.Green;
                }
                AddLog("✨ 네트워크가 연결되었습니다! 게임을 시작하세요.");
            }
            else if (msg.StartsWith("MOVE"))
            {
                string[] p = msg.Split(',');
                Position from = new Position(int.Parse(p[1]), int.Parse(p[2]));
                Position to = new Position(int.Parse(p[3]), int.Parse(p[4]));

                gameManager.IsLocalAction = false;
                gameManager.TryMoveOrAttack(from, to);
                gameManager.IsLocalAction = true;
            }
            else if (msg.StartsWith("CARD"))
            {
                string[] p = msg.Split(',');
                string cardName = p[1];
                Position target = new Position(int.Parse(p[2]), int.Parse(p[3]));

                ICard cardToUse = gameManager.State.Hands[gameManager.CurrentTurn].FirstOrDefault(c => c.Name == cardName);
                if (cardToUse != null)
                {
                    gameManager.IsLocalAction = false;
                    gameManager.TryUseCard(cardToUse, target);
                    gameManager.IsLocalAction = true;
                }
                RefreshBoard();
            }
            else if (msg == "PASS")
            {
                gameManager.IsLocalAction = false;
                gameManager.PassTurn();
                gameManager.IsLocalAction = true;
            }
            else if (msg == "SURRENDER")
            {
                gameManager.State.IsGameOver = true;
                gameManager.State.Winner = inputController.MyPlayerType;

                RefreshBoard();
                ShowGameEndMessageIfNeeded();
            }
        }

        private void ShowGameEndMessageIfNeeded()
        {
            if (gameEndMessageShown || !gameManager.State.IsGameOver) return;
            string message = gameManager.State.Winner.HasValue ? $"{gameManager.State.Winner.Value} 승리!" : "게임이 종료되었습니다.";
            logbox.Items.Add(message);
            gameEndMessageShown = true;
            MessageBox.Show(message, "게임 종료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddLog(string message)
        {
            logbox.Items.Add(message);
            if (logbox.Items.Count > 0) logbox.TopIndex = logbox.Items.Count - 1;
        }

        /* ==================== 메인 폼 내의 기본 윈도우 컨트롤 템플릿에 관련된 코드 ==================== */
        /* ==================== 메인 폼 내의 기본 윈도우 컨트롤 템플릿에 관련된 코드 ==================== */
        /* ==================== 메인 폼 내의 기본 윈도우 컨트롤 템플릿에 관련된 코드 ==================== */
        /* ==================== 메인 폼 내의 기본 윈도우 컨트롤 템플릿에 관련된 코드 ==================== */
        /* ==================== 메인 폼 내의 기본 윈도우 컨트롤 템플릿에 관련된 코드 ==================== */

        private void InitializeComponent()
        {
            this.pnlPlayerHand = new System.Windows.Forms.Panel();
            this.pnlPlayerDeck = new System.Windows.Forms.Panel();
            this.pnlBoard = new System.Windows.Forms.Panel();
            this.pnlDrawArea = new System.Windows.Forms.Panel();
            this.pnlPlayArea = new System.Windows.Forms.Panel();
            this.logbox = new System.Windows.Forms.ListBox();
            this.pnlOpponentDeck = new System.Windows.Forms.Panel();
            this.pnlOpponentHand = new System.Windows.Forms.Panel();
            this.lblNetworkStatus = new System.Windows.Forms.Label();
            this.btnPassTurn = new System.Windows.Forms.Button();

            this.pnlPlayerHand.SuspendLayout();
            this.pnlOpponentHand.SuspendLayout();
            this.SuspendLayout();

            // pnlPlayerHand
            this.pnlPlayerHand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPlayerHand.Controls.Add(this.pnlPlayerDeck);
            this.pnlPlayerHand.Location = new System.Drawing.Point(829, 468);
            this.pnlPlayerHand.Name = "pnlPlayerHand";
            this.pnlPlayerHand.Size = new System.Drawing.Size(590, 280);
            this.pnlPlayerHand.TabIndex = 13;

            // lblNetworkStatus
            this.lblNetworkStatus.AutoSize = true;
            this.lblNetworkStatus.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblNetworkStatus.ForeColor = System.Drawing.Color.White;
            this.lblNetworkStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblNetworkStatus.Location = new System.Drawing.Point(829, 230);
            this.lblNetworkStatus.Name = "lblNetworkStatus";
            this.lblNetworkStatus.Size = new System.Drawing.Size(150, 20);
            this.lblNetworkStatus.Text = "네트워크: 오프라인";

            // pnlPlayerDeck
            this.pnlPlayerDeck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPlayerDeck.Location = new System.Drawing.Point(465, 95);
            this.pnlPlayerDeck.Name = "pnlPlayerDeck";
            this.pnlPlayerDeck.Size = new System.Drawing.Size(120, 180);
            this.pnlPlayerDeck.TabIndex = 7;

            // pnlBoard
            this.pnlBoard.Location = new System.Drawing.Point(79, 58);
            this.pnlBoard.Name = "pnlBoard";
            this.pnlBoard.Size = new System.Drawing.Size(720, 720);
            this.pnlBoard.TabIndex = 10;

            // pnlDrawArea
            this.pnlDrawArea.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlDrawArea.Location = new System.Drawing.Point(1169, 268);
            this.pnlDrawArea.Name = "pnlDrawArea";
            this.pnlDrawArea.Size = new System.Drawing.Size(120, 180);
            this.pnlDrawArea.TabIndex = 14;

            // pnlPlayArea
            this.pnlPlayArea.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPlayArea.Location = new System.Drawing.Point(1295, 268);
            this.pnlPlayArea.Name = "pnlPlayArea";
            this.pnlPlayArea.Size = new System.Drawing.Size(120, 180);
            this.pnlPlayArea.TabIndex = 15;

            // logbox
            this.logbox.FormattingEnabled = true;
            this.logbox.ItemHeight = 12;
            this.logbox.Location = new System.Drawing.Point(829, 268);
            this.logbox.Name = "logbox";
            this.logbox.Size = new System.Drawing.Size(334, 184);
            this.logbox.TabIndex = 11;

            // pnlOpponentDeck
            this.pnlOpponentDeck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlOpponentDeck.Location = new System.Drawing.Point(465, 3);
            this.pnlOpponentDeck.Name = "pnlOpponentDeck";
            this.pnlOpponentDeck.Size = new System.Drawing.Size(120, 180);
            this.pnlOpponentDeck.TabIndex = 6;

            // pnlOpponentHand
            this.pnlOpponentHand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlOpponentHand.Controls.Add(this.pnlOpponentDeck);
            this.pnlOpponentHand.Location = new System.Drawing.Point(829, 58);
            this.pnlOpponentHand.Name = "pnlOpponentHand";
            this.pnlOpponentHand.Size = new System.Drawing.Size(590, 190);
            this.pnlOpponentHand.TabIndex = 12;

            // btnPassTurn
            this.btnPassTurn.Location = new System.Drawing.Point(1169, 468);
            this.btnPassTurn.Name = "btnPassTurn";
            this.btnPassTurn.Size = new System.Drawing.Size(246, 40);
            this.btnPassTurn.TabIndex = 16;
            this.btnPassTurn.Text = "턴 넘기기";
            this.btnPassTurn.UseVisualStyleBackColor = true;
            this.btnPassTurn.Click += new System.EventHandler(this.BtnPassTurn_Click);

            // MainForm
            this.ClientSize = new System.Drawing.Size(1584, 861);
            this.Controls.Add(this.btnPassTurn);
            this.Controls.Add(this.pnlPlayerHand);
            this.Controls.Add(this.pnlBoard);
            this.Controls.Add(this.pnlDrawArea);
            this.Controls.Add(this.pnlPlayArea);
            this.Controls.Add(this.logbox);
            this.Controls.Add(this.pnlOpponentHand);
            this.Name = "MainForm";
            this.pnlPlayerHand.ResumeLayout(false);
            this.pnlOpponentHand.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}