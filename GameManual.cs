using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace CardChess
{
    public partial class GameManual : Form
    {
        private Image backgroundBg;
        private Image imgTurn;
        private Image imgRule;
        private Image imgControl;
        private Image imgBack;
        public GameManual()
        {
            InitializeComponent();

            // 창 크기 및 기본 설정
            this.Width = 1600;
            this.Height = 900;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackgroundImageLayout = ImageLayout.Stretch;
            LoadGameAssets();
            // 처음 켜졌을 때 첫 번째 탭(기본 룰)을 보여줌
            LoadRuleData("Basic");
            // 픽쳐박스 2개 모두 비율에 맞춰 축소 (짤림 방지)
            picTop.SizeMode = PictureBoxSizeMode.Zoom;
            picBottom.SizeMode = PictureBoxSizeMode.Zoom;
            ApplyButtonImages(); // 버튼에 이미지를 씌우는 로직 호출
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
            CardChess.View.ResponsiveLayout.Attach(this, new Size(1584, 861));
        }
        private void LoadGameAssets()
        {
            try
            {
                string assetsPath = Path.Combine(Application.StartupPath, "Assets");

                backgroundBg = Image.FromFile(Path.Combine(assetsPath, "bg_remove_text.png"));
                imgBack = Image.FromFile(Path.Combine(assetsPath, "button_back.png"));
                imgTurn = Image.FromFile(Path.Combine(assetsPath, "btn_turn.png"));
                imgRule = Image.FromFile(Path.Combine(assetsPath, "btn_rule.png"));
                imgControl = Image.FromFile(Path.Combine(assetsPath, "btn_control.png"));
                this.BackgroundImage = backgroundBg;
            }
            catch (Exception ex)
            {
                MessageBox.Show("이미지 로드 실패: " + ex.Message);
            }
        }
        private void ApplyButtonImages()
        {
            // 1. 폼 배경 지정
            if (backgroundBg != null)
            {
                this.BackgroundImage = backgroundBg;
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }

            // 2. 디자이너에서 올린 버튼들에 이미지 적용 및 투명화 세팅
            SetupImageButton(btnRuleBasic, imgRule);
            SetupImageButton(btnRuleControl, imgControl);
            SetupImageButton(btnRuleTurn, imgTurn);
            SetupImageButton(btnBack, imgBack);
        }
        // 버튼 테두리를 없애고 이미지만 깔끔하게 보이도록 만드는 헬퍼 함수
        private void SetupImageButton(Button btn, Image img)
        {
            if (btn == null || img == null) return;

            btn.Image = img;
            btn.Size = new Size(img.Width, img.Height);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0; // 테두리 제거
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent; // 클릭 시 배경색 제거
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent; // 마우스 올릴 때 배경색 제거
            btn.BackColor = Color.Transparent;
            btn.Text = ""; // 기존 텍스트 가리기
            btn.Cursor = Cursors.Hand; // 마우스 올리면 손가락 모양으로 변경
        }
        // 상단 탭 버튼 클릭 이벤트
        private void btnRuleBasic_Click(object sender, EventArgs e) { LoadRuleData("Basic"); }
        private void btnRuleTurn_Click(object sender, EventArgs e) { LoadRuleData("Turn"); }
        private void btnRuleControl_Click(object sender, EventArgs e) { LoadRuleData("Control"); }

        // 탭에 맞춰 이미지 2장과 글을 갈아끼우는 함수
        private void LoadRuleData(string ruleType)
        {
            string assetsPath = Path.Combine(Application.StartupPath, "Assets");

            try
            {
                string img1 = "", img2 = "";

                switch (ruleType)
                {
                    case "Basic":
                        img1 = "rule_first.png"; // 예: 초기 배치 화면
                        img2 = "rule_win.png"; // 예: 체크메이트 당한 화면
                        break;
                    case "Turn":
                        img1 = "rule_carduse.png"; // 예: 카드 사용 화면 
                        img2 = "bishop_move.png"; // 예: 기물 이동 화면
                        break;
                    case "Control":
                        img1 = "rule_carduse.png"; // 예: 카드 사용 방법
                        img2 = "queen_move.png"; // 예: 기물 이동 및 공격 방법
                        break;
                }

                // 이미지 로드 (파일이 없으면 안전하게 null 처리)
                string path1 = Path.Combine(assetsPath, img1);
                string path2 = Path.Combine(assetsPath, img2);

                picTop.Image = File.Exists(path1) ? Image.FromFile(path1) : null;
                picBottom.Image = File.Exists(path2) ? Image.FromFile(path2) : null;

                // 텍스트 교체 호출
                SetDescription(ruleType);
            }
            catch (Exception) { /* 무시 */ }
        }

        // 규칙 상세 설명 텍스트
        private void SetDescription(string ruleType)
        {
            rtbDescription.Clear();
            // 공통 타이틀 스타일 (대제목)
            rtbDescription.SelectionFont = new Font("맑은 고딕", 20f, FontStyle.Bold);
            rtbDescription.SelectionColor = Color.Gold;

            switch (ruleType)
            {
                case "Basic":
                    rtbDescription.AppendText("👑 기본 룰 & 승리 조건\n");

                    // 본문
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 11f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("카드 체스(Card Chess)는 체스와 마법 카드가 결합된 보드게임입니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 11f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("기물 움직임 방식 및 승리조건 등은 기존 체스와 유사합니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 11f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("기존 체스와 달리 킹, 퀸을 제외한 모든 기물이 폰(Pawn)으로 시작합니다.\n\n");

                    // 소제목
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.LightSkyBlue;
                    rtbDescription.AppendText("■ 승리 조건\n");

                    // 본문
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 11f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("상대방의 킹을 먼저 잡는 플레이어가 승리합니다.\n\n");

                    // 사진 부가 설명
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 10f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("[사진 설명]\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 10f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 위쪽 사진: 게임 시작 시 기물 초기 배치\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 10f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 아래쪽 사진: 상대방의 킹을 잡아 승리 조건을 달성한 상황\n");

                    break;

                case "Turn":
                    rtbDescription.AppendText("🔄 턴(Turn) 진행 방식\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("내 턴이 시작되면 아래의 순서대로 진행합니다.\n\n");

                    // 소제목 1
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.LightSkyBlue;
                    rtbDescription.AppendText("1. 카드 드로우\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("내 턴이 시작되면 덱에서 카드를 한 장 뽑습니다.\n\n");

                    // 소제목 2
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.LightGreen;
                    rtbDescription.AppendText("2. 마법 카드 시전 (선택)\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("손패의 카드를 사용합니다. (위쪽 사진 참고)\n\n");

                    // 소제목 3
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.OrangeRed;
                    rtbDescription.AppendText("3. 기물 이동/공격 (필수)\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("기물을 이동하거나 적을 공격합니다. (아래쪽 사진 참고)\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("기물을 조작하면 나의 행동이 끝나고 자동으로 턴이 종료됩니다.\n");
                    break;

                case "Control":
                    rtbDescription.AppendText("🖱️ 조작법 & UI 안내\n");

                    // 소제목 1
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.LightSkyBlue;
                    rtbDescription.AppendText("1. 마법 카드 시전\n");

                    // 본문
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 11f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("손패(화면 하단)에 있는 카드를 클릭한 뒤, 대상을 향해 드래그하거나, 손패에 있는 카드의 단축키 번호를 누르고 대상을 클릭하여 시전합니다.\n");
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("(조건이 맞지 않는 대상을 클릭하면 카드가 발동하지 않고 취소됩니다.)\n\n");

                    // 소제목 2
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.LightGreen;
                    rtbDescription.AppendText("2. 기물 이동 및 공격\n");

                    // 본문
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 11f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("보드 위의 아군 기물을 클릭하면 이동 가능한 범위가 파란색으로 표시되고, 공격 가능한 적 기물은 빨간색으로 표시됩니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 11f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("이후 표시된 칸 중 하나를 클릭하면 해당 위치로 이동하거나 적을 공격합니다.\n\n");

                    // 사진 부가 설명
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 10f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("[사진 설명]\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 10f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 위쪽 사진: 카드를 사용하는 조작 화면\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 10f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 아래쪽 사진: 기물을 클릭했을 때의 이동 및 공격 범위 표시\n");
                    break;
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
