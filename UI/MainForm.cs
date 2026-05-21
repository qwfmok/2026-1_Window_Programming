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
        private UDPprotocol udpProtocol; //여기부터 아래 5개 pvp 구현때매 추가 했습니다 - 현빈
        private TextBox txtNetworkCode;
        private Button btnHost;
        private Button btnJoin;
        private Label lblNetworkStatus;

        public MainForm()
        {
            InitializeComponent();

            this.Width = 1600;
            this.Height = 900;

            gameManager = new GameManager();
            // 게임매니저의 방송을 UDP 통신으로 쏴버림
            gameManager.OnNetworkBroadcast += (msg) =>
            {
                if (udpProtocol != null && udpProtocol.IsConnected)
                    udpProtocol.Send(msg);
            };
            inputController = new InputController(gameManager, PlayerType.Player1);
            inputController.OnLogMessage += (sender, msg) =>
            {
                AddLog(msg);
            };

            CreateBoard();
            RefreshBoard();
            RefreshHand();
            CreateNetworkUI(); // 새로 추가함
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
        //  내 손패(Hand) UI 띄우기 및 연결
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
        // ==========================================================
        // 로컬 PvP 네트워크 통신 UI 및 연결 로직
        // ==========================================================
        private void CreateNetworkUI()
        {
            // 접속 코드 입력 칸
            txtNetworkCode = new TextBox();
            txtNetworkCode.Location = new Point(780, 740); // 손패 패널 아래쪽 빈 공간으로 이동
            txtNetworkCode.Size = new Size(200, 30);
            txtNetworkCode.Font = new Font("맑은 고딕", 12f);
            txtNetworkCode.TextAlign = HorizontalAlignment.Center;

            // 방 만들기 버튼 (Player1)
            btnHost = new Button();
            btnHost.Location = new Point(990, 735);
            btnHost.Size = new Size(130, 40);
            btnHost.Text = "방 만들기(Host)";
            btnHost.Click += BtnHost_Click;

            // 참여하기 버튼 (Player2)
            btnJoin = new Button();
            btnJoin.Location = new Point(1130, 735);
            btnJoin.Size = new Size(130, 40);
            btnJoin.Text = "참여하기(Guest)";
            btnJoin.Click += BtnJoin_Click;

            // 연결 상태 표시 라벨
            lblNetworkStatus = new Label();
            lblNetworkStatus.Location = new Point(780, 780);
            lblNetworkStatus.Size = new Size(400, 30);
            lblNetworkStatus.Font = new Font("맑은 고딕", 11f, FontStyle.Bold);
            lblNetworkStatus.Text = "네트워크: 오프라인";

            // 폼 화면에 추가
            this.Controls.Add(txtNetworkCode);
            this.Controls.Add(btnHost);
            this.Controls.Add(btnJoin);
            this.Controls.Add(lblNetworkStatus);

            // 🌟 패널 뒤에 숨지 않도록 무조건 맨 앞으로 가져오기!
            txtNetworkCode.BringToFront();
            btnHost.BringToFront();
            btnJoin.BringToFront();
            lblNetworkStatus.BringToFront();
        }

        private void BtnHost_Click(object sender, EventArgs e)
        {
            udpProtocol = new UDPprotocol();
            udpProtocol.OnMessage += UdpProtocol_OnMessage;

            // 방을 파고 랜덤 코드를 받아옴
            string code = udpProtocol.Starthostip();
            txtNetworkCode.Text = code;
            lblNetworkStatus.Text = "호스트 대기중... (코드 전달)";
            AddLog($"방을 만들었습니다! 상대에게 [{code}] 코드를 알려주세요.");

            // 방장은 무조건 Player1 (아래쪽)
            inputController.MyPlayerType = PlayerType.Player1;
            btnHost.Enabled = false;
            btnJoin.Enabled = false;
        }

        private void BtnJoin_Click(object sender, EventArgs e)
        {
            string code = txtNetworkCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("친구에게 받은 접속 코드를 입력해주세요!");
                return;
            }

            udpProtocol = new UDPprotocol();
            udpProtocol.OnMessage += UdpProtocol_OnMessage;

            // 코드를 가지고 방에 접속
            udpProtocol.Joinguestip(code);
            lblNetworkStatus.Text = "서버 접속 시도중...";
            AddLog("상대방의 방에 접속을 시도합니다.");

            // 참가자는 무조건 Player2 (위쪽)
            inputController.MyPlayerType = PlayerType.Player2;
            btnHost.Enabled = false;
            btnJoin.Enabled = false;
        }

        // 통신선(UDP)을 통해 메시지가 날아왔을 때 실행되는 함수
        private void UdpProtocol_OnMessage(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UdpProtocol_OnMessage(msg)));
                return;
            }

            if (msg == "CONNECTED")
            {
                lblNetworkStatus.Text = "네트워크: 연결됨! 🟢";
                lblNetworkStatus.ForeColor = Color.Green;
                AddLog("✨ 네트워크가 연결되었습니다! 게임을 시작하세요.");
            }
            else if (msg.StartsWith("MOVE"))
            {
                // 암호 해독: MOVE,시작Row,시작Col,도착Row,도착Col <-이걸 꼭 틀리지 않게 조심하세요들!!
                string[] p = msg.Split(',');
                Position from = new Position(int.Parse(p[1]), int.Parse(p[2]));
                Position to = new Position(int.Parse(p[3]), int.Parse(p[4]));

                // 상대방이 조종하는 거니까 IsLocalAction을 잠깐 끄고 움직임
                gameManager.IsLocalAction = false;
                gameManager.TryMoveOrAttack(from, to);
                gameManager.IsLocalAction = true;

                RefreshBoard();
                ShowGameEndMessageIfNeeded();
            }
            else if (msg.StartsWith("CARD"))
            {
                // 암호 해독: CARD,카드이름,타겟Row,타겟Col
                string[] p = msg.Split(',');
                string cardName = p[1];
                Position target = new Position(int.Parse(p[2]), int.Parse(p[3]));

                // 내 화면의 상대방 손패에서 해당 이름의 카드를 찾아서 씀
                ICard cardToUse = gameManager.State.Hands[gameManager.CurrentTurn].FirstOrDefault(c => c.Name == cardName);
                if (cardToUse != null)
                {
                    gameManager.IsLocalAction = false;
                    gameManager.TryUseCard(cardToUse, target);
                    gameManager.IsLocalAction = true;
                }
                RefreshBoard();
                RefreshHand();
            }
            else if (msg == "PASS")
            {
                gameManager.IsLocalAction = false;
                gameManager.PassTurn();
                gameManager.IsLocalAction = true;

                RefreshBoard();
                RefreshHand();
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