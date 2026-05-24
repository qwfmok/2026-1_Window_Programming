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
        private Panel pnlBoard; // 중앙 패널보드 여기에 보드들어감
        private Panel pnlPlayArea;
        private ListBox logbox;
        private Panel pnlOpponentDeck;
        private Panel pnlOpponentHand;
        private Button btnPassTurn;
        private Label lblCardDescription;

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

        // 어디서든 MainForm에 접근할 수 있게 해주는 static 변수 선언
        public static MainForm Instance;

        // 진입점

        public MainForm(UDPprotocol connectedUdp, PlayerType assignedPlayerType, int seed)
        {
            InitializeComponent();

            this.Width = 1600;
            this.Height = 900; // MainForm(이하 메인폼)의 최초 크기 정의
            this.pnlBoard.Size = new Size(720, 720); // 패널보드의 최초 크기 정의

            // 폼이 생성될 때 자기 자신을 Instance 변수에 등록
            Instance = this;

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

            this.gameManager = new GameManager(seed);
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
            this.boardView.MyPlayerType = assignedPlayerType; // 1p 2p 헷갈려서 넣음
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

            pnlBoard.MouseMove += PnlBoard_MouseMove;
            pnlBoard.MouseLeave += PnlBoard_MouseLeave;

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

                UpdateBoardHighlights(position);
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
            // 카드 컨트롤들 안전하게 지우기
            for (int i = pnlPlayerHand.Controls.Count - 1; i >= 0; i--)
            {
                if (pnlPlayerHand.Controls[i] != pnlPlayerDeck)
                    pnlPlayerHand.Controls.RemoveAt(i);
            }

            for (int i = pnlOpponentHand.Controls.Count - 1; i >= 0; i--)
            {
                if (pnlOpponentHand.Controls[i] != pnlOpponentDeck)
                    pnlOpponentHand.Controls.RemoveAt(i);
            }

            pnlPlayArea.Controls.Clear();

            int cardWidth = 80;
            int cardHeight = 120;
            int spacingX = 10;
            int spacingY = 10;
            int startX = 10;
            int startY = 10;
            int maxColumn = 4; // 한 줄에 4장

            PlayerType myType = inputController.MyPlayerType;
            PlayerType oppType = (myType == PlayerType.Player1) ? PlayerType.Player2 : PlayerType.Player1;

            // 내 손패 그리기: 4장씩 두 줄
            for (int i = 0; i < gameManager.State.Hands[myType].Count; i++)
            {
                ICard card = gameManager.State.Hands[myType][i];

                int col = i % maxColumn;
                int row = i / maxColumn;

                Button btnCard = new Button
                {
                    Width = cardWidth,
                    Height = cardHeight,
                    Left = startX + (cardWidth + spacingX) * col,
                    Top = startY + (cardHeight + spacingY) * row,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.LightGoldenrodYellow,
                    Font = new Font("맑은 고딕", 10, FontStyle.Bold),
                    Text = card.Name,
                    Tag = card
                };

                btnCard.MouseDown += CardButton_MouseDown;
                btnCard.MouseEnter += CardButton_MouseEnter;
                btnCard.Click += CardButton_Click;

                pnlPlayerHand.Controls.Add(btnCard);
            }

            // 상대 손패 그리기: 한 줄로 표시, 공간 안에서 잘리지 않게 표시
            int opponentCardCount = gameManager.State.Hands[oppType].Count;

            string cardBackPath = Path.Combine(Application.StartupPath, "Assets", "card_back.png");
            Image cardBackImg = null; // 카드 이미지 씌우는 변수 초기화 후 이미지 호출 1번만 해서 접근최소화
            if (File.Exists(cardBackPath))
            {
                cardBackImg = Image.FromFile(cardBackPath);
            }

            int opponentCardWidth = 70;
            int opponentCardHeight = 100;
            int opponentStartX = 10;
            int opponentStartY = 15;

            int availableWidth = pnlOpponentHand.Width - 20;

            int opponentSpacing;

            if (opponentCardCount <= 1)
            {
                opponentSpacing = 0;
            }
            else
            {
                opponentSpacing = (availableWidth - opponentCardWidth) / (opponentCardCount - 1);

                if (opponentSpacing > 85)
                    opponentSpacing = 85;

                if (opponentSpacing < 45)
                    opponentSpacing = 45;
            }

            for (int i = 0; i < opponentCardCount; i++)
            {
                Button btnOppCard = new Button
                {
                    Width = opponentCardWidth,
                    Height = opponentCardHeight,
                    Left = opponentStartX + opponentSpacing * i,
                    Top = opponentStartY,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.SlateGray,
                    Text = "CARD",
                    Enabled = false
                };
                if (cardBackImg != null)
                {
                    btnOppCard.BackgroundImage = cardBackImg;
                    btnOppCard.BackgroundImageLayout = ImageLayout.Stretch;
                    btnOppCard.Text = "";
                }
                else
                {
                    btnOppCard.Text = "CARD"; // 텍스트 제거 및 예외 처리
                }

                pnlOpponentHand.Controls.Add(btnOppCard);
            }

            pnlPlayerDeck.BringToFront();

            // 공용 덱 표시
            Button btnSharedDeck = new Button
            {
                Width = pnlPlayArea.Width - 10,
                Height = pnlPlayArea.Height - 10,
                Left = 5,
                Top = 5,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.SaddleBrown,
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 12, FontStyle.Bold),
                Text = $"덱\n{gameManager.State.SharedDeck.Count}장",
                Enabled = false
            };

            if (cardBackImg != null)
            {
                btnSharedDeck.BackgroundImage = cardBackImg;
                btnSharedDeck.BackgroundImageLayout = ImageLayout.Stretch;
            }

            pnlPlayArea.Controls.Add(btnSharedDeck);
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
                // 현재 마우스가 폼 전체에서 어디에 있는지 좌표 확인
                Point formPt = this.PointToClient(Cursor.Position);

                // 현재 마우스가 체스판(pnlBoard) 안에서 어디에 있는지 좌표 확인
                Point boardPt = pnlBoard.PointToClient(Cursor.Position);

                // [핵심 추가] 카드를 드롭한 위치가 '손패(pnlPlayerHand) 영역' 안쪽인가?
                bool isDroppedInHand = pnlPlayerHand.Bounds.Contains(formPt);

                if (isDroppedInHand)
                {
                    // 손패에 다시 내려놓았으므로 '사용 취소' 처리!
                    inputController.CancelSelection();
                    AddLog("카드 사용을 취소했습니다.");
                }
                else if (boardView.TryConvertPixelToPosition(boardPt.X, boardPt.Y, out Position targetPos))
                {
                    // 체스판 위에 내려놓았을 때 (기존 타겟팅 로직)
                    inputController.OnBoardClicked(targetPos);
                    boardView.HandleMovementAnimation();
                    ShowGameEndMessageIfNeeded();
                    RefreshHand();
                }
                else
                {
                    // 체스판 밖 & 손패 밖 (진짜 허공)에 던졌을 때 -> 즉시 발동!
                    ICard selectedCard = inputController.SelectedCard;

                    if (selectedCard != null &&
                       (selectedCard.Type == CardType.ActiveSkill || selectedCard.Type == CardType.Trap))
                    {
                        string errorMsg;
                        bool success = gameManager.TryUseCard(selectedCard, new Position(0, 0), out errorMsg);

                        if (!success)
                            AddLog($"[실패] {errorMsg}");
                        else
                            RefreshHand(); // 발동 성공 시 UI 갱신
                    }

                    inputController.CancelSelection(); // 발동 시도 후엔 무조건 빈손으로
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
                this.BeginInvoke(new Action(() => UdpProtocol_OnMessage(msg)));
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
                gameManager.TryMoveOrAttack(from, to, out _);
                gameManager.IsLocalAction = true;

                // 🌟 추가됨: 상대방 행동 로그 출력
                AddLog($"[네트워크] 상대방이 ({from.Row}, {from.Col})에서 ({to.Row}, {to.Col})(으)로 기물을 이동했습니다.");

                // 🌟 핵심: 내부 이동 처리 후 멈춰있던 애니메이션과 화면을 강제로 갱신!
                boardView.HandleMovementAnimation();
                RefreshBoard();
                RefreshHand();
                ShowGameEndMessageIfNeeded();
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
                    gameManager.TryUseCard(cardToUse, target, out _);
                    gameManager.IsLocalAction = true;

                    // 상대방 카드 로그 출력
                    AddLog($"[네트워크] 상대방이 '{cardName}' 카드를 사용했습니다!");
                }
                else
                {
                    // 덱이 엇갈려서 카드를 못 찾았을 때 확실하게 알려주는 에러 로그
                    AddLog($"[동기화 오류] 상대방이 '{cardName}'을(를) 썼지만, 내 화면의 상대 손패에는 그 카드가 없습니다! ");
                }

                // 🌟 핵심: 화면 및 애니메이션 갱신
                boardView.HandleMovementAnimation();
                RefreshBoard();
                RefreshHand();
                ShowGameEndMessageIfNeeded();
            }
            else if (msg == "PASS")
            {
                gameManager.IsLocalAction = false;
                gameManager.PassTurn();
                gameManager.IsLocalAction = true;

                AddLog("[네트워크] 상대방이 턴을 넘겼습니다.");

                RefreshBoard();
                RefreshHand();
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
            this.Close();
        }

        public void AddLog(string message)
        {
            logbox.Items.Add(message);
            if (logbox.Items.Count > 0) logbox.TopIndex = logbox.Items.Count - 1;
        }

        // 마우스 이전 위치 기억용
        private Position? lastHoveredPos = null;

        // 마우스가 체스판 위를 돌아다닐 때 매 프레임 실행됨
        private void PnlBoard_MouseMove(object sender, MouseEventArgs e)
        {
            if (!battleManager.IsPlayable || gameManager.State.IsGameOver) return;

            if (boardView.TryConvertPixelToPosition(e.X, e.Y, out Position position))
            {
                // 같은 칸 위를 맴돌 때는 계산 낭비 방지
                if (lastHoveredPos.HasValue && lastHoveredPos.Value.Equals(position)) return;

                lastHoveredPos = position;
                boardView.HoveredCell = position;
                UpdateBoardHighlights(position);
                pnlBoard.Invalidate();
            }
        }

        // 마우스가 체스판 밖으로 나갔을 때
        private void PnlBoard_MouseLeave(object sender, EventArgs e)
        {
            lastHoveredPos = null;
            boardView.HoveredCell = null;

            // 기물을 들고 있는(선택한) 상태가 아니라면 불빛 끄기
            if (inputController.CurrentState != InputState.PieceSelected)
            {
                boardView.MoveHighlights.Clear();
                boardView.AttackHighlights.Clear();
            }
            pnlBoard.Invalidate();
        }

        // 🌟 갈 수 있는 곳을 계산해서 넘겨주는 핵심 마법 함수
        private void UpdateBoardHighlights(Position hoverPos)
        {
            IPiece targetPiece = null;

            // 1. 이미 내 기물을 클릭해서(들고) 있다면, 그 기물 기준으로 고정
            if (inputController.CurrentState == InputState.PieceSelected && inputController.SelectedPosition.HasValue)
            {
                targetPiece = gameManager.State.GetPieceAt(inputController.SelectedPosition.Value);
            }
            // 2. 빈손(Idle)일 때 내 턴이고 내 기물에 마우스를 올렸다면 그 기물 정보 가져오기
            else if (inputController.CurrentState == InputState.Idle && gameManager.CurrentTurn == inputController.MyPlayerType)
            {
                IPiece hoverPiece = gameManager.State.GetPieceAt(hoverPos);
                if (hoverPiece != null && hoverPiece.Owner == inputController.MyPlayerType)
                    targetPiece = hoverPiece;
            }

            // 하이라이트 계산
            if (targetPiece != null)
            {
                // 이동 가능 칸 계산
                var movables = targetPiece.GetMovablePositions(gameManager.State);
                // 공격 가능 칸 계산
                var attackables = targetPiece.GetAttackablePositions(gameManager.State);

                // 화면에 뿌려줄 불빛 리스트 정리 (적 기물이 있는 곳은 빨간불, 빈칸은 초록불)
                boardView.AttackHighlights = attackables
                    .Where(p => gameManager.State.GetPieceAt(p) != null && gameManager.State.GetPieceAt(p).Owner != targetPiece.Owner).ToList();

                boardView.MoveHighlights = movables
                    .Where(p => !boardView.AttackHighlights.Contains(p)).ToList(); // 빨간불 들어간 곳은 초록불에서 제외
            }
            else
            {
                boardView.MoveHighlights.Clear();
                boardView.AttackHighlights.Clear();
            }
        }

        private void InitializeComponent()
        {
            this.pnlPlayerHand = new System.Windows.Forms.Panel();
            this.pnlPlayerDeck = new System.Windows.Forms.Panel();
            this.lblCardDescription = new System.Windows.Forms.Label();
            this.pnlBoard = new System.Windows.Forms.Panel();
            this.pnlPlayArea = new System.Windows.Forms.Panel();
            this.logbox = new System.Windows.Forms.ListBox();
            this.pnlOpponentDeck = new System.Windows.Forms.Panel();
            this.pnlOpponentHand = new System.Windows.Forms.Panel();
            this.lblNetworkStatus = new System.Windows.Forms.Label();
            this.btnPassTurn = new System.Windows.Forms.Button();
            this.pnlPlayerHand.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlPlayerHand
            // 
            this.pnlPlayerHand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPlayerHand.Controls.Add(this.pnlPlayerDeck);
            this.pnlPlayerHand.Location = new System.Drawing.Point(829, 498);
            this.pnlPlayerHand.Name = "pnlPlayerHand";
            this.pnlPlayerHand.Size = new System.Drawing.Size(590, 280);
            this.pnlPlayerHand.TabIndex = 13;
            // 
            // pnlPlayerDeck
            // 
            this.pnlPlayerDeck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPlayerDeck.Location = new System.Drawing.Point(381, 3);
            this.pnlPlayerDeck.Name = "pnlPlayerDeck";
            this.pnlPlayerDeck.Size = new System.Drawing.Size(204, 272);
            this.pnlPlayerDeck.TabIndex = 7;
            // 
            // pnlBoard
            // 
            this.pnlBoard.Location = new System.Drawing.Point(79, 58);
            this.pnlBoard.Name = "pnlBoard";
            this.pnlBoard.Size = new System.Drawing.Size(720, 720);
            this.pnlBoard.TabIndex = 10;
            // 
            // pnlPlayArea
            // 
            this.pnlPlayArea.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPlayArea.Location = new System.Drawing.Point(1295, 211);
            this.pnlPlayArea.Name = "pnlPlayArea";
            this.pnlPlayArea.Size = new System.Drawing.Size(120, 180);
            this.pnlPlayArea.TabIndex = 15;
            // 
            // logbox
            // 
            this.logbox.FormattingEnabled = true;
            this.logbox.ItemHeight = 15;
            this.logbox.Location = new System.Drawing.Point(829, 207);
            this.logbox.Name = "logbox";
            this.logbox.Size = new System.Drawing.Size(460, 184);
            this.logbox.TabIndex = 11;
            // 
            // pnlOpponentDeck
            // 
            this.pnlOpponentDeck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlOpponentDeck.Location = new System.Drawing.Point(465, 3);
            this.pnlOpponentDeck.Name = "pnlOpponentDeck";
            this.pnlOpponentDeck.Size = new System.Drawing.Size(120, 180);
            this.pnlOpponentDeck.TabIndex = 6;
            // 
            // pnlOpponentHand
            // 
            this.pnlOpponentHand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlOpponentHand.Location = new System.Drawing.Point(829, 58);
            this.pnlOpponentHand.Name = "pnlOpponentHand";
            this.pnlOpponentHand.Size = new System.Drawing.Size(590, 143);
            this.pnlOpponentHand.TabIndex = 12;
            // 
            // lblNetworkStatus
            // 
            this.lblNetworkStatus.AutoSize = true;
            this.lblNetworkStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblNetworkStatus.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblNetworkStatus.ForeColor = System.Drawing.Color.White;
            this.lblNetworkStatus.Location = new System.Drawing.Point(829, 230);
            this.lblNetworkStatus.Name = "lblNetworkStatus";
            this.lblNetworkStatus.Size = new System.Drawing.Size(150, 20);
            this.lblNetworkStatus.TabIndex = 0;
            this.lblNetworkStatus.Text = "네트워크: 오프라인";
            // 
            // btnPassTurn
            // 
            this.btnPassTurn.Location = new System.Drawing.Point(829, 452);
            this.btnPassTurn.Name = "btnPassTurn";
            this.btnPassTurn.Size = new System.Drawing.Size(246, 40);
            this.btnPassTurn.TabIndex = 16;
            this.btnPassTurn.Text = "턴 넘기기";
            this.btnPassTurn.UseVisualStyleBackColor = true;
            this.btnPassTurn.Click += new System.EventHandler(this.BtnPassTurn_Click);
            // 
            // lblCardDescription
            // 
            this.lblCardDescription.AutoSize = false;
            this.lblCardDescription.Location = new System.Drawing.Point(10, 10);
            this.lblCardDescription.Name = "lblCardDescription";
            this.lblCardDescription.Size = new System.Drawing.Size(184, 252);
            this.lblCardDescription.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCardDescription.ForeColor = System.Drawing.Color.White;
            this.lblCardDescription.BackColor = System.Drawing.Color.Transparent;
            this.lblCardDescription.Text = "카드 설명";
            this.lblCardDescription.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.pnlPlayerDeck.Controls.Add(this.lblCardDescription);
            this.lblCardDescription.ForeColor = System.Drawing.Color.Black;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1584, 861);
            this.Controls.Add(this.btnPassTurn);
            this.Controls.Add(this.pnlPlayerHand);
            this.Controls.Add(this.pnlBoard);
            this.Controls.Add(this.pnlPlayArea);
            this.Controls.Add(this.logbox);
            this.Controls.Add(this.pnlOpponentHand);
            this.Name = "MainForm";
            this.Text = "Card Chess Game";
            this.pnlPlayerHand.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        // 폼이 닫힐 때(게임이 끝날 때) 무조건 실행되는 안전장치
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (udpProtocol != null)
            {
                // 상대방에게 "나 나간다!" 라고 알려주고 통신선을 끊음
                if (udpProtocol.IsConnected) udpProtocol.Send("SURRENDER");
                udpProtocol.Close();
            }
            base.OnFormClosed(e);
        }

        private void CardButton_MouseEnter(object sender, EventArgs e)
        {
            Button cardButton = sender as Button;

            if (cardButton == null)
                return;

            ICard card = cardButton.Tag as ICard;

            if (card == null)
                return;

            ShowCardDescription(card);
        }

        private void CardButton_Click(object sender, EventArgs e)
        {
            Button cardButton = sender as Button;

            if (cardButton == null)
                return;

            ICard card = cardButton.Tag as ICard;

            if (card == null)
                return;

            ShowCardDescription(card);
        }

        private void ShowCardDescription(ICard card)
        {
            lblCardDescription.Text =
                $"[{card.Name}]\n\n" +
                $"종류: {card.Type}\n\n" +
                $"{card.Description}";
        }
    }
}