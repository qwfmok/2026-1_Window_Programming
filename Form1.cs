using CardChess.Core;
using CardChess.Models;
using CardChess.Networking;
using CardChess.View;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

// 메인 메뉴 UI 통제는 여기서

namespace CardChess
{
    public partial class Form1 : Form
    {
        // 배경이랑 텍스트박스 UI
        private Image backgroundBg;
        private Image Barimg;

        // 원본 이미지 버튼들
        private Image imgRoomCreate;
        private Image imgRoomJoin;
        private Image imgGameStart;
        private Image imgManual;
        private Image imgCredit;
        private Image imgExit;

        // 네트워크 통신 관련 컨트롤
        private SignalRProtocol networkProtocol;
        private PlayerType myPlayerType;
        private TextBox txtNetworkCode;
        private Label lblNetworkStatus;
        private bool isGameLaunched = false; // 중복창 버그 방지
        private int sharedSeed;
        // 주 진입점

        public Form1()
        {
            InitializeComponent();

            CardChess.Menu.SoundsManager.LoadALLSounds();
            CardChess.Menu.SoundsManager.PlayBGM("bg_music");

            this.Width = 1600;
            this.Height = 900;
            this.DoubleBuffered = true; // 이미지 깜빡임 방지

            LoadGameAssets();
            InitLobbyUI();
            // 설정 에셋 이미지 버튼 추가
            Button btnSettings = new Button();
            btnSettings.Size = new Size(60, 59);    // 에셋 이미지 크기에 맞게 조절
            btnSettings.Location = new Point(20, 20); // 좌측 상단
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
                using (CardChess.Menu.SettingsMenu settings = new CardChess.Menu.SettingsMenu(this, false, null))
                {
                    settings.ShowDialog();
                }
            };

            this.Controls.Add(btnSettings);
            btnSettings.BringToFront(); // 다른 UI 요소에 가려지지 않도록 맨 앞으로 가져옴
            ResponsiveLayout.Attach(this, new Size(1584, 861));
        }
        
        // 간단하게 로드 게임 에셋으로 시작

        private void LoadGameAssets()
        {
            try
            {
                string assetsPath = Path.Combine(Application.StartupPath, "Assets"); // 폴더 경로 정의. 해당 프로그램이 실행되는 곳의 디버그 폴더까지 자동 경로로 찾아가는거

                backgroundBg = Image.FromFile(Path.Combine(assetsPath, "bg.png")); // 라서 이렇게 파일명만 맞춰주면
                Barimg = Image.FromFile(Path.Combine(assetsPath, "bar.png")); // 위에처럼 배경도 불러오고 텍스트에 테두리도 칠해주고
                imgManual = Image.FromFile(Path.Combine(assetsPath, "button_manual.png")); // 설명서 버튼 이미지 불러오기
                imgRoomCreate = Image.FromFile(Path.Combine(assetsPath, "button_roomcreate.png")); // 방만들기 버튼도 있으면 그것도 넣어주고
                imgRoomJoin = Image.FromFile(Path.Combine(assetsPath, "button_join.png")); // 들어가는 버튼도 넣어주고
                imgGameStart = Image.FromFile(Path.Combine(assetsPath, "button_gamestart.png")); // 시작 버튼도 넣어주고
                imgExit = Image.FromFile(Path.Combine(assetsPath, "button_exit.png")); // 나가기 버튼이랑
                imgCredit = Image.FromFile(Path.Combine(assetsPath, "button_credit.png")); // 크레딧까지 구현해줘요 참 편리하죠?
            }
            catch (Exception ex)
            {
                MessageBox.Show("이미지 로드 실패: " + ex.Message); // 에셋이 없어?
            }
        }

        private void InitLobbyUI()
        {
            // 접속 코드 입력하는 칸 텍스트 박스
            txtNetworkCode = new TextBox();
            txtNetworkCode.Location = new Point(90, 600);
            txtNetworkCode.Size = new Size(140, 21);
            txtNetworkCode.Font = new Font("맑은 고딕", 10f);
            txtNetworkCode.TextAlign = HorizontalAlignment.Center;
            txtNetworkCode.BorderStyle = BorderStyle.None;

            // 네트워크 체크 여부 알려주는 라벨
            lblNetworkStatus = new Label();
            lblNetworkStatus.Location = new Point(90, 640);
            lblNetworkStatus.Size = new Size(540, 45);
            lblNetworkStatus.Font = new Font("맑은 고딕", 11f, FontStyle.Bold);
            lblNetworkStatus.ForeColor = Color.White;
            lblNetworkStatus.BackColor = Color.Transparent;
            lblNetworkStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblNetworkStatus.Text = "네트워크: 오프라인";

            this.Controls.Add(txtNetworkCode);
            this.Controls.Add(lblNetworkStatus);
        }

