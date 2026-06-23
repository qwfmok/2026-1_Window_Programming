using CardChess.Cards;
using CardChess.Core;
using CardChess.Input;
using CardChess.Menu;
using CardChess.Models;
using CardChess.Networking;
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
        private Panel pnlBoard;
        private Panel pnlPlayArea;
        private ListBox logbox;
        private Panel pnlOpponentDeck;
        private Panel pnlOpponentHand;
        private Button btnPassTurn;
        private Label lblCardDescription;
        // 채팅 입력창 변수
        private TextBox txtChatInput;

        // --- 드래그 앤 드롭 카드 분신 제어 ---
        private Button ghostCard = null;
        private Button originalCardButton = null;
        private Point cardMouseDownScreenPoint;
        private bool isCardDragPending;
        private bool gameEndMessageShown = false;
        private bool surrenderSent = false;

        // --- 메인 메뉴에서 생성한 SignalR 연결을 게임 화면에서도 그대로 사용 ---
        private SignalRProtocol networkProtocol;
        private Label lblNetworkStatus;

        // --- 엔진 타이머 ---
        private System.Windows.Forms.Timer gameLoopTimer;

        // --- 현재 플레이어 상태 나타내는 용도 ---
        private Image playerStateImg;

        // --- 카드 텍스트 인식시키는 변수부 ---
        private readonly Dictionary<string, string> cardImageMap = new Dictionary<string, string>
        {
            { "기사 진화", "card_1_knightevo" },
            { "룩 진화", "card_1_rookevo" },
            { "비숍 진화", "card_1_bishopevo" },
            { "두장 뽑기", "card_1_bottle" },
            { "손패 교환", "card_1_handdeath" },
            { "카드 뺏기", "card_1_thief" },
            { "시간 왜곡", "card_1_timewalk" },
            { "랜덤 시전", "card_1_auction" },
            { "방벽 건설", "card_1_wallconst" },
            { "증원", "card_1_reinforce" },
            { "봉인", "card_1_zhonya" },
            { "위치 교환", "card_1_change" },
            { "부활", "card_1_revive" },
            { "랜덤 진화", "card_1_pandora" },
            { "기물 뺏기", "card_1_mindcontrol" },
            { "복제", "card_1_kagebunshin" },
            { "랜덤 방어", "card_1_gamble" }

            // 이후 신규 카드를 추가할 때는 { "스킬 이름", "파일 이름(확장자 불필요)" }, 로 개행하면 됨 + 마지막 행은 ,를 붙이지 않는다
        };

        // --- 게임 플레이 화면 UI 이미지 변수부 ---
        private Image imgCardBack;
        private Image imgPlayareabg;
        private Button btnSettings;
        private readonly ToolTip chatToolTip = new ToolTip();

        private const int MaxChatLength = 200;
        private const int DesignClientWidth = 1584;
        private const int DesignClientHeight = 861;

        // 어디서든 MainForm에 접근할 수 있게 해주는 static 변수 선언
        public static MainForm Instance;

        // 1. 생성자 진입점
        public MainForm(SignalRProtocol connectedNetwork, PlayerType assignedPlayerType, int seed)
        {
            InitializeComponent();


            // --- 화면 버퍼 제거부 ---
            Action<Control> enableDoubleBuffer = (control) =>
            {
                typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                               ?.SetValue(control, true);
            };

            enableDoubleBuffer(pnlPlayerHand);
            enableDoubleBuffer(pnlOpponentHand);
            enableDoubleBuffer(pnlPlayArea);
            // --- 화면 버퍼 제거부 ---

            this.AutoScaleMode = AutoScaleMode.None;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(700, 430);
            this.ClientSize = ResponsiveLayout.GetFittedClientSize(
                this,
                new Size(DesignClientWidth, DesignClientHeight));
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Resize += MainForm_Resize;

            // 폼이 생성될 때 자기 자신을 Instance 변수에 등록
            Instance = this;

            // 1-1. 백그라운드 이미지
            // 메인폼 배경 이미지 설정 경로는 동일하게 바이너리/디버그의 에셋 폴더
            string bgPath = Path.Combine(Application.StartupPath, "Assets", "bg.png");
            if (File.Exists(bgPath))
            {
                this.BackgroundImage = Image.FromFile(bgPath);
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            this.DoubleBuffered = true; // 버퍼링 깜빡이는거 방지
            string playAreaBgPath = Path.Combine(Application.StartupPath, "Assets", "playarea_bg.png");
            if (File.Exists(playAreaBgPath))
            {
                imgPlayareabg = Image.FromFile(playAreaBgPath);
            }

            RelayoutUI();

            // 1-2. 턴 나타내는 버튼
            string imgPath = Path.Combine(Application.StartupPath, "Assets", "button_long_player_state.png");
            if (File.Exists(imgPath))
            {
                playerStateImg = Image.FromFile(imgPath);
            }

            // 1-3. Form1에서 연결한 SignalR 통신을 MainForm까지 유지
            this.networkProtocol = connectedNetwork;
            if (this.networkProtocol != null)
            {
                this.networkProtocol.OnMessage += NetworkProtocol_OnMessage;
            }

            this.gameManager = new GameManager(seed);
            this.inputController = new InputController(this.gameManager, assignedPlayerType);
            this.inputController.MyPlayerType = assignedPlayerType;

            this.gameManager.OnNetworkBroadcast += (msg) =>
            {
                if (networkProtocol != null && networkProtocol.IsConnected)
                    networkProtocol.Send(msg);
                
                if (msg.StartsWith("CARD")) // 카드 발동 시 진입하는 블록
                {
                    string[] p = msg.Split(',');
                    string cardName = p.Length > 1 ? p[1] : "";

                    if (cardName == "방벽 건설")
                    {
                        SoundsManager.Play("Card_wall");
                    } // 벽 놓을 때 쿵 떨어지는 사운드 재생시키는 구간

                    else if (cardName == "시간 왜곡")
                    {
                        SoundsManager.Play("Card_timewalk");
                        boardView.TriggerClockEffect();
                    } // 윗줄이 사운드, 밑줄이 글로벌 이펙트

                    else
                    {
                        SoundsManager.Play("Card_effect");
                    } // 그 외 일반 카드는 일반 효과음 재생
                }
            };
            this.inputController.OnLogMessage += (sender, msg) => { AddLog(msg); };

            // 1-4. 보드 뷰 참조
            this.boardView = new BoardView(pnlBoard, gameManager);
            this.boardView.MyPlayerType = assignedPlayerType; // 1p 2p 헷갈려서 넣음
            InitCanvasBoardEvents();

            // 1-5. 프레임 연산 구동용 메인 루프 타이머 가동
            gameLoopTimer = new System.Windows.Forms.Timer();
            gameLoopTimer.Interval = 10;
            gameLoopTimer.Tick += GameLoopTimer_Tick;
            gameLoopTimer.Start();

            // 1-6. 배틀 매니저
            this.battleManager = new BattleManager(gameManager);
            this.battleManager.OnTurnChanged += BattleManager_OnTurnChanged;
            this.gameManager.OnTurnEndRequired += () => { battleManager.RequestTurnEnd(); };
            this.btnPassTurn.Click += BtnPassTurn_Click;


            // 1-7. 뒷면 이미지를 프로그램 켤 때 딱 1번만 안전하게 로드
            string cardBackPath = Path.Combine(Application.StartupPath, "Assets", "card_back.png");
            if (File.Exists(cardBackPath)) imgCardBack = Image.FromFile(cardBackPath);

            //UI들을 깔끔하게 강제 재배치하는 함수 호출
            RelayoutUI();

            //아까 BattleManager에 만든 알람을 듣고 카드를 바로 화면에 띄우는 기능
            this.battleManager.OnPhaseChanged += (phase) =>
            {
                if (phase == BattlePhase.Phase2_Play)
                {
                    this.BeginInvoke(new Action(() => {
                        RefreshHand();
                        RefreshBoard();
                    }));
                }
            };

            RefreshBoard();
            

            // 이게 있어야 첫 턴이라는 개념이 생김
            _ = this.battleManager.ProcessNextPhase();


            // 설정 에셋 이미지 버튼 추가
            btnSettings = new Button();
            btnSettings.Size = new Size(60, 59);    // 에셋 이미지 크기에 맞게 조절
            btnSettings.Location = new Point(10, 10); // 좌측 상단
            btnSettings.Cursor = Cursors.Hand;

            // 버튼 뼈대(테두리, 클릭 시 효과 등) 투명화
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnSettings.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnSettings.BackColor = Color.Transparent;

            // btn_settings.png 이미지 씌우기
            try
            {
                string settingsImgPath = Path.Combine(Application.StartupPath, "Assets", "btn_settings.png");
                if (File.Exists(settingsImgPath))
                {
                    btnSettings.BackgroundImage = Image.FromFile(settingsImgPath);
                    btnSettings.BackgroundImageLayout = ImageLayout.Zoom; // 비율 유지하며 꽉 채우기
                }
                else
                {
                    // 혹시라도 이미지를 못 찾을 경우를 대비한 텍스트 임시 출력
                    btnSettings.Text = "⚙️ 설정";
                    btnSettings.ForeColor = Color.White;
                }
            }
            catch { }

            // 버튼 클릭 시 로비용 설정창 활성화
            btnSettings.Click += (s, e) =>
            {
                CardChess.Menu.SoundsManager.Play("Menu_icon_select");
                using (CardChess.Menu.SettingsMenu settings = new CardChess.Menu.SettingsMenu(this, true, this.networkProtocol))
                {
                    settings.ShowDialog();
                }
            };

            this.Controls.Add(btnSettings);
            btnSettings.BringToFront(); // 다른 UI 요소에 가려지지 않도록 맨 앞으로 가져옴


            // 채팅창 세팅
            txtChatInput = new TextBox();
            txtChatInput.Font = new Font("맑은 고딕", 11f);
            txtChatInput.BackColor = Color.FromArgb(40, 40, 40);
            txtChatInput.ForeColor = Color.White;
            txtChatInput.BorderStyle = BorderStyle.FixedSingle;
            txtChatInput.AutoSize = false;
            txtChatInput.KeyDown += TxtChatInput_KeyDown; // 엔터키 이벤트 연결
            this.Controls.Add(txtChatInput);
            chatToolTip.SetToolTip(txtChatInput, "채팅 입력 중에는 숫자 카드 단축키가 꺼집니다. Esc를 누르면 보드 조작으로 돌아갑니다.");


            RelayoutUI();
        }

        // 2. 턴 체인지
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
                btnPassTurn.Text = "Player 1's TURN";
                btnPassTurn.ForeColor = Color.LightSkyBlue;
            }
            else
            {
                btnPassTurn.Text = "Player 2's TURN";
                btnPassTurn.ForeColor = Color.LightCoral;
            }

            RefreshBoard();
        }

        // 3. 보드 내 이벤트 핸들러
        private void InitCanvasBoardEvents()
        {
            pnlBoard.Controls.Clear();

            // 기존 Paint 이벤트가 중복 연결되는 것을 방지
            pnlBoard.Paint -= PnlBoard_Paint;
            pnlBoard.Paint += PnlBoard_Paint;

            pnlBoard.MouseClick -= PnlBoard_MouseClick;
            pnlBoard.MouseClick += PnlBoard_MouseClick;

            pnlBoard.MouseMove -= PnlBoard_MouseMove;
            pnlBoard.MouseMove += PnlBoard_MouseMove;

            pnlBoard.MouseLeave -= PnlBoard_MouseLeave;
            pnlBoard.MouseLeave += PnlBoard_MouseLeave;

            pnlBoard.AllowDrop = true;

            pnlBoard.DragEnter -= BoardPanel_DragEnter;
            pnlBoard.DragEnter += BoardPanel_DragEnter;

            pnlBoard.DragDrop -= BoardPanel_DragDrop;
            pnlBoard.DragDrop += BoardPanel_DragDrop;

            boardView.SyncPiecesWithBackend();
        }

        // 4. 체크
        private void PnlBoard_Paint(object sender, PaintEventArgs e)
        {
            boardView.DrawBoard(e.Graphics);

            // 체크를 건 기물 칸을 가장 마지막에 그려서 확실히 보이게 함
            DrawCheckWarning(e.Graphics);
        }

        private void DrawCheckWarning(Graphics g)
        {
            if (gameManager == null || gameManager.State == null)
                return;

            List<Position> checkingPieces = gameManager.GetCheckingPiecePositions();

            if (checkingPieces == null || checkingPieces.Count == 0)
                return;

            foreach (Position pos in checkingPieces)
            {
                DrawCheckingPieceCell(g, pos);
            }
        }

        private void DrawCheckingPieceCell(Graphics g, Position pos)
        {
            RectangleF cellRect = boardView.GetCellRectangle(pos);

            // 은은한 주황색 칸만 표시
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(110, Color.Orange)))
            {
                g.FillRectangle(brush, cellRect);
            }
        }

        private void GameLoopTimer_Tick(object sender, EventArgs e)
        {
            boardView.UpdateLoopTick();
            pnlBoard.Invalidate();
        }


        /// --- 보드 이벤트 처리기 ---

        // 보드 기물 마우스 클릭 이벤트 처리
        private void PnlBoard_MouseClick(object sender, MouseEventArgs e)
        {
            if (!battleManager.IsPlayable)
                return;

            if (!IsNetworkReady())
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
                Position? preSelectedPos = inputController.SelectedPosition;
                IPiece clickedPiece = gameManager.State.GetPieceAt(position);

                if (clickedPiece != null && boardView.PieceAnimations.ContainsKey(clickedPiece))
                {
                    boardView.PieceAnimations[clickedPiece].Onclick();
                    if (clickedPiece.Owner == inputController.MyPlayerType)
                    {
                        SoundsManager.Play("Piece_select"); // 여기서 플레이어의 기물 클릭 사운드 재생
                    }
                }

                inputController.OnBoardClicked(position);
                boardView.HandleMovementAnimation();

                if (preSelectedPos.HasValue && gameManager.State.GetPieceAt(preSelectedPos.Value) == null)
                {
                    SoundsManager.Play("Piece_attack"); // 기물 공격 사운드
                }

                UpdateBoardHighlights(position);

                // 체크 표시 포함해서 보드 강제 갱신
                RefreshBoard();
                pnlBoard.Invalidate();
                RefreshHand();

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

        // ====================================================================
        //  UI 강제 정렬 (항복 버튼 위치 고정 포함)
        // ====================================================================
        private void RelayoutUI()
        {
            float scaleX = ClientSize.Width / (float)DesignClientWidth;
            float scaleY = ClientSize.Height / (float)DesignClientHeight;
            Func<int, int> sx = value => Math.Max(1, (int)Math.Round(value * scaleX));
            Func<int, int> sy = value => Math.Max(1, (int)Math.Round(value * scaleY));

            pnlBoard.Location = new Point(sx(50), sy(50));
            int boardSize = Math.Min(sx(720), sy(720));
            pnlBoard.Size = new Size(boardSize, boardSize);

            lblNetworkStatus.Location = new Point(sx(800), sy(50));

            pnlOpponentHand.Location = new Point(sx(800), sy(80));
            pnlOpponentHand.Size = new Size(sx(580), sy(120));

            if (imgPlayareabg != null)
            {
                pnlOpponentHand.BackgroundImage = imgPlayareabg;
                pnlOpponentHand.BackgroundImageLayout = ImageLayout.Stretch;
            }

            logbox.Location = new Point(sx(800), sy(210));
            logbox.Size = new Size(sx(450), sy(130));
            logbox.BackColor = Color.FromArgb(40, 40, 40);
            logbox.ForeColor = Color.White;

            // 로그박스 마우스로 클릭해도 파랗게 선택되지 않도록
            logbox.SelectionMode = SelectionMode.None;

            // 로그박스 바로 밑에 채팅 입력창
            if (txtChatInput != null)
            {
                txtChatInput.Location = new Point(sx(800), sy(345));
                txtChatInput.Size = new Size(sx(450), sy(28));
                txtChatInput.BringToFront();
            }

            // 로그박스 바로 오른쪽에 공용 덱이 예쁘게 들어감
            pnlPlayArea.Location = new Point(sx(1260), sy(210));
            pnlPlayArea.Size = new Size(sx(120), sy(160));

            pnlPlayArea.BackgroundImage = null;
            pnlPlayArea.BackColor = Color.Transparent;
            pnlPlayArea.BorderStyle = BorderStyle.None;

            if (imgPlayareabg != null)
            {
                pnlPlayArea.BackgroundImage = imgPlayareabg;
                pnlPlayArea.BackgroundImageLayout = ImageLayout.Stretch;
            }

            pnlPlayArea.BackgroundImage = null;
            pnlPlayArea.BackColor = Color.Transparent;
            pnlPlayArea.BorderStyle = BorderStyle.None;

            // 턴 넘기기 버튼 -> 턴 상태 간판으로 수정
            btnPassTurn.Location = new Point(sx(800), sy(380));
            btnPassTurn.Size = new Size(sx(200), sy(40));
            btnPassTurn.Cursor = Cursors.Hand;
            btnPassTurn.FlatStyle = FlatStyle.Flat; // 테두리 제거
            btnPassTurn.FlatAppearance.BorderSize = 0; // 깔끔한 간판 디자인

            pnlPlayerHand.Location = new Point(sx(800), sy(430));
            pnlPlayerHand.Size = new Size(sx(580), sy(300));
            if (imgPlayareabg != null)
            {
                pnlPlayerHand.BackgroundImage = imgPlayareabg;
                pnlPlayerHand.BackgroundImageLayout = ImageLayout.Stretch;
            }

            // 카드 툴팁 텍스트 칸을 플레이어 핸드의 오른쪽 칸에 넣어서 가독성 향상
            pnlPlayerDeck.Parent = pnlPlayerHand;
            pnlPlayerDeck.Location = new Point(sx(360), sy(10));
            pnlPlayerDeck.Size = new Size(sx(210), sy(280));
            pnlPlayerDeck.BackColor = Color.FromArgb(200, 20, 20, 20);
            pnlPlayerDeck.BorderStyle = BorderStyle.FixedSingle;

            lblCardDescription.Parent = pnlPlayerDeck;
            lblCardDescription.Location = new Point(sx(5), sy(5));
            lblCardDescription.Size = new Size(
                Math.Max(1, pnlPlayerDeck.Width - sx(10)),
                Math.Max(1, pnlPlayerDeck.Height - sy(10)));
            lblCardDescription.BackColor = Color.Transparent;
            lblCardDescription.ForeColor = Color.White;

            // 기타 패널 숨기기
            if (pnlOpponentDeck != null) pnlOpponentDeck.Visible = false;

            if (btnSettings != null)
            {
                btnSettings.Location = new Point(sx(10), sy(10));
                btnSettings.Size = new Size(sx(60), sy(59));
            }

            pnlPlayerDeck.BringToFront();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            RelayoutUI();
            boardView?.CalculateBoardDimensions();
            if (gameManager != null && inputController != null)
            {
                RefreshHand();
                pnlBoard.Invalidate();
            }
        }

        private void RefreshHand()
        {
            // 1. 기존 컨트롤 안전하게 폭파 (메모리 릭 방지)
            for (int i = pnlPlayerHand.Controls.Count - 1; i >= 0; i--)
            {
                Control c = pnlPlayerHand.Controls[i];
                if (c != pnlPlayerDeck) // 설명창은 살려둠
                {
                    pnlPlayerHand.Controls.RemoveAt(i);
                    c.Dispose();
                }
            }
            for (int i = pnlOpponentHand.Controls.Count - 1; i >= 0; i--)
            {
                Control c = pnlOpponentHand.Controls[i];
                pnlOpponentHand.Controls.RemoveAt(i);
                c.Dispose();
            }
            for (int i = pnlPlayArea.Controls.Count - 1; i >= 0; i--)
            {
                Control c = pnlPlayArea.Controls[i];
                pnlPlayArea.Controls.RemoveAt(i);
                c.Dispose();
            }

            int cardWidth, cardHeight, spacingX, spacingY, startX, startY;
            GetPlayerCardLayout(out cardWidth, out cardHeight, out spacingX, out spacingY, out startX, out startY);
            PlayerType myType = inputController.MyPlayerType;
            PlayerType oppType = (myType == PlayerType.Player1) ? PlayerType.Player2 : PlayerType.Player1;

            // 2. 내 손패 그리기 (설명창을 침범하지 않게 좌측에 차곡차곡 쌓임)
            if (gameManager.State.Hands.ContainsKey(myType))
            {
                for (int i = 0; i < gameManager.State.Hands[myType].Count; i++)
                {
                    ICard card = gameManager.State.Hands[myType][i];
                    int col = i % 4; // 한 줄에 4장
                    int row = i / 4;

                    Button btnCard = new Button
                    {
                        Width = cardWidth,
                        Height = cardHeight,
                        Left = startX + (cardWidth + spacingX) * col,
                        Top = startY + (cardHeight + spacingY) * row,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(40, 40, 40),
                        Font = new Font(
                            "맑은 고딕",
                            Math.Max(7f, 10f * Math.Min(
                                ClientSize.Width / (float)DesignClientWidth,
                                ClientSize.Height / (float)DesignClientHeight)),
                            FontStyle.Bold),
                        ForeColor = Color.White,
                        Text = card.Name,
                        Tag = card,
                        Cursor = Cursors.Hand
                    };

                    string fileName = card.Name; // 딕셔너리 변수부에서 선언된 한글 이름을 파일명으로 치환하여 대입
                    if (cardImageMap.ContainsKey(card.Name))
                    {
                        fileName = cardImageMap[card.Name];
                    }

                    string cardImgPath = Path.Combine(Application.StartupPath, "Assets", $"{fileName}.png"); // 실제 파일명을 인식하여 이미지 출력
                    if (File.Exists(cardImgPath))
                    {
                        btnCard.BackgroundImage = Image.FromFile(cardImgPath);
                        btnCard.BackgroundImageLayout = ImageLayout.Stretch;
                    }

                    btnCard.MouseDown += CardButton_MouseDown;
                    btnCard.MouseMove += CardButton_MouseMove;
                    btnCard.MouseUp += CardButton_MouseUp;
                    btnCard.MouseEnter += CardButton_MouseEnter;
                    btnCard.Click += CardButton_Click;
                    pnlPlayerHand.Controls.Add(btnCard);
                }
            }

            // 3. 상대방 손패 그리기
            if (gameManager.State.Hands.ContainsKey(oppType))
            {
                int oppCount = gameManager.State.Hands[oppType].Count;
                int oppCardHeight = Math.Max(60, pnlOpponentHand.Height - 20);
                int oppCardWidth = Math.Max(42, (int)Math.Round(oppCardHeight * 0.7));
                int oppSpacing = oppCount > 1 ? (pnlOpponentHand.Width - 20 - oppCardWidth) / (oppCount - 1) : oppCardWidth + 15;
                if (oppSpacing > oppCardWidth + 15) oppSpacing = oppCardWidth + 15;
                if (oppSpacing < Math.Max(25, oppCardWidth / 2)) oppSpacing = Math.Max(25, oppCardWidth / 2);

                for (int i = 0; i < oppCount; i++)
                {
                    Button btnOppCard = new Button
                    {
                        Width = oppCardWidth,
                        Height = oppCardHeight,
                        Left = 10 + oppSpacing * i,
                        Top = 10,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.SlateGray,
                        Enabled = false
                    };
                    if (imgCardBack != null)
                    {
                        btnOppCard.BackgroundImage = imgCardBack;
                        btnOppCard.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                    else btnOppCard.Text = "CARD";

                    pnlOpponentHand.Controls.Add(btnOppCard);
                }
            }

            // 4. 공용 덱 표시
            Button btnSharedDeck = new Button
            {
                Width = pnlPlayArea.Width - 10,
                Height = pnlPlayArea.Height - 10,
                Left = 5,
                Top = 5,
                BackColor = Color.Transparent,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent },
                Font = new Font("맑은 고딕", 12, FontStyle.Bold),
                Text = $"공용 덱\n{gameManager.State.SharedDeck.Count}장",
                ForeColor = Color.White,
                Enabled = true
            };
            if (imgCardBack != null)
            {
                btnSharedDeck.BackgroundImage = imgCardBack;
                btnSharedDeck.BackgroundImageLayout = ImageLayout.Stretch;
            }
            pnlPlayArea.Controls.Add(btnSharedDeck);

            // 5. Z-Index 정리 (설명창이 카드 뒤에 숨지 않게 앞으로 당김)
            pnlPlayerDeck.BringToFront();
            HighlightSelectedCard();
        }

        private void CardButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (!IsNetworkReady())
                return;

            originalCardButton = sender as Button;
            if (originalCardButton == null || !(originalCardButton.Tag is ICard))
                return;

            ICard clickedCard = (ICard)originalCardButton.Tag;
            if (battleManager.IsPlayable && gameManager.CurrentTurn == inputController.MyPlayerType)
            {
                inputController.OnCardClicked(clickedCard);
                HighlightSelectedCard();
            }

            cardMouseDownScreenPoint = Cursor.Position;
            isCardDragPending = true;
            originalCardButton.Capture = true;
        }

        private void CardButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isCardDragPending || e.Button != MouseButtons.Left || originalCardButton != sender)
                return;

            Size dragSize = SystemInformation.DragSize;
            Rectangle dragThreshold = new Rectangle(
                cardMouseDownScreenPoint.X - dragSize.Width / 2,
                cardMouseDownScreenPoint.Y - dragSize.Height / 2,
                dragSize.Width,
                dragSize.Height);

            if (!dragThreshold.Contains(Cursor.Position))
            {
                StartGhostCardDrag();
            }
        }

        private void CardButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || originalCardButton != sender || ghostCard != null)
                return;

            originalCardButton.Capture = false;
            isCardDragPending = false;
            originalCardButton = null;
        }

        private void StartGhostCardDrag()
        {
            if (originalCardButton == null || originalCardButton.IsDisposed || ghostCard != null)
                return;

            isCardDragPending = false;
            originalCardButton.Capture = false;
            ghostCard = new Button
            {
                Width = originalCardButton.Width,
                Height = originalCardButton.Height,
                Text = originalCardButton.Text,
                Font = originalCardButton.Font,
                BackColor = originalCardButton.BackColor,
                FlatStyle = FlatStyle.Flat,
                BackgroundImage = originalCardButton.BackgroundImage,
                BackgroundImageLayout = originalCardButton.BackgroundImageLayout,
                ForeColor = Color.White
            };

            Point mousePos = PointToClient(Cursor.Position);
            ghostCard.Location = new Point(mousePos.X - ghostCard.Width / 2, mousePos.Y - ghostCard.Height / 2);
            Controls.Add(ghostCard);
            ghostCard.BringToFront();
            originalCardButton.Visible = false;

            ghostCard.MouseMove += GhostCard_MouseMove;
            ghostCard.MouseUp += GhostCard_MouseUp;
            ghostCard.Capture = true;
        }

        private void GhostCard_MouseMove(object sender, MouseEventArgs e)
        {
            if (ghostCard != null && e.Button == MouseButtons.Left)
            {
                Point mousePos = this.PointToClient(Cursor.Position);
                ghostCard.Location = new Point(mousePos.X - (ghostCard.Width / 2), mousePos.Y - (ghostCard.Height / 2));
                // 잔상제거용
                this.Update();
            }
        }

        private void GhostCard_MouseUp(object sender, MouseEventArgs e)
        {
            if (ghostCard != null)
            {
                ghostCard.Capture = false;

                if (!IsNetworkReady())
                {
                    inputController.CancelSelection();
                    FinishGhostCardDrag();
                    return;
                }

                // 현재 마우스가 폼 전체에서 어디에 있는지 좌표 확인
                Point formPt = this.PointToClient(Cursor.Position);
                // 현재 마우스가 체스판(pnlBoard) 안에서 어디에 있는지 좌표 확인
                Point boardPt = pnlBoard.PointToClient(Cursor.Position);

                // [체크] 카드를 드롭한 위치가 '손패(pnlPlayerHand) 영역' 안쪽인가?
                bool isDroppedInHand = pnlPlayerHand.Bounds.Contains(formPt);
                // 현재 쥐고 있는 카드 정보 가져오기 (내 턴이 아니어도 가져올 수 있음)
                ICard draggedCard = (ICard)originalCardButton.Tag;

                if (isDroppedInHand)
                {
                    // 손패 안에서 드롭했다면 카드 순서(위치) 변경!
                    Point handPt = pnlPlayerHand.PointToClient(Cursor.Position);

                    // 마우스 좌표를 이용해 카드가 놓일 가상의 '칸(인덱스)'을 계산합니다 (카드너비 80+간격10=90, 높이 120+간격10=130)
                    int cardWidth, cardHeight, spacingX, spacingY, startX, startY;
                    GetPlayerCardLayout(out cardWidth, out cardHeight, out spacingX, out spacingY, out startX, out startY);
                    int col = Math.Max(0, (handPt.X - startX) / Math.Max(1, cardWidth + spacingX));
                    int row = Math.Max(0, (handPt.Y - startY) / Math.Max(1, cardHeight + spacingY));
                    int newIndex = row * 4 + col;

                    var myHand = gameManager.State.Hands[inputController.MyPlayerType];

                    // 인덱스가 배열 크기를 벗어나지 않도록 안전장치
                    if (newIndex >= myHand.Count) newIndex = myHand.Count - 1;
                    if (newIndex < 0) newIndex = 0;

                    ICard selectedCard = inputController.SelectedCard;
                    int oldIndex = myHand.IndexOf(draggedCard);

                    // 위치가 달라졌다면 원래 자리에서 빼서 새 자리에 쏙 끼워넣기
                    if (oldIndex != -1 && oldIndex != newIndex)
                    {
                        myHand.RemoveAt(oldIndex);
                        myHand.Insert(newIndex, draggedCard);
                        if (networkProtocol != null && networkProtocol.IsConnected)
                        {
                            networkProtocol.Send($"HANDORDER,{inputController.MyPlayerType},{oldIndex},{newIndex}");
                        }
                    }

                    inputController.CancelSelection();
                    RefreshHand(); // 변경된 순서대로 손패 화면 즉시 새로고침
                }
                else
                {
                    // 손패 밖으로 던졌을 때는 사용 시도로 간주

                    // 내 턴이 아닐 경우 카드의 발동을 캔슬
                    if (!battleManager.IsPlayable || gameManager.CurrentTurn != inputController.MyPlayerType)
                    {
                        AddLog("지금은 카드를 사용할 수 없는 턴입니다. (순서 변경만 가능)");
                        inputController.CancelSelection();
                        RefreshHand();
                    }
                    else // 내 턴이 맞을 경우 정상적으로 카드 발동
                    {
                        if (boardView.TryConvertPixelToPosition(boardPt.X, boardPt.Y, out Position targetPos))
                        {
                            inputController.OnBoardClicked(targetPos);
                            boardView.HandleMovementAnimation();
                            ShowGameEndMessageIfNeeded();
                            RefreshHand();
                        }
                        else
                        {
                            AddLog("카드를 보드 밖에 놓아 사용을 취소했습니다.");
                            inputController.CancelSelection();
                        }
                    }
                }

                FinishGhostCardDrag();
            }
        }

        private void FinishGhostCardDrag()
        {
            if (ghostCard != null)
            {
                this.Controls.Remove(ghostCard);
                ghostCard.Dispose();
                ghostCard = null;
            }

            isCardDragPending = false;
            if (originalCardButton != null)
            {
                if (!originalCardButton.IsDisposed)
                    originalCardButton.Visible = true;
                originalCardButton = null;
            }
        }

        private void BtnPassTurn_Click(object sender, EventArgs e)
        {
            if (!IsNetworkReady())
                return;

            if (!battleManager.IsPlayable || gameManager.CurrentTurn != inputController.MyPlayerType)
            {
                AddLog("지금은 턴을 종료할 수 없습니다.");
                return;
            }

            gameManager.PassTurn();
        }

        private bool IsNetworkReady()
        {
            if (networkProtocol == null || networkProtocol.IsConnected)
                return true;

            AddLog("[네트워크] 재연결 중에는 게임 조작을 할 수 없습니다.");
            return false;
        }

        private void NetworkProtocol_OnMessage(string msg)
        {
            if (this.InvokeRequired)
            {
                if (!IsDisposed && IsHandleCreated)
                    this.BeginInvoke(new Action(() => NetworkProtocol_OnMessage(msg)));
                return;
            }

            if (msg == "CONNECTED")
            {
                if (lblNetworkStatus != null)
                {
                    lblNetworkStatus.Text = "네트워크: 연결됨";
                    lblNetworkStatus.ForeColor = Color.Green;
                }

                AddLog("[네트워크] 상대방과 연결되었습니다.");
            }
            else if (msg == "SERVER_RECONNECTING" || msg == "SERVER_DISCONNECTED" || msg == "PEER_RECONNECTING")
            {
                if (lblNetworkStatus != null)
                {
                    lblNetworkStatus.Text = "네트워크: 재연결 중";
                    lblNetworkStatus.ForeColor = Color.Goldenrod;
                }

                inputController.CancelSelection();
                FinishGhostCardDrag();
                AddLog(msg == "PEER_RECONNECTING"
                    ? "[네트워크] 상대방의 연결이 끊어졌습니다. 재접속을 기다립니다."
                    : "[네트워크] 서버 연결이 끊어져 자동 재접속 중입니다.");
            }
            else if (msg == "SERVER_RECONNECTED" || msg == "PEER_RECONNECTED" || msg == "REJOINED")
            {
                if (lblNetworkStatus != null)
                {
                    lblNetworkStatus.Text = "네트워크: 연결 복구됨";
                    lblNetworkStatus.ForeColor = Color.Green;
                }

                AddLog("[네트워크] 연결이 복구되었습니다.");
            }
            else if (msg == "OPPONENT_DISCONNECTED")
            {
                AddLog("[네트워크] 상대방이 재접속 제한 시간 안에 돌아오지 않았습니다.");
                gameManager.State.IsGameOver = true;
                gameManager.State.Winner = inputController.MyPlayerType;
                ShowGameEndMessageIfNeeded();
            }
            else if (msg == "ROOM_LOST")
            {
                AddLog("[네트워크] 서버가 재시작되어 기존 방을 복구할 수 없습니다.");
                gameManager.State.IsGameOver = true;
                gameManager.State.Winner = null;
                ShowGameEndMessageIfNeeded();
            }
            else if (msg.StartsWith("MOVE,"))
            {
                string[] p = msg.Split(',');

                int fromRow, fromCol, toRow, toCol;
                if (p.Length < 5 ||
                    !int.TryParse(p[1], out fromRow) ||
                    !int.TryParse(p[2], out fromCol) ||
                    !int.TryParse(p[3], out toRow) ||
                    !int.TryParse(p[4], out toCol))
                {
                    AddLog("[네트워크] 잘못된 이동 패킷을 무시했습니다.");
                    return;
                }

                Position from = new Position(fromRow, fromCol);
                Position to = new Position(toRow, toCol);
                if (!gameManager.State.IsWithinBoard(from) || !gameManager.State.IsWithinBoard(to))
                {
                    AddLog("[네트워크] 보드 범위를 벗어난 이동 패킷을 무시했습니다.");
                    return;
                }

                gameManager.QueueRandomResults(ParseRandomResults(p, 5));

                gameManager.IsLocalAction = false;
                string networkMoveMessage;
                bool networkMoveSuccess;
                try
                {
                    networkMoveSuccess = gameManager.TryMoveOrAttack(from, to, out networkMoveMessage);
                }
                finally
                {
                    gameManager.IsLocalAction = true;
                }

                AddLog($"[네트워크] 상대방이 ({from.Row}, {from.Col})에서 ({to.Row}, {to.Col})(으)로 기물을 이동했습니다.");

                if (!string.IsNullOrEmpty(networkMoveMessage))
                {
                    if (networkMoveSuccess)
                    {
                        AddLog($"[네트워크] {networkMoveMessage}");
                    }
                    else
                        AddLog($"[네트워크 실패] {networkMoveMessage}");
                }

                boardView.HandleMovementAnimation();

                RefreshBoard();
                RefreshHand();
                pnlBoard.Invalidate();

                ShowGameEndMessageIfNeeded();
            }
            else if (msg.StartsWith("CARD,"))
            {
                string[] p = msg.Split(',');

                int targetRow, targetCol;
                if (p.Length < 4 ||
                    !int.TryParse(p[2], out targetRow) ||
                    !int.TryParse(p[3], out targetCol))
                {
                    AddLog("[네트워크] 잘못된 카드 패킷을 무시했습니다.");
                    return;
                }

                string cardName = p[1];
                Position target = new Position(targetRow, targetCol);
                if (!gameManager.State.IsWithinBoard(target))
                {
                    AddLog("[네트워크] 보드 범위를 벗어난 카드 패킷을 무시했습니다.");
                    return;
                }

                gameManager.QueueRandomResults(ParseRandomResults(p, 4));

                ICard cardToUse = gameManager.State.Hands[gameManager.CurrentTurn]
                    .FirstOrDefault(c => c.Name == cardName);

                if (cardToUse != null)
                {
                    gameManager.IsLocalAction = false;

                    string networkCardMessage;
                    bool networkCardSuccess;
                    try
                    {
                        networkCardSuccess = gameManager.TryUseCard(cardToUse, target, out networkCardMessage);
                    }
                    finally
                    {
                        gameManager.IsLocalAction = true;
                    }

                    AddLog($"[네트워크] 상대방이 '{cardName}' 카드를 사용했습니다!");

                    if (!string.IsNullOrEmpty(networkCardMessage))
                    {
                        if (networkCardSuccess)
                        {
                            AddLog($"[네트워크] {networkCardMessage}");
                            SoundsManager.Play("Piece_attack"); // 상대방 공격 사운드
                            if (cardName == "시간 왜곡")
                            {
                                boardView.TriggerClockEffect();
                            }
                        }
                        else
                            AddLog($"[네트워크 카드 실패] {networkCardMessage}");
                    }
                }
                else
                {
                    AddLog($"[동기화 오류] 상대방이 '{cardName}'을(를) 썼지만, 내 화면의 상대 손패에는 그 카드가 없습니다!");
                }

                boardView.HandleMovementAnimation();

                RefreshBoard();
                RefreshHand();
                pnlBoard.Invalidate();

                ShowGameEndMessageIfNeeded();
            }
            else if (msg.StartsWith("HANDORDER,"))
            {
                string[] p = msg.Split(',');
                PlayerType player;
                int oldIndex, newIndex;
                if (p.Length == 4 &&
                    Enum.TryParse(p[1], out player) &&
                    int.TryParse(p[2], out oldIndex) &&
                    int.TryParse(p[3], out newIndex) &&
                    player != inputController.MyPlayerType &&
                    gameManager.State.Hands.ContainsKey(player))
                {
                    List<ICard> hand = gameManager.State.Hands[player];
                    if (oldIndex >= 0 && oldIndex < hand.Count && newIndex >= 0 && newIndex < hand.Count)
                    {
                        ICard movedCard = hand[oldIndex];
                        hand.RemoveAt(oldIndex);
                        hand.Insert(newIndex, movedCard);
                        RefreshHand();
                    }
                }
            }
            // 상대방이 보낸 채팅 수신
            else if (msg.StartsWith("CHAT,"))
            {
                // "CHAT," 이후의 메시지 본문만 잘라내기
                string chatText = SanitizeChatMessage(msg.Substring(5));

                // 내가 Player1이면 상대는 Player2
                PlayerType opponent = (inputController.MyPlayerType == PlayerType.Player1) ? PlayerType.Player2 : PlayerType.Player1;

                // 로그창에 띄워주기
                AddLog($"[{opponent}] : {chatText}");
            }
            else if (msg.StartsWith("NETWORK_WARNING,"))
            {
                AddLog("[네트워크 경고] " + msg.Substring("NETWORK_WARNING,".Length));
            }
            else if (msg.StartsWith("SEND_FAILED,"))
            {
                AddLog("[네트워크 오류] " + msg.Substring("SEND_FAILED,".Length));
            }
            else if (msg == "PASS")
            {
                gameManager.IsLocalAction = false;
                gameManager.PassTurn();
                gameManager.IsLocalAction = true;

                AddLog("[네트워크] 상대방이 턴을 넘겼습니다.");

                RefreshBoard();
                RefreshHand();
                pnlBoard.Invalidate();
            }

            else if (msg == "SURRENDER")
            {
                gameManager.State.IsGameOver = true;
                gameManager.State.Winner = inputController.MyPlayerType;

                RefreshBoard();
                pnlBoard.Invalidate();

                ShowGameEndMessageIfNeeded();
            }
        }

        private void ShowGameEndMessageIfNeeded()
        {
            if (gameEndMessageShown || !gameManager.State.IsGameOver)
                return;

            string message = gameManager.State.Winner.HasValue
                ? $"{gameManager.State.Winner.Value} 승리!"
                : "게임이 종료되었습니다.";

            AddLog(message);

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

        // 이동할 수 있는 곳을 계산
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
            this.pnlPlayerDeck.SuspendLayout();
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
            this.pnlPlayerDeck.Controls.Add(this.lblCardDescription);
            this.pnlPlayerDeck.Location = new System.Drawing.Point(381, 3);
            this.pnlPlayerDeck.Name = "pnlPlayerDeck";
            this.pnlPlayerDeck.Size = new System.Drawing.Size(204, 272);
            this.pnlPlayerDeck.TabIndex = 7;
            // 
            // lblCardDescription
            // 
            this.lblCardDescription.BackColor = System.Drawing.Color.Transparent;
            this.lblCardDescription.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCardDescription.ForeColor = System.Drawing.Color.Black;
            this.lblCardDescription.Location = new System.Drawing.Point(10, 10);
            this.lblCardDescription.Name = "lblCardDescription";
            this.lblCardDescription.Size = new System.Drawing.Size(184, 252);
            this.lblCardDescription.TabIndex = 0;
            this.lblCardDescription.Text = "카드 설명";
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
            this.logbox.ItemHeight = 18;
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
            this.btnPassTurn.Text = "턴 보여줌";
            this.btnPassTurn.UseVisualStyleBackColor = true;
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
            this.pnlPlayerDeck.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        // 폼이 닫힐 때(게임이 끝날 때) 무조건 실행되는 안전장치
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (gameLoopTimer != null)
            {
                gameLoopTimer.Stop();
                gameLoopTimer.Dispose();
                gameLoopTimer = null;
            }

            if (networkProtocol != null)
            {
                networkProtocol.OnMessage -= NetworkProtocol_OnMessage;
                if (networkProtocol.IsConnected && !surrenderSent &&
                    (gameManager == null || !gameManager.State.IsGameOver))
                {
                    networkProtocol.Send("SURRENDER");
                    surrenderSent = true;
                }
                networkProtocol.Close();
            }
            base.OnFormClosed(e);
        }

        public void SurrenderAndClose()
        {
            if (!surrenderSent && networkProtocol != null && networkProtocol.IsConnected)
            {
                networkProtocol.Send("SURRENDER");
                surrenderSent = true;
            }

            if (gameManager != null)
            {
                gameManager.State.IsGameOver = true;
                gameManager.State.Winner = inputController.MyPlayerType == PlayerType.Player1
                    ? PlayerType.Player2
                    : PlayerType.Player1;
            }

            Close();
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

        // 단축키(키보드 1~8)로 손패 선택 및 ESC 취소 기능
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (txtChatInput != null && txtChatInput.Focused && keyData == Keys.Escape)
            {
                pnlBoard.Focus();
                return true;
            }

            // 채팅창에 글 쓸때는 숫자키 무시
            if (txtChatInput != null && txtChatInput.Focused)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
            if (!IsNetworkReady())
                return true;
            // 내 턴이 아니거나 조작 불가능한 상태면 키보드 입력 무시
            if (!battleManager.IsPlayable || gameManager.CurrentTurn != inputController.MyPlayerType)
                return base.ProcessCmdKey(ref msg, keyData);

            int cardIndex = -1;

            // 눌린 키가 1~8번(키패드 포함)인지 확인
            switch (keyData)
            {
                case Keys.D1: case Keys.NumPad1: cardIndex = 0; break;
                case Keys.D2: case Keys.NumPad2: cardIndex = 1; break;
                case Keys.D3: case Keys.NumPad3: cardIndex = 2; break;
                case Keys.D4: case Keys.NumPad4: cardIndex = 3; break;
                case Keys.D5: case Keys.NumPad5: cardIndex = 4; break;
                case Keys.D6: case Keys.NumPad6: cardIndex = 5; break;
                case Keys.D7: case Keys.NumPad7: cardIndex = 6; break;
                case Keys.D8: case Keys.NumPad8: cardIndex = 7; break;

                // ESC 키를 누르면 선택 취소
                case Keys.Escape:
                    inputController.CancelSelection();
                    AddLog("[키보드] 선택이 취소되었습니다.");
                    HighlightSelectedCard();
                    return true;
            }

            // 1~8번 키를 눌렀을 때
            if (cardIndex >= 0)
            {
                PlayerType myType = inputController.MyPlayerType;

                // 해당 번호의 카드가 내 손패에 존재할 경우에만 실행
                if (gameManager.State.Hands.ContainsKey(myType) && gameManager.State.Hands[myType].Count > cardIndex)
                {
                    ICard cardToSelect = gameManager.State.Hands[myType][cardIndex];

                    // 입력 컨트롤러에 '이 카드 선택했음' 알림 (마우스로 클릭한 것과 동일한 효과!)
                    inputController.OnCardClicked(cardToSelect);

                    // 선택한 카드의 설명을 좌측 설명창에 바로 띄워줌
                    ShowCardDescription(cardToSelect);
                    HighlightSelectedCard();

                    // 스킬 종류에 따라 안내 로그 출력
                    if (cardToSelect.Type == CardType.ActiveSkill || cardToSelect.Type == CardType.Trap)
                        AddLog($"[키보드] '{cardToSelect.Name}' 선택됨. 체스판 아무 곳이나 클릭하면 발동됩니다!");
                    else
                        AddLog($"[키보드] '{cardToSelect.Name}' 선택됨. 스킬을 사용할 타겟(체스판)을 마우스로 클릭하세요!");

                    return true; 
                }
            }

            // 위에서 처리되지 않은 나머지 키보드 입력은 폼이 원래 하던 대로 처리하도록 넘김
            return base.ProcessCmdKey(ref msg, keyData);
        }

        //  채팅 치고 엔터 눌렀을 때 발동하는 함수
        private void TxtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // 윈도우 특유의 에러 비프음 방지

                if (!IsNetworkReady())
                    return;

                string chatMsg = SanitizeChatMessage(txtChatInput.Text);
                if (!string.IsNullOrEmpty(chatMsg))
                {
                    if (txtChatInput.Text.Trim().Length > MaxChatLength)
                    {
                        AddLog($"[채팅] 메시지는 {MaxChatLength}자까지만 전송됩니다.");
                    }
                    // 1. 내 화면 로그창에 먼저 띄우기
                    AddLog($"[{inputController.MyPlayerType}] : {chatMsg}");

                    // 2. 상대방에게 네트워크 전달 ("CHAT,메시지" 형태)
                    if (networkProtocol != null && networkProtocol.IsConnected)
                    {
                        networkProtocol.Send($"CHAT,{chatMsg}");
                    }

                    // 3. 보냈으니 입력창 비우기
                    txtChatInput.Clear();
                }
            }
        }

        private List<int> ParseRandomResults(string[] parts, int startIndex)
        {
            List<int> results = new List<int>();
            for (int i = startIndex; i < parts.Length; i++)
            {
                if (!parts[i].StartsWith("R:"))
                    continue;

                string[] values = parts[i].Substring(2).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string valueText in values)
                {
                    int value;
                    if (int.TryParse(valueText, out value))
                    {
                        results.Add(value);
                    }
                }
            }
            return results;
        }

        private string SanitizeChatMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "";

            string cleaned = new string(message
                .Where(c => !char.IsControl(c) || c == '\t')
                .ToArray())
                .Trim();

            return cleaned.Length <= MaxChatLength
                ? cleaned
                : cleaned.Substring(0, MaxChatLength);
        }

        // 선택된 카드 시각적 강조 (위로 15px 띄우고 황금색 테두리)
        private void HighlightSelectedCard()
        {
            int cardWidth, cardHeight, spacingX, spacingY, startX, startY;
            GetPlayerCardLayout(out cardWidth, out cardHeight, out spacingX, out spacingY, out startX, out startY);
            PlayerType myType = inputController.MyPlayerType;

            if (!gameManager.State.Hands.ContainsKey(myType)) return;
            var myHand = gameManager.State.Hands[myType];

            foreach (Control c in pnlPlayerHand.Controls)
            {
                if (c is Button btnCard && btnCard.Tag is ICard card)
                {
                    int index = myHand.IndexOf(card);
                    if (index < 0) continue;

                    int row = index / 4; // 몇 번째 줄인지 계산
                    int baseTop = startY + (cardHeight + spacingY) * row; // 카드의 원래 Y좌표

                    if (inputController.SelectedCard == card)
                    {
                        btnCard.Top = baseTop - 15;
                        btnCard.FlatAppearance.BorderColor = Color.Gold;
                        btnCard.ForeColor = Color.White; // 글자색 강제 하얀색
                    }
                    else
                    {
                        // 선택되지 않은 카드: 원래 위치와 기본 색상으로 얌전하게 복구
                        btnCard.Top = baseTop;
                        btnCard.FlatAppearance.BorderColor = Color.Black;
                        btnCard.ForeColor = Color.White; // 글자색 강제 하얀색
                    }
                }
            }
        }

        private void GetPlayerCardLayout(
            out int cardWidth,
            out int cardHeight,
            out int spacingX,
            out int spacingY,
            out int startX,
            out int startY)
        {
            float scaleX = ClientSize.Width / (float)DesignClientWidth;
            float scaleY = ClientSize.Height / (float)DesignClientHeight;
            spacingX = Math.Max(4, (int)Math.Round(10 * scaleX));
            spacingY = Math.Max(4, (int)Math.Round(10 * scaleY));
            startX = Math.Max(4, (int)Math.Round(10 * scaleX));
            startY = Math.Max(4, (int)Math.Round(10 * scaleY));

            int cardAreaWidth = Math.Max(200, pnlPlayerDeck.Left - startX);
            cardWidth = Math.Max(32, (cardAreaWidth - startX - spacingX * 3) / 4);
            int maxCardHeight = Math.Max(50, (pnlPlayerHand.Height - startY * 2 - spacingY) / 2);
            cardHeight = Math.Max(48, Math.Min(maxCardHeight, (int)Math.Round(cardWidth * 1.5)));
        }

        private void ShowCardDescription(ICard card)
        {
            lblCardDescription.ForeColor = Color.White;
            lblCardDescription.Text =
                $"[{card.Name}]\n\n" +
                $"종류: {card.Type}\n\n" +
                $"{card.Description}";
        }
    }
}
