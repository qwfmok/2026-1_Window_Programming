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
    public partial class CardManual : Form
    {
        private Image backgroundBg;
        private Image imgBack;
        public CardManual()
        {
            InitializeComponent();
            // 창 크기 및 기본 설정
            this.Width = 1600;
            this.Height = 900;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            listBoxCards.BorderStyle = BorderStyle.None; // 촌스러운 기본 테두리 제거
            listBoxCards.BackColor = Color.FromArgb(30, 30, 30); // 배경을 어두운 쥐색으로
            listBoxCards.ForeColor = Color.White; // 기본 글자색은 흰색
            listBoxCards.Font = new Font("맑은 고딕", 14f, FontStyle.Bold); // 폰트 크기 키우기
            listBoxCards.ItemHeight = 45; // 항목들 사이의 간격을 널널하게(위아래 여백)
            this.BackgroundImageLayout = ImageLayout.Stretch;

            LoadGameAssets();
            // 폼이 켜질 때 ListBox에 17개 카드 이름 채워넣기
            InitializeCardList();
            // 카드 이미지가 잘리지 않게 픽쳐박스 크기에 맞춰 비율대로 축소
            picCard.SizeMode = PictureBoxSizeMode.Zoom;
            // 처음 켜졌을 때 맨 위에 있는 카드(기사 진화)가 자동 선택되도록 세팅
            if (listBoxCards.Items.Count > 0)
            {
                listBoxCards.SelectedIndex = 0;
            }
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
            // (주의: 디자이너에 올린 버튼 이름이 다르면 아래 이름들을 맞춰주세요!)
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

        // 리스트박스에 17개 카드 이름 밀어넣기
        private void InitializeCardList()
        {
            listBoxCards.Items.Clear(); // 찌꺼기 방지용 초기화

            // 진화 카드 (3종)
            listBoxCards.Items.Add("기사 진화");
            listBoxCards.Items.Add("룩 진화");
            listBoxCards.Items.Add("비숍 진화");

            // 필드 스킬 (2종)
            listBoxCards.Items.Add("방벽 건설");
            listBoxCards.Items.Add("증원");

            // 액티브 스킬 (5종)
            listBoxCards.Items.Add("두장 뽑기");
            listBoxCards.Items.Add("손패 교환");
            listBoxCards.Items.Add("카드 뺏기");
            listBoxCards.Items.Add("시간 왜곡");
            listBoxCards.Items.Add("랜덤 시전");

            // 타겟 스킬 (6종)
            listBoxCards.Items.Add("부활");
            listBoxCards.Items.Add("기물 뺏기");
            listBoxCards.Items.Add("위치 교환");
            listBoxCards.Items.Add("봉인");
            listBoxCards.Items.Add("복제");
            listBoxCards.Items.Add("랜덤 진화");

            // 트랩 카드 (1종)
            listBoxCards.Items.Add("랜덤 방어");
        }
        // 유저가 ListBox에서 카드를 클릭(선택)했을 때 발동하는 이벤트
        private void listBoxCards_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 아무것도 선택 안 된 상태면 무시
            if (listBoxCards.SelectedItem == null) return;

            // 클릭한 카드 이름 가져오기
            string selectedCard = listBoxCards.SelectedItem.ToString();

            // 해당 카드 이름에 맞춰서 이미지와 설명 갈아끼우기!
            LoadCardData(selectedCard);
        }

        // 카드 이름에 맞춰 데이터(사진+글)를 갈아 끼우는 함수
        private void LoadCardData(string cardName)
        {
            string assetsPath = Path.Combine(Application.StartupPath, "Assets");

            try
            {
                string frontImg = "";

                // 나중에 Assets 폴더에 들어갈 실제 파일명으로 수정해주시면 됩니다!
                switch (cardName)
                {
                    // 🔹 진화 카드 (3종)
                    case "기사 진화": frontImg = "card_1_evo_knight.png"; break;
                    case "룩 진화": frontImg = "card_1_evo_rook.png"; break;
                    case "비숍 진화": frontImg = "card_1_evo_bishop.png"; break;

                    // 🌿 필드 스킬 (2종)
                    case "방벽 건설": frontImg = "card_1_wall.png"; break;
                    case "증원": frontImg = "card_1_reinforced.png"; break;

                    // 💥 액티브 스킬 (5종)
                    case "두장 뽑기": frontImg = "card_1_draw_two.png"; break;
                    case "손패 교환": frontImg = "card_1_redraw.png"; break;
                    case "카드 뺏기": frontImg = "card_1_steal_card.png"; break;
                    case "시간 왜곡": frontImg = "card_1_time_warp.png"; break;
                    case "랜덤 시전": frontImg = "card_1_random_cast.png"; break;

                    // 🎯타겟 스킬 (6종)
                    case "부활": frontImg = "card_1_revival.png"; break;
                    case "기물 뺏기": frontImg = "card_1_steal_piece.png"; break;
                    case "위치 교환": frontImg = "card_1_swap.png"; break;
                    case "봉인": frontImg = "card_1_seal.png"; break;
                    case "복제": frontImg = "card_1_clone.png"; break;
                    case "랜덤 진화": frontImg = "card_1_random_evo.png"; break;

                    // 🪤 트랩 카드
                    case "랜덤 방어": frontImg = "card_1_random_defend.png"; break;

                    // 에러 방지용 기본 이미지
                    default: frontImg = "card_1_placeholder.png"; break;
                }

                // 이미지 교체
                string frontPath = Path.Combine(assetsPath, frontImg);

                if (File.Exists(frontPath)) picCard.Image = Image.FromFile(frontPath);

                // 설명 텍스트 작성
                SetDescription(cardName);
            }
            catch (Exception) { /* 무시 */ }
        }

        // 카드 상세 설명 텍스트를 다시 쓰는 함수
        private void SetDescription(string cardName)
        {
            rtbDescription.Clear();

            // 타이틀
            rtbDescription.SelectionFont = new Font("맑은 고딕", 24f, FontStyle.Bold);
            rtbDescription.SelectionColor = Color.Gold;
            rtbDescription.AppendText($"🃏 {cardName}\n\n");

            rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
            rtbDescription.SelectionColor = Color.White;

            switch (cardName)
            {
                // 🔹 진화 카드 (LightSkyBlue)
                case "기사 진화":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.LightSkyBlue;
                    rtbDescription.AppendText("[진화 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 보드 위의 폰을 나이트(Knight)로 진화시킵니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 장애물을 뛰어넘는 변칙적인 L자 공격으로 상대의 허를 찌를 때 유용한 카드입니다.");
                    break;

                case "룩 진화":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.LightSkyBlue;
                    rtbDescription.AppendText("[진화 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 보드 위의 폰을 룩(Rook)으로 진화시킵니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 직선상의 적을 멀리서도 위협할 수 있어 라인 장악력이 크게 상승합니다.");
                    break;

                case "비숍 진화":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.LightSkyBlue;
                    rtbDescription.AppendText("[진화 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 보드 위의 폰을 비숍(Bishop)으로 진화시킵니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 대각선으로 빠르게 침투하여 상대 진영을 붕괴시키는 데 특화되어 있습니다.");
                    break;
                // 🌿 필드 스킬 카드 (LightGreen)
                case "방벽 건설":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.LightGreen;
                    rtbDescription.AppendText("[필드 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 지정한 빈칸에 2턴간 유지되는 벽을 생성합니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 적 기물의 이동 경로를 차단하여 킹을 방어하거나, 적의 퇴로를 막고 포위할 때 사용합니다.");
                    break;

                case "증원":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.LightGreen;
                    rtbDescription.AppendText("[필드 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 지정한 빈칸에 내 폰 1기를 즉시 소환합니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 부족한 전력을 보충하거나, 상대의 치명적인 공격을 대신 맞아줄 방패막이를 세울 수 있습니다.");
                    break;
                // 💥 액티브 스킬 카드 (OrangeRed)
                case "두장 뽑기":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.OrangeRed;
                    rtbDescription.AppendText("[액티브 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 덱에서 카드를 2장 뽑습니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 손패를 보충하여 다음 턴의 선택지를 크게 넓혀주는 강력한 드로우 카드입니다.");
                    break;

                case "손패 교환":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.OrangeRed;
                    rtbDescription.AppendText("[액티브 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 현재 손패를 모두 버리고 덱에서 새로 카드를 뽑습니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 당장 쓸모없는 카드만 잡혔을 때, 위기를 돌파할 핵심 카드를 찾기 위한 승부수입니다.");
                    break;

                case "카드 뺏기":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.OrangeRed;
                    rtbDescription.AppendText("[액티브 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 상대의 손패 1장을 무작위로 빼앗아옵니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 내 전력을 강화함과 동시에 상대의 핵심 카드를 훔쳐 심리적인 타격을 줍니다.");
                    break;

                case "시간 왜곡":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.OrangeRed;
                    rtbDescription.AppendText("[액티브 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 상대의 다음 턴을 스킵합니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 나에게 2번의 턴이 주어지는 것과 같으며, 체크메이트를 강제할 수 있는 최상위 마법입니다.");
                    break;

                case "랜덤 시전":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.OrangeRed;
                    rtbDescription.AppendText("[액티브 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 덱에서 카드 2장을 무작위로 즉시 시전합니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 어떤 마법이 발동될지 알 수 없지만, 절망적인 상황을 일발 역전시킬 수 있는 도박수입니다.");
                    break;
                // 🎯 타겟 스킬 카드 (Plum)
                case "부활":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.Plum;
                    rtbDescription.AppendText("[타겟 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 내 진영의 빈칸에 죽은 기물을 부활시킵니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 초반에 잃어버린 핵심 기물(퀸, 룩 등)을 살려내어 전황을 다시 팽팽하게 만듭니다.");
                    break;

                case "기물 뺏기":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.Plum;
                    rtbDescription.AppendText("[타겟 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 타겟으로 지정한 상대 기물 1개의 소유권을 강탈합니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 제한: 킹과 퀸은 강탈할 수 없으며, 상대 진영의 시작 지점(첫 두 줄)에 있는 기물에는 사용할 수 없습니다.\n");
                    rtbDescription.AppendText("• 상세: 제한 구역을 벗어난 상대 기물을 내 것으로 만들어 게임의 판도를 뒤집는 카드입니다.");
                    break;

                case "위치 교환":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.Plum;
                    rtbDescription.AppendText("[타겟 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 지정한 타겟을 내 무작위 기물과 위치 교환합니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 적의 공격에 노출된 기물을 구출하거나, 아군 기물을 적진 한복판에 기습 투하할 때 사용합니다.");
                    break;

                case "봉인":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.Plum;
                    rtbDescription.AppendText("[타겟 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 타겟 기물을 1턴 동안 무적 및 행동 불가 상태로 만듭니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 상대의 핵심 기물을 묶어두거나, 반대로 내 킹에게 사용하여 1턴 간 죽음을 회피하는 생존기로도 쓰입니다.");
                    break;

                case "복제":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.Plum;
                    rtbDescription.AppendText("[타겟 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 타겟 기물을 인접한 빈칸 중 1곳에 복제합니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 강력한 진화 기물이나 퀸을 복제하여 압도적인 화력 차이를 만들어냅니다.");
                    break;

                case "랜덤 진화":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.Plum;
                    rtbDescription.AppendText("[타겟 스킬 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 내 기물을 킹을 제외한 무작위 기물로 변이시킵니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 내 폰이나 진화 기물을 무작위로 변이시켜 전황을 바꾸는 카드이며 상대 기물에는 사용할 수 없습니다.");
                    break;
                // 🪤 트랩 카드 (Khaki)
                case "랜덤 방어":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.Khaki;
                    rtbDescription.AppendText("[트랩 카드]\n\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 효과: 상대의 공격을 50% 확률로 무효화하고 반사하여 파괴합니다.\n");
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 12f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 상세: 덫을 설치하여 상대가 감히 나의 기물을 공격하지 못하도록 주저하게 만드는 심리전 카드입니다.");
                    break;

                default:
                    rtbDescription.AppendText("해당 카드의 상세 설명이 아직 업데이트되지 않았습니다.");
                    break;
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