        // 버튼 가로 정렬 영역 계산해주는 함수
        private Dictionary<string, Rectangle> CalculateButtonRects()
        {
            var rects = new Dictionary<string, Rectangle>();

            int currentX = 90;   // 처음 버튼이 놓이는 자리 (방 만들기 버튼이 여기 놓임)
            int buttonY = 700;  // 그 버튼들 Y좌표
            int gap = 80;       // 버튼 간 간격

            if (imgRoomCreate != null) // 방만들기 버튼이 비어있다면
            {
                rects["Create"] = ScaleDesignRectangle(new Rectangle(currentX, buttonY, imgRoomCreate.Width, imgRoomCreate.Height));
                currentX += imgRoomCreate.Width + gap; // 다음 버튼은 버튼의 와이드값 + gap만큼인데 gap이 80이니까 쉽게 말해서 80px 뒤에 배치하란 뜻
            }
            if (imgRoomJoin != null) // 그 다음은 방 들어가기 버튼인데
            {
                rects["Join"] = ScaleDesignRectangle(new Rectangle(currentX, buttonY, imgRoomJoin.Width, imgRoomJoin.Height));
                currentX += imgRoomJoin.Width + gap; // 똑같음
            }
            if (imgGameStart != null) // 게임 시작
            {
                rects["Start"] = ScaleDesignRectangle(new Rectangle(currentX, buttonY, imgGameStart.Width, imgGameStart.Height));
                // [핵심] 메뉴얼 버튼은 가로 스크롤(currentX) 로직 밖으로 빼서 독립적으로 계산합니다!
                if (imgManual != null)
                {
                    // 게임 시작 버튼의 정중앙 좌표를 기준으로 매뉴얼 버튼 X좌표 설정
                    int manualX = currentX + (imgGameStart.Width / 2) - (imgManual.Width / 2);

                    // 게임 시작 버튼 Y좌표에서 위로 20픽셀만큼 띄워서 배치
                    int manualY = buttonY - imgManual.Height - 20;

                    rects["Manual"] = ScaleDesignRectangle(new Rectangle(manualX, manualY, imgManual.Width, imgManual.Height));
                }
                currentX += imgGameStart.Width + gap;
            }
            if (imgCredit != null) // 크레딧
            {
                rects["Credit"] = ScaleDesignRectangle(new Rectangle(currentX, buttonY, imgCredit.Width, imgCredit.Height));
                currentX += imgCredit.Width + gap;
            }
            if (imgExit != null) // 나가기
            {
                rects["Exit"] = ScaleDesignRectangle(new Rectangle(currentX, buttonY, imgExit.Width, imgExit.Height));
            }

            return rects;
        }

        private Rectangle ScaleDesignRectangle(Rectangle designRect)
        {
            float scaleX = ClientSize.Width / 1584f;
            float scaleY = ClientSize.Height / 861f;
            return new Rectangle(
                (int)Math.Round(designRect.X * scaleX),
                (int)Math.Round(designRect.Y * scaleY),
                Math.Max(1, (int)Math.Round(designRect.Width * scaleX)),
                Math.Max(1, (int)Math.Round(designRect.Height * scaleY)));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 방 번호 발급받는 텍스트 박스 감싸주는 UI에 관한 코드다요
            if (backgroundBg != null) e.Graphics.DrawImage(backgroundBg, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            if (Barimg != null) e.Graphics.DrawImage(Barimg, ScaleDesignRectangle(new Rectangle(77, 592, 166, 37)));

            // 
            var rects = CalculateButtonRects();

            // 버튼들 이미지가 null상태인지 체크하고 그리기
            if (imgRoomCreate != null && rects.ContainsKey("Create")) e.Graphics.DrawImage(imgRoomCreate, rects["Create"]);
            if (imgRoomJoin != null && rects.ContainsKey("Join")) e.Graphics.DrawImage(imgRoomJoin, rects["Join"]);
            if (imgGameStart != null && rects.ContainsKey("Start")) e.Graphics.DrawImage(imgGameStart, rects["Start"]);
            if (imgCredit != null && rects.ContainsKey("Credit")) e.Graphics.DrawImage(imgCredit, rects["Credit"]);
            if (imgExit != null && rects.ContainsKey("Exit")) e.Graphics.DrawImage(imgExit, rects["Exit"]);
            if (imgManual != null && rects.ContainsKey("Manual")) e.Graphics.DrawImage(imgManual, rects["Manual"]);
        }

        // 각 버튼 마우스 클릭 상속받는거

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Point mousePos = e.Location;
            var rects = CalculateButtonRects();

            if (System.Linq.Enumerable.Any(rects.Values, rect => rect.Contains(mousePos))) // 이게 있으면 OnMouseClick 메소드 내에서 행해지는 이벤트 핸들러들이 일괄적으로 UI 사운드를 내게 됨
            {
                CardChess.Menu.SoundsManager.Play("Menu_icon_select");
            }

            // 방 생성 클릭
            if (rects.ContainsKey("Create") && rects["Create"].Contains(mousePos))
            {
                HandleRoomCreate();
                return;
            }
            // 방 접속 클릭
            if (rects.ContainsKey("Join") && rects["Join"].Contains(mousePos))
            {
                HandleRoomJoin();
                return;
            }
            // 게임 시작 클릭
            if (rects.ContainsKey("Start") && rects["Start"].Contains(mousePos) && lblNetworkStatus.Text.Contains("연결됨")) // AND GATE 뒤에 있는 건 네트워크 연결 중인지에 대한 판단여부임
            {
                HandleGameStart();
                return;
                // 만약 인게임에서 테스트해야할 사항이 있다면 이걸 편집하지 말고 Program.cs에서 Application Run이라는 주 진입점에 주석처리된 MainForm 부분을 활성화하면 됨.
                // 반대로 MainForm에서 테스트를 마치고 돌아왔다면 다시 Form1 부분을 활성화하고 MainForm은 주석처리해서 해결가능
            }
            // 크레딧
            if (rects.ContainsKey("Credit") && rects["Credit"].Contains(mousePos))
            {
                HandleCredit();
                return;
            }
            // 종료버튼
            if (rects.ContainsKey("Exit") && rects["Exit"].Contains(mousePos))
            {
                HandleExit();
                return;
            }
            // 메뉴얼 버튼
            if (rects.ContainsKey("Manual") && rects["Manual"].Contains(mousePos))
            {
                HandleManual();
                return;
            }
        }

