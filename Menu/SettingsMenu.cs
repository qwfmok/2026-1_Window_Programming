using CardChess.Core;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CardChess.Menu
{
    public class SettingsMenu : Form
    {
        private Form parentForm;
        private bool isInGame;
        private UDPprotocol udp;

        // 🌟 BGM이 켜져 있는지 꺼져 있는지 기억하는 변수 (창을 닫아도 유지됨)
        private static bool isBgmOn = true;

        public SettingsMenu(Form parent, bool inGame, UDPprotocol udpProtocol = null)
        {
            this.parentForm = parent;
            this.isInGame = inGame;
            this.udp = udpProtocol;

            // 설정창 기본 세팅
            this.Text = "환경 설정";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 🌟 메인 화면 배경 가져다 쓰기 (글자 없는 깔끔한 배경 사용)
            try
            {
                string bgPath = Path.Combine(Application.StartupPath, "Assets", "bg_remove_text.png");
                if (File.Exists(bgPath))
                {
                    this.BackgroundImage = Image.FromFile(bgPath);
                    this.BackgroundImageLayout = ImageLayout.Stretch;
                }
                else
                {
                    this.BackColor = Color.FromArgb(40, 40, 40); // 실패 시 다크 테마
                }
            }
            catch { }

            InitializeUI();
        }

        private void InitializeUI()
        {
            int startY = 30;
            int gap = 70;

            // 1. 전체화면 / 창모드 전환 버튼
            Button btnScreen = CreateButton("🖥️ 전체화면 / 창모드", startY);
            btnScreen.Click += (s, e) =>
            {
                if (parentForm.FormBorderStyle == FormBorderStyle.None)
                {
                    parentForm.FormBorderStyle = FormBorderStyle.Sizable;
                    parentForm.WindowState = FormWindowState.Normal;

                    // 모니터 정중앙으로 좌표 계산 후 이동
                    Rectangle screen = Screen.FromControl(parentForm).WorkingArea;
                    parentForm.Location = new Point(
                        screen.X + (screen.Width - parentForm.Width) / 2,
                        screen.Y + (screen.Height - parentForm.Height) / 2
                    );
                }
                else
                {
                    parentForm.FormBorderStyle = FormBorderStyle.None;
                    parentForm.WindowState = FormWindowState.Maximized;
                }
            };

            // 2. 🌟 BGM 끄기 / 켜기 버튼 (알림창 없이 즉시 토글)
            Button btnSound = CreateButton(isBgmOn ? "🔊 BGM 끄기" : "🔇 BGM 켜기", startY + gap);
            btnSound.Click += (s, e) =>
            {
                isBgmOn = !isBgmOn; // 상태 뒤집기

                if (isBgmOn)
                {
                    SoundsManager.PlayBGM("bg_music");
                    btnSound.Text = "🔊 BGM 끄기";
                }
                else
                {
                    SoundsManager.StopBGM("bg_music");
                    btnSound.Text = "🔇 BGM 켜기";
                }
            };

            // 3. 메인으로 돌아가기 (게임 중일 때만 표시)
            if (isInGame)
            {
                Button btnLobby = CreateButton("🏃 메인으로 (항복)", startY + gap * 2);
                btnLobby.BackColor = Color.FromArgb(200, 205, 92, 92); // 눈에 띄는 붉은색 반투명
                btnLobby.Click += (s, e) =>
                {
                    DialogResult result = MessageBox.Show("메인 화면으로 돌아가시겠습니까?\n진행 중인 게임은 패배(항복) 처리됩니다.", "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        if (udp != null && udp.IsConnected) udp.Send("SURRENDER");
                        this.Close();
                        parentForm.Close();
                    }
                };
            }

            // 4. 닫기 버튼
            Button btnClose = CreateButton("❌ 닫기", startY + gap * 3);
            btnClose.Click += (s, e) => this.Close();
        }

        // 🌟 배경이 보이도록 버튼을 '반투명'하게 만드는 헬퍼 함수
        private Button CreateButton(string text, int yPos)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(260, 50);
            btn.Location = new Point((this.ClientSize.Width - 260) / 2, yPos);
            btn.Font = new Font("맑은 고딕", 12f, FontStyle.Bold);
            btn.FlatStyle = FlatStyle.Flat;
            btn.ForeColor = Color.White;
            btn.BackColor = Color.FromArgb(180, 30, 30, 30); // 180의 투명도를 가진 다크그레이 (배경이 비침)
            btn.FlatAppearance.BorderSize = 0; // 테두리 제거로 깔끔하게
            btn.Cursor = Cursors.Hand;
            this.Controls.Add(btn);
            return btn;
        }
    }
}