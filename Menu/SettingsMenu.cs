using CardChess.Core;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using CardChess.Networking;

namespace CardChess.Menu
{
    public class SettingsMenu : Form
    {
        private Form parentForm;
        private bool isInGame;
        private SignalRProtocol network;

        private sealed class WindowPlacement
        {
            public Rectangle Bounds;
            public FormBorderStyle BorderStyle;
            public FormWindowState WindowState;
        }

        private static readonly ConditionalWeakTable<Form, WindowPlacement> windowPlacements
            = new ConditionalWeakTable<Form, WindowPlacement>();

        // 배경음악 온오프 관련
        private static bool isBgmOn = true;

        public SettingsMenu(Form parent, bool inGame, SignalRProtocol networkProtocol = null)
        {
            this.parentForm = parent;
            this.isInGame = inGame;
            this.network = networkProtocol;

            // 환경 설정 UI 기본값 세팅
            this.Text = "환경 설정";
            this.Size = new Size(400, 440);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

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
                    this.BackColor = Color.FromArgb(40, 40, 40);
                }
            }
            catch { }

            InitializeUI();
        }

        private void InitializeUI()
        {
            int currentY = 25;
            int gap = 65;

            // 전체화면 및 창모드 전환 버튼
            Button btnScreen = CreateImageButton("button_long_screen_change.png", "전체화면 / 창모드", currentY);
            currentY += gap;
            btnScreen.Click += (s, e) =>
            {
                if (parentForm.FormBorderStyle == FormBorderStyle.None)
                {
                    parentForm.WindowState = FormWindowState.Normal;
                    WindowPlacement placement;
                    if (windowPlacements.TryGetValue(parentForm, out placement))
                    {
                        parentForm.FormBorderStyle = placement.BorderStyle;
                        parentForm.Bounds = placement.Bounds;
                        parentForm.WindowState = placement.WindowState == FormWindowState.Minimized
                            ? FormWindowState.Normal
                            : placement.WindowState;
                    }
                    else
                    {
                        parentForm.FormBorderStyle = FormBorderStyle.Sizable;
                        Rectangle screen = Screen.FromControl(parentForm).WorkingArea;
                        parentForm.Location = new Point(
                            screen.X + (screen.Width - parentForm.Width) / 2,
                            screen.Y + (screen.Height - parentForm.Height) / 2);
                    }
                }
                else
                {
                    Rectangle windowBounds = parentForm.WindowState == FormWindowState.Normal
                        ? parentForm.Bounds
                        : parentForm.RestoreBounds;
                    WindowPlacement placement = new WindowPlacement
                    {
                        Bounds = windowBounds,
                        BorderStyle = parentForm.FormBorderStyle,
                        WindowState = parentForm.WindowState
                    };
                    windowPlacements.Remove(parentForm);
                    windowPlacements.Add(parentForm, placement);

                    Rectangle screenBounds = Screen.FromControl(parentForm).Bounds;
                    parentForm.WindowState = FormWindowState.Normal;
                    parentForm.FormBorderStyle = FormBorderStyle.None;
                    parentForm.Bounds = screenBounds;
                }
            };

            // 배경음악 온오프 관리 버튼

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

            // 서렌더 버튼으로 이벤트 발생 시 상대방에게 패킷 송신 후 게임 종료 | 마우스 올리면 텍스트 노출
            if (isInGame)
            {
                Button btnLobby = CreateImageButton("button_long_surrender.png", "항복", currentY);
                currentY += gap;

                ToolTip toolTip = new ToolTip();
                toolTip.SetToolTip(btnLobby, "메인 화면으로 돌아가며 패배 처리됩니다.");

                btnLobby.Click += (s, e) =>
                {
                    DialogResult result = MessageBox.Show("항복하시겠습니까?\n메인 화면으로 돌아가며 패배 처리됩니다.", "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        this.Close();
                        MainForm mainForm = parentForm as MainForm;
                        if (mainForm != null)
                            mainForm.SurrenderAndClose();
                        else
                        {
                            if (network != null && network.IsConnected) network.Send("SURRENDER");
                            parentForm.Close();
                        }
                    }
                };
            }

            // 설정 창 폼 닫기 버튼
            Button btnClose = CreateImageButton("button_long_close_menu.png", "닫기", currentY);
            currentY += gap;
            btnClose.Click += (s, e) => this.Close();

            // 배경음악 볼륨을 트랙바 형태에 묶어서 음량 조절
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

        // 기본 윈도우 폼 버튼 속성
        private Button CreateImageButton(string imgName, string fallbackText, int yPos)
        {
            Button btn = new Button();
            btn.Size = new Size(260, 50);
            btn.Location = new Point((this.ClientSize.Width - 260) / 2, yPos);

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

        // 버튼에 이미지를 덧씌운 후 텍스트 제거 | 이미지가 없으면 텍스트를 노출
        private void UpdateImageButton(Button btn, string imgName, string fallbackText)
        {
            string imgPath = Path.Combine(Application.StartupPath, "Assets", imgName);
            if (File.Exists(imgPath))
            {
                btn.BackgroundImage = Image.FromFile(imgPath);
                btn.BackgroundImageLayout = ImageLayout.Zoom;
                btn.Text = "";
            }
            else
            {
                btn.Text = fallbackText;
                btn.Font = new Font("맑은 고딕", 12f, FontStyle.Bold);
                btn.ForeColor = Color.White;
                btn.BackColor = Color.FromArgb(180, 30, 30, 30);
            }
        }

        //private void LoadImageSafe(PictureBox pic, string imgName)
        //{
        //    string imgPath = Path.Combine(Application.StartupPath, "Assets", imgName);
        //    if (File.Exists(imgPath)) pic.Image = Image.FromFile(imgPath);
        //}

        // 볼륨 조절용 라벨 및 트랙바 속성값

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