        // 버튼 기능 처리 로직
        // 설명서 메인 허브 창 열기
        private void HandleManual()
        {
            this.Hide();

            using (MainManual manualForm = new MainManual())
            {
                manualForm.ShowDialog();
            }

            this.Show();
            this.Invalidate();
        }

        private async void HandleRoomCreate() // 방 만드는 핸들러
        {
            try
            {
                ResetNetworkProtocol();
                lblNetworkStatus.Text = "온라인 서버 연결 중...";
                lblNetworkStatus.ForeColor = Color.White;
                string roomCode = await networkProtocol.CreateRoomAsync();
                if (string.IsNullOrEmpty(roomCode))
                    return;

                txtNetworkCode.Text = roomCode;
                lblNetworkStatus.Text = "방 생성 완료 - 상대방을 기다리는 중";
                lblNetworkStatus.ForeColor = Color.LightSkyBlue;
                myPlayerType = PlayerType.Player1;
            }
            catch (Exception ex)
            {
                lblNetworkStatus.Text = "방 생성 실패: " + ex.Message;
                lblNetworkStatus.ForeColor = Color.LightCoral;
            }
        }

        private async void HandleRoomJoin() // 접속 핸들러
        {
            string code = txtNetworkCode.Text.Trim();
            if (code.Length != 6 || !int.TryParse(code, out _))
            {
                MessageBox.Show("숫자 6자리 방 코드를 입력해주세요!");
                return;
            }

            try
            {
                ResetNetworkProtocol();
                lblNetworkStatus.Text = "온라인 서버 연결 중...";
                lblNetworkStatus.ForeColor = Color.White;
                bool joined = await networkProtocol.JoinRoomAsync(code);
                if (!joined)
                    return;

                txtNetworkCode.Text = networkProtocol.RoomCode;
                myPlayerType = PlayerType.Player2;
            }
            catch (Exception ex)
            {
                lblNetworkStatus.Text = "방 참여 실패: " + ex.Message;
                lblNetworkStatus.ForeColor = Color.LightCoral;
            }
        }

