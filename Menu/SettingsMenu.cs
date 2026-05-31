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
            this.Size = new Size(400, 440);
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
            int currentY = 25;
            int gap = 65;

            // 1. 전체화면 / 창모드 전환 버튼
            Button btnScreen = CreateImageButton("button_long_screen_change.png", "전체화면 / 창모드", currentY);
            currentY += gap;
            btnScreen.Click += (s, e) =>
            {
                if (parentForm.FormBorderStyle == FormBorderStyle.None)
                {
                    parentForm.FormBorderStyle = FormBorderStyle.Sizable;
                    parentForm.WindowState = FormWindowState.Normal;

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

            // 2. BGM 끄기 / 켜기 버튼 (이미지 동적 전환)

            string initialBgmImg = isBgmOn ? "button_long_bgm_off.png" : "button_long_bgm_on.png";
            Button btnSound = CreateImageButton(initialBgmImg, isBgmOn ? "BGM 끄기" : "BGM 켜기", currentY);
            currentY += gap;

            btnSound.Click += (s, e) =>
            {
                isBgmOn = !isBgmOn;

                if (isBgmOn)
                {
                    SoundsManager.PlayBGM("bg_music");
                    UpdateImageButton(btnSound, "button_long_bgm_off.png", "BGM 끄기");
                }
                else
                {
                    SoundsManager.StopBGM();
                    UpdateImageButton(btnSound, "button_long_bgm_on.png", "BGM 켜기");
                }
            };

            // 3. 항복 버튼 (게임 중일 때만 표시)
            if (isInGame)
            {
                // 텍스트를 "항복"으로 간결하게 변경
                Button btnLobby = CreateImageButton("button_long_surrender.png", "항복", currentY);
                currentY += gap;

                // 마우스를 올렸을 때 부가 설명(Tooltip) 표시
                ToolTip toolTip = new ToolTip();
                toolTip.SetToolTip(btnLobby, "메인 화면으로 돌아가며 패배 처리됩니다.");

                btnLobby.Click += (s, e) =>
                {
                    DialogResult result = MessageBox.Show("항복하시겠습니까?\n메인 화면으로 돌아가며 패배 처리됩니다.", "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        if (udp != null && udp.IsConnected) udp.Send("SURRENDER");
                        this.Close();
                        parentForm.Close();
                    }
                };
            }

            // 4. 닫기 버튼
            Button btnClose = CreateImageButton("button_long_close_menu.png", "닫기", currentY);
            currentY += gap;
            btnClose.Click += (s, e) => this.Close();

            // 5. 마스터 볼륨 조절
            currentY += 10;

            Label lblMasterVol = CreateLabel($"🔊 게임 음량: {(int)(SoundsManager.MasterVolume * 100)}%", currentY);

            TrackBar trackMaster = CreateTrackBar((int)(SoundsManager.MasterVolume * 100), currentY + 25);
            trackMaster.Scroll += (s, e) =>
            {
                SoundsManager.MasterVolume = trackMaster.Value / 100f;
                lblMasterVol.Text = $"🔊 게임 음량: {trackMaster.Value}%";
            };

            this.ClientSize = new Size(this.ClientSize.Width, currentY + 120);
        }

        // 이미지 버튼 생성기
        private Button CreateImageButton(string imgName, string fallbackText, int yPos)
        {
            Button btn = new Button();
            btn.Size = new Size(260, 50);
            btn.Location = new Point((this.ClientSize.Width - 260) / 2, yPos);

            // 이미지 버튼의 배경/테두리 투명화 처리
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.BackColor = Color.Transparent;
            btn.Cursor = Cursors.Hand;

            UpdateImageButton(btn, imgName, fallbackText);

            this.Controls.Add(btn);
            return btn;
        }

        // 버튼 이미지 갈아끼우기 (BGM 토글 등에서 재사용)
        private void UpdateImageButton(Button btn, string imgName, string fallbackText)
        {
            string imgPath = Path.Combine(Application.StartupPath, "Assets", imgName);
            if (File.Exists(imgPath))
            {
                btn.BackgroundImage = Image.FromFile(imgPath);
                btn.BackgroundImageLayout = ImageLayout.Zoom;
                btn.Text = ""; // 이미지가 정상 로드되면 텍스트 제거
            }
            else
            {
                // 이미지가 없을 때를 대비한 텍스트 렌더링
                btn.Text = fallbackText;
                btn.Font = new Font("맑은 고딕", 12f, FontStyle.Bold);
                btn.ForeColor = Color.White;
                btn.BackColor = Color.FromArgb(180, 30, 30, 30);
            }
        }

        // 픽쳐박스 안전 로드 (타이틀용)
        private void LoadImageSafe(PictureBox pic, string imgName)
        {
            string imgPath = Path.Combine(Application.StartupPath, "Assets", imgName);
            if (File.Exists(imgPath)) pic.Image = Image.FromFile(imgPath);
        }

        private Label CreateLabel(string text, int yPos)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Size = new Size(260, 20);
            lbl.Location = new Point((this.ClientSize.Width - 260) / 2 + 5, yPos);
            lbl.Font = new Font("맑은 고딕", 10f, FontStyle.Bold);
            lbl.ForeColor = Color.White;
            lbl.BackColor = Color.Transparent;
            this.Controls.Add(lbl);
            return lbl;
        }

        private TrackBar CreateTrackBar(int currentVal, int yPos)
        {
            TrackBar track = new TrackBar();
            track.Minimum = 0;
            track.Maximum = 100;
            track.Value = currentVal;
            track.Size = new Size(260, 30);
            track.Location = new Point((this.ClientSize.Width - 260) / 2, yPos);
            track.TickStyle = TickStyle.None;
            track.BackColor = Color.FromArgb(40, 40, 40);

            track.Cursor = Cursors.Hand;
            this.Controls.Add(track);
            return track;
        }
        

    }
}