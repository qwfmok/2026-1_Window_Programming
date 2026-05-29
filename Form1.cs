using CardChess.Core;
using CardChess.Models;
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
        private UDPprotocol udpProtocol;
        private PlayerType myPlayerType;
        private TextBox txtNetworkCode;
        private Label lblNetworkStatus;
        private bool isGameLaunched = false; // 중복창 버그 방지
        private int sharedSeed;
        // 주 진입점

        public Form1()
        {
            InitializeComponent();

            this.Width = 1600;
            this.Height = 900;
            this.DoubleBuffered = true; // 이미지 깜빡임 방지

            LoadGameAssets();
            InitLobbyUI();
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
            lblNetworkStatus.Size = new Size(300, 30);
            lblNetworkStatus.Font = new Font("맑은 고딕", 11f, FontStyle.Bold);
            lblNetworkStatus.ForeColor = Color.White;
            lblNetworkStatus.BackColor = Color.Transparent;
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
                rects["Create"] = new Rectangle(currentX, buttonY, imgRoomCreate.Width, imgRoomCreate.Height);
                currentX += imgRoomCreate.Width + gap; // 다음 버튼은 버튼의 와이드값 + gap만큼인데 gap이 80이니까 쉽게 말해서 80px 뒤에 배치하란 뜻
            }
            if (imgRoomJoin != null) // 그 다음은 방 들어가기 버튼인데
            {
                rects["Join"] = new Rectangle(currentX, buttonY, imgRoomJoin.Width, imgRoomJoin.Height);
                currentX += imgRoomJoin.Width + gap; // 똑같음
            }
            if (imgGameStart != null) // 게임 시작
            {
                rects["Start"] = new Rectangle(currentX, buttonY, imgGameStart.Width, imgGameStart.Height);
                // [핵심] 메뉴얼 버튼은 가로 스크롤(currentX) 로직 밖으로 빼서 독립적으로 계산합니다!
                if (imgManual != null)
                {
                    // 게임 시작 버튼의 정중앙 좌표를 기준으로 매뉴얼 버튼 X좌표 설정
                    int manualX = currentX + (imgGameStart.Width / 2) - (imgManual.Width / 2);

                    // 게임 시작 버튼 Y좌표에서 위로 20픽셀만큼 띄워서 배치
                    int manualY = buttonY - imgManual.Height - 20;

                    rects["Manual"] = new Rectangle(manualX, manualY, imgManual.Width, imgManual.Height);
                }
                currentX += imgGameStart.Width + gap;
            }
            if (imgCredit != null) // 크레딧
            {
                rects["Credit"] = new Rectangle(currentX, buttonY, imgCredit.Width, imgCredit.Height);
                currentX += imgCredit.Width + gap;
            }
            if (imgExit != null) // 나가기
            {
                rects["Exit"] = new Rectangle(currentX, buttonY, imgExit.Width, imgExit.Height);
            }

            return rects;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 방 번호 발급받는 텍스트 박스 감싸주는 UI에 관한 코드다요
            if (backgroundBg != null) e.Graphics.DrawImage(backgroundBg, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            if (Barimg != null) e.Graphics.DrawImage(Barimg, 77, 592, 166, 37);

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

        private void HandleRoomCreate() // 방 만드는 핸들러
        {
            udpProtocol = new UDPprotocol();
            udpProtocol.OnMessage += UdpProtocol_OnMessage;

            // 6자리 무작위 난수(100000 ~ 999999) 생성 로직 추가
            Random rand = new Random();
            string randomCode = rand.Next(100000, 1000000).ToString();

            // 생성된 난수를 텍스트 박스에 기입
            txtNetworkCode.Text = randomCode;

            // 기존 호스트 통신 시작 함수 호출
            udpProtocol.Starthostip();

            lblNetworkStatus.Text = "호스트 대기중... (코드 전달)";
            myPlayerType = PlayerType.Player1;
        }

        private void HandleRoomJoin() // 접속 핸들러
        {
            string code = txtNetworkCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("접속 코드를 입력해주세요!");
                return;
            }

            udpProtocol = new UDPprotocol();
            udpProtocol.OnMessage += UdpProtocol_OnMessage;

            udpProtocol.Joinguestip(code);
            lblNetworkStatus.Text = "서버 접속 시도중...";
            myPlayerType = PlayerType.Player2;
        }

        private void UdpProtocol_OnMessage(string msg)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UdpProtocol_OnMessage(msg)));
                return;
            }

            // 1. 연결 성공 패킷 처리
            if (msg == "CONNECTED")
            {
                lblNetworkStatus.Text = "네트워크: 연결됨! 🟢";
                lblNetworkStatus.ForeColor = Color.LightGreen;
                this.Invalidate(); // 버튼 상태(활성화 등)를 다시 그리기 위해 화면 갱신
            }
            // 2. 방장이 보낸 게임 시작 동기화 패킷 처리 (게스트용)
            else if (msg.StartsWith("START"))
            {
                // 방장이 보낸 패킷(START,12345)을 쪼개서 내 시드로함
                string[] parts = msg.Split(',');
                if (parts.Length > 1)
                {
                    sharedSeed = int.Parse(parts[1]);
                }
                LaunchMainGame();
            }
        }

        private void HandleGameStart()
        {
            if (myPlayerType == PlayerType.Player2)
            {
                MessageBox.Show("방장(Host)이 게임을 시작할 때까지 대기해 주세요!", "대기 중", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (myPlayerType == PlayerType.Player1)
            {
                // 방장만 무작위 시드 생성
                sharedSeed = new Random().Next(10000, 99999);
                // 상대방한테 시드 쏘기
                udpProtocol.Send($"START,{sharedSeed}");
            }
            LaunchMainGame();
        }

        private void LaunchMainGame()
        {
            // 이미 게임창이 켜졌다면 패킷이 중복으로 날아와도 무조건 차단
            if (isGameLaunched) return;
            isGameLaunched = true;

            // 메인 폼으로 넘어갈 때 기존 로비의 수신기를 끔
            udpProtocol.OnMessage -= UdpProtocol_OnMessage;
            MainForm gameForm = new MainForm(udpProtocol, myPlayerType, sharedSeed);

            this.Hide();           // 로비 화면을 잠깐 숨김
            gameForm.ShowDialog(); //  게임 화면을 띄움 (게임이 끝날 때까지 여기서 코드가 멈춤)


            // 네트워크 상태 초기화 및 소켓 닫기
            if (udpProtocol != null)
            {
                udpProtocol.Close();
                udpProtocol = null;
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
            Application.Exit();
        }
    }
}

//using CardChess.Core;
//using CardChess.Models;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace CardChess
//{
//    public partial class Form1 : Form
//    {
//        private Image backgroundBg;
//        private Image Barimg;

//        private Image Numbercreate;
//        private Image Numberedit;
//        private Image Gamestartbtn;
//        private Image Exitgame;
//        private Image Makercredits;

//        private UDPprotocol udpProtocol;
//        private PlayerType myPlayerType;
//        private TextBox txtNetworkCode;
//        private Label lblNetworkStatus;

//        private Button btnHost;
//        private Button btnJoin;
//        private Button btnStart;
//        private Button btnCredits;
//        private Button btnExit;

//        public Form1()
//        {
//            InitializeComponent();

//            this.Width = 1600;
//            this.Height = 900; // 폼 최초 크기 정의
//            this.DoubleBuffered = true; // 이미지 깜빡임 방지

//            LoadBackgroundImage();
//            SetupTextBox();
//        }

//        private void LoadBackgroundImage()
//        {
//            {
//                try
//                {
//                    string assetsPath = Path.Combine(Application.StartupPath, "Assets");

//                    // 배경과 바 이미지 로드
//                    backgroundBg = Image.FromFile(Path.Combine(assetsPath, "bg.png"));
//                    Barimg = Image.FromFile(Path.Combine(assetsPath, "bar.png"));
//                    Numbercreate = Image.FromFile(Path.Combine(assetsPath, "button_roomcreate.png"));
//                    Numberedit = Image.FromFile(Path.Combine(assetsPath, "button_join.png"));
//                    Gamestartbtn = Image.FromFile(Path.Combine(assetsPath, "button_gamestart.png"));
//                    Makercredits = Image.FromFile(Path.Combine(assetsPath, "button_exit.png"));
//                    Exitgame = Image.FromFile(Path.Combine(assetsPath, "button_credit.png"));
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show("이미지 로드 실패: " + ex.Message);
//                }
//            }
//        }
//        private void SetupTextBox()
//        {
//            textBox1.BorderStyle = BorderStyle.None;
//        }

//        private void InitLobbyUI()
//        {
//            lblNetworkStatus = new Label();
//            lblNetworkStatus.Location = new Point(90, 640);
//            lblNetworkStatus.Size = new Size(300, 30);
//            lblNetworkStatus.Font = new Font("맑은 고딕", 11f, FontStyle.Bold);
//            lblNetworkStatus.ForeColor = Color.White;
//            lblNetworkStatus.BackColor = Color.Transparent;
//            lblNetworkStatus.Text = "네트워크: 오프라인";

//            btnHost = CreateImageButton(Numbercreate, new Point(90, 680), new Size(140, 40), BtnHost_Click);
//            btnJoin = CreateImageButton(Numberedit, new Point(240, 680), new Size(140, 40), BtnJoin_Click);
//            btnStart = CreateImageButton(Gamestartbtn, new Point(90, 740), new Size(290, 50), BtnStart_Click);
//            btnCredits = CreateImageButton(Makercredits, new Point(90, 800), new Size(140, 40), BtnCredits_Click);
//            btnExit = CreateImageButton(Exitgame, new Point(240, 800), new Size(140, 40), BtnExit_Click);

//            btnStart.Enabled = false;

//            this.Controls.Add(txtNetworkCode);
//            this.Controls.Add(lblNetworkStatus);
//            this.Controls.Add(btnHost);
//            this.Controls.Add(btnJoin);
//            this.Controls.Add(btnStart);
//            this.Controls.Add(btnCredits);
//            this.Controls.Add(btnExit);
//        }
//        private Button CreateImageButton(Image buttonImage, Point location, Size size, EventHandler clickEvent)
//        {
//            Button btn = new Button();
//            btn.Location = location;
//            btn.Size = size;

//            // 텍스트는 지우고 이미지를 정중앙에 배치합니다.
//            btn.Text = "";
//            btn.Image = buttonImage;
//            btn.ImageAlign = ContentAlignment.MiddleCenter;

//            // 버튼 특유의 Windows 스타일 뼈대 완전히 걷어내기
//            btn.FlatStyle = FlatStyle.Flat;
//            btn.FlatAppearance.BorderSize = 0;
//            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
//            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
//            btn.BackColor = Color.Transparent;

//            btn.Click += clickEvent;
//            return btn;
//        }
//        private void BtnHost_Click(object sender, EventArgs e)
//        {
//            udpProtocol = new UDPprotocol();
//            udpProtocol.OnMessage += UdpProtocol_OnMessage;

//            string code = udpProtocol.Starthostip();
//            txtNetworkCode.Text = code;
//            lblNetworkStatus.Text = "호스트 대기중... (코드 전달)";

//            myPlayerType = PlayerType.Player1;
//            btnHost.Enabled = false;
//            btnJoin.Enabled = false;
//        }

//        private void BtnJoin_Click(object sender, EventArgs e)
//        {
//            string code = txtNetworkCode.Text.Trim();
//            if (string.IsNullOrEmpty(code))
//            {
//                MessageBox.Show("접속 코드를 입력해주세요!");
//                return;
//            }

//            udpProtocol = new UDPprotocol();
//            udpProtocol.OnMessage += UdpProtocol_OnMessage;

//            udpProtocol.Joinguestip(code);
//            lblNetworkStatus.Text = "서버 접속 시도중...";

//            myPlayerType = PlayerType.Player2;
//            btnHost.Enabled = false;
//            btnJoin.Enabled = false;
//        }

//        private void UdpProtocol_OnMessage(string msg)
//        {
//            if (this.InvokeRequired)
//            {
//                this.Invoke(new Action(() => UdpProtocol_OnMessage(msg)));
//                return;
//            }

//            if (msg == "CONNECTED")
//            {
//                lblNetworkStatus.Text = "네트워크: 연결됨! 🟢";
//                lblNetworkStatus.ForeColor = Color.LightGreen;
//                btnStart.Enabled = true; // 대기 타던 시작 이미지 버튼 활성화
//            }
//        }

//        private void BtnStart_Click(object sender, EventArgs e)
//        {
//            udpProtocol.OnMessage -= UdpProtocol_OnMessage;
//            MainForm gameForm = new MainForm(udpProtocol, myPlayerType);

//            this.Hide();
//            gameForm.ShowDialog();
//            this.Close();
//        }

//        private void BtnCredits_Click(object sender, EventArgs e)
//        {
//            MessageBox.Show("Game Created by 개발자님!\n2026 All Rights Reserved.", "크레딧");
//        }

//        private void BtnExit_Click(object sender, EventArgs e)
//        {
//            Application.Exit();
//        }
//        protected override void OnPaint(PaintEventArgs e)
//        {
//            base.OnPaint(e);
//            if (backgroundBg != null)
//            {
//                e.Graphics.DrawImage(backgroundBg, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
//            }
//            if (Barimg != null)
//            {
//                e.Graphics.DrawImage(Barimg, 77, 588, 166, 37);
//            }
//        }
//    }
//}