        private void ResetNetworkProtocol()
        {
            if (networkProtocol != null)
            {
                networkProtocol.OnMessage -= NetworkProtocol_OnMessage;
                networkProtocol.Close();
            }

            networkProtocol = new SignalRProtocol(NetworkSettings.SignalRServerUrl);
            networkProtocol.OnMessage += NetworkProtocol_OnMessage;
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
                lblNetworkStatus.Text = "네트워크: 연결됨! 🟢";
                lblNetworkStatus.ForeColor = Color.LightGreen;
                this.Invalidate();
            }
            else if (msg == "SERVER_CONNECTING")
            {
                lblNetworkStatus.Text = "서버를 깨우는 중... 최대 1분 정도 걸릴 수 있습니다.";
                lblNetworkStatus.ForeColor = Color.Khaki;
            }
            else if (msg.StartsWith("SERVER_RETRYING,"))
            {
                lblNetworkStatus.Text = "무료 서버 시작 대기 중... 자동으로 재시도합니다.";
                lblNetworkStatus.ForeColor = Color.Khaki;
            }
            else if (msg == "SERVER_CONNECTED")
            {
                lblNetworkStatus.Text = "서버 연결됨 - 방 정보를 처리하는 중";
                lblNetworkStatus.ForeColor = Color.LightSkyBlue;
            }
            else if (msg.StartsWith("ROOM_CREATED,"))
            {
                txtNetworkCode.Text = msg.Substring("ROOM_CREATED,".Length);
            }
            else if (msg.StartsWith("ROOM_JOINED,"))
            {
                txtNetworkCode.Text = msg.Substring("ROOM_JOINED,".Length);
            }
            else if (msg.StartsWith("CONNECTION_REJECTED,"))
            {
                lblNetworkStatus.Text = msg.Substring("CONNECTION_REJECTED,".Length);
                lblNetworkStatus.ForeColor = Color.LightCoral;
            }
            else if (msg == "PEER_RECONNECTING" || msg == "SERVER_RECONNECTING" || msg == "SERVER_DISCONNECTED")
            {
                lblNetworkStatus.Text = "연결이 끊겨 재접속 중입니다...";
                lblNetworkStatus.ForeColor = Color.Khaki;
            }
            else if (msg == "PEER_RECONNECTED" || msg == "REJOINED")
            {
                lblNetworkStatus.Text = "네트워크: 재연결됨! 🟢";
                lblNetworkStatus.ForeColor = Color.LightGreen;
            }
            else if (msg == "OPPONENT_DISCONNECTED" || msg == "ROOM_LOST")
            {
                lblNetworkStatus.Text = "방 연결이 종료되었습니다.";
                lblNetworkStatus.ForeColor = Color.LightCoral;
            }
            else if (msg.StartsWith("START"))
            {
                string[] parts = msg.Split(',');
                int parsedSeed;
                if (parts.Length > 1 && int.TryParse(parts[1], out parsedSeed))
                {
                    sharedSeed = parsedSeed;
                    LaunchMainGame();
                }
            }
        }

        private void HandleGameStart()
        {
            if (networkProtocol == null || !networkProtocol.IsConnected)
            {
                MessageBox.Show("상대방이 연결될 때까지 기다려주세요!", "연결 대기", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (myPlayerType == PlayerType.Player2)
            {
                MessageBox.Show("방장(Host)이 게임을 시작할 때까지 대기해 주세요!", "대기 중", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (myPlayerType == PlayerType.Player1)
            {
                // 방장만 무작위 시드 생성
                sharedSeed = new Random().Next(10000, 99999);
                if (!networkProtocol.Send($"START,{sharedSeed}"))
                {
                    MessageBox.Show("게임 시작 정보를 상대에게 보내지 못했습니다.");
                    return;
                }
            }
            LaunchMainGame();
        }

        // 로비에서 창을 강제로 'X' 눌러서 껐을 때 통신 포트를 완벽하게 닫아줌
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (networkProtocol != null)
            {
                networkProtocol.Close();
            }
            base.OnFormClosed(e);
        }
        private void LaunchMainGame()
        {
            // 이미 게임창이 켜졌다면 패킷이 중복으로 날아와도 무조건 차단
            if (isGameLaunched) return;
            isGameLaunched = true;

            // 메인 폼으로 넘어갈 때 기존 로비의 수신기를 끔
            networkProtocol.OnMessage -= NetworkProtocol_OnMessage;
            bool startFullScreen = this.FormBorderStyle == FormBorderStyle.None;
            Rectangle fullScreenBounds = Screen.FromControl(this).Bounds;
            MainForm gameForm = new MainForm(
                networkProtocol,
                myPlayerType,
                sharedSeed,
                startFullScreen,
                fullScreenBounds);

            this.Hide();           // 로비 화면을 잠깐 숨김
            gameForm.ShowDialog(); //  게임 화면을 띄움 (게임이 끝날 때까지 여기서 코드가 멈춤)


            // 네트워크 상태 초기화 및 소켓 닫기
            if (networkProtocol != null)
            {
                networkProtocol.Close();
                networkProtocol = null;
            }

            // 라벨을 오프라인으로 돌려서 시작 버튼 자동 잠금
            lblNetworkStatus.Text = "네트워크: 오프라인";
            lblNetworkStatus.ForeColor = Color.White;

            //  게임이 완벽히 종료되었으므로 플래그를 해제하여 다음 판 재시작을 허용함
            isGameLaunched = false;

            //  다시 로비 화면을 짠! 하고 보여주고 화면 그래픽 새로고침
            this.Show();
            this.Invalidate();
        }

        private void HandleCredit() // 크레딧 핸들러
        {
            MessageBox.Show("Game Created by 김재민, 박정우, 장현빈, 전경원\n2026 All Rights Reserved.\nTHANKS FOR PLAY ^^*", "CREDIT");
        }

        private void HandleExit() // 메모리 해체 핸들러
        {
            Environment.Exit(0);
        }
    }
}
