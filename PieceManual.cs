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

namespace CardChess
{
    public partial class PieceManual : Form
    {
        private Image backgroundBg;
        private Image imgBack;
        private Image imgPawn;
        private Image imgBishop;
        private Image imgKnight;
        private Image imgRook;
        private Image imgQueen;
        private Image imgKing;

        public PieceManual()
        {
            InitializeComponent();
            // 창 크기 및 기본 설정
            this.Width = 1600;
            this.Height = 900;
            this.DoubleBuffered = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackgroundImageLayout = ImageLayout.Stretch;
            LoadGameAssets();
            // 폼이 처음 켜졌을 때는 기본으로 '폰' 탭을 띄워줍니다.
            LoadPieceData("Pawn");
            // 픽쳐박스 3개 모두 비율에 맞춰 축소 (짤림 방지)
            picTopLeft.SizeMode = PictureBoxSizeMode.Zoom;
            picTopRight.SizeMode = PictureBoxSizeMode.Zoom;
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

        }
        private void LoadGameAssets()
        {
            try
            {
                string assetsPath = Path.Combine(Application.StartupPath, "Assets");

                backgroundBg = Image.FromFile(Path.Combine(assetsPath, "bg_remove_text.png"));
                imgBack = Image.FromFile(Path.Combine(assetsPath, "button_back.png"));
                imgPawn = Image.FromFile(Path.Combine(assetsPath, "btn_pawn.png"));
                imgBishop = Image.FromFile(Path.Combine(assetsPath, "btn_bishop.png"));
                imgKnight = Image.FromFile(Path.Combine(assetsPath, "btn_knight.png"));
                imgRook = Image.FromFile(Path.Combine(assetsPath, "btn_rook.png"));
                imgQueen = Image.FromFile(Path.Combine(assetsPath, "btn_queen.png"));
                imgKing = Image.FromFile(Path.Combine(assetsPath, "btn_king.png"));
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
            SetupImageButton(btnTabPawn, imgPawn);
            SetupImageButton(btnTabBishop, imgBishop);
            SetupImageButton(btnTabKnight, imgKnight);
            SetupImageButton(btnTabRook, imgRook);
            SetupImageButton(btnTabQueen, imgQueen);
            SetupImageButton(btnTabKing, imgKing);
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
        // 상단 탭(버튼) 클릭 이벤트들
        private void btnTabPawn_Click(object sender, EventArgs e) { LoadPieceData("Pawn"); }
        private void btnTabKnight_Click(object sender, EventArgs e) { LoadPieceData("Knight"); }
        private void btnTabBishop_Click(object sender, EventArgs e) { LoadPieceData("Bishop"); }
        private void btnTabRook_Click(object sender, EventArgs e) { LoadPieceData("Rook"); }
        private void btnTabQueen_Click(object sender, EventArgs e) { LoadPieceData("Queen"); }
        private void btnTabKing_Click(object sender, EventArgs e) { LoadPieceData("King"); }

        // 탭에 맞춰 데이터(사진+글)를 갈아 끼우는 마법의 함수
        private void LoadPieceData(string pieceType)
        {
            string assetsPath = Path.Combine(Application.StartupPath, "Assets");

            try
            {
                // 1. 탭 이름에 맞춰 불러올 이미지 파일명 결정
                string p1Img = "", p2Img = "", movementImg = "";

                switch (pieceType)
                {
                    // ♟️ 폰 (Pawn)
                    case "Pawn":
                        p1Img = "Player1_Pawn_num_1.png";
                        p2Img = "Player2_Pawn_num_1.png";
                        movementImg = "pawn_move.png"; // 폰 이동 방향 사진
                        break;

                    // ♞ 나이트 (Knight)
                    case "Knight":
                        p1Img = "Player1_Knight_num_1.png";
                        p2Img = "Player2_Knight_num_1.png";
                        movementImg = "knight_move.png"; // 나이트 이동 방향 사진
                        break;

                    // ♝ 비숍 (Bishop)
                    case "Bishop":
                        p1Img = "Player1_Bishop_num_1.png";
                        p2Img = "Player2_Bishop_num_1.png";
                        movementImg = "bishop_move.png"; // 비숍 이동 방향 사진
                        break;

                    // ♜ 룩 (Rook)
                    case "Rook":
                        p1Img = "Player1_Rook_num_1.png";
                        p2Img = "Player2_Rook_num_1.png";
                        movementImg = "rook_move.png"; // 룩 이동 방향 사진
                        break;

                    // ♛ 퀸 (Queen)
                    case "Queen":
                        p1Img = "Player1_Queen_num_1.png";
                        p2Img = "Player2_Queen_num_1.png";
                        movementImg = "queen_move.png"; // 퀸 이동 방향 사진
                        break;

                    // ♚ 킹 (King)
                    case "King":
                        p1Img = "Player1_King_num_1.png";
                        p2Img = "Player2_King_num_1.png";
                        movementImg = "king_move.png"; // 킹 이동 방향 사진
                        break;

                    // 🚨 에러 방지용 기본값
                    default:
                        p1Img = "placeholder.png";
                        p2Img = "placeholder.png";
                        movementImg = "placeholder_move.png";
                        break;
                }

                // 2. PictureBox에 이미지 교체 (기존 이미지는 지우고 새 이미지 장착!)
                string p1Path = Path.Combine(assetsPath, p1Img);
                string p2Path = Path.Combine(assetsPath, p2Img);
                string gifPath = Path.Combine(assetsPath, movementImg);

                if (File.Exists(p1Path)) picTopLeft.Image = Image.FromFile(p1Path);
                if (File.Exists(p2Path)) picTopRight.Image = Image.FromFile(p2Path);
                if (File.Exists(gifPath)) picBottom.Image = Image.FromFile(gifPath);

                // 3. 텍스트 교체 호출
                SetDescription(pieceType);
            }
            catch (Exception ex)
            {
                // 로드 에러 무시
            }
        }
        // 탭에 맞춰 상세 설명 텍스트를 다시 쓰는 함수
        private void SetDescription(string pieceType)
        {
            rtbDescription.Clear(); // 이전 기물 텍스트 싹 비우기

            rtbDescription.SelectionFont = new Font("맑은 고딕", 24f, FontStyle.Bold);
            rtbDescription.SelectionColor = Color.Gold;

            switch (pieceType)
            {
                // ♟️ 폰 (Pawn)
                case "Pawn":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 24f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.Gold;
                    rtbDescription.AppendText("♟️ 폰 (Pawn)\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 이동: 앞으로 딱 1칸만 전진할 수 있습니다. (단, 첫 이동 시에는 2칸 전진 가능)\n");
                    rtbDescription.AppendText("• 공격: 대각선 앞쪽 1칸에 있는 적만 공격할 수 있습니다.\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 13f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.LightGreen; // 특수 능력 포인트 컬러
                    rtbDescription.AppendText("• 특수 능력 (승급): ");
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("카드를 통해 언제든 진화가 가능합니다.\n");
                    break;

                // ♞ 나이트 (Knight)
                case "Knight":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 24f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.Gold;
                    rtbDescription.AppendText("♞ 나이트 (Knight)\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 이동 및 공격: 알파벳 'L'자 모양(직선 2칸 이동 후 꺾어서 1칸)으로 이동하며, 도착 지점의 적을 공격합니다.\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 13f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.LightSkyBlue;
                    rtbDescription.AppendText("• 특수 능력 (도약): ");
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("모든 기물 중 유일하게 이동 경로에 있는 아군이나 적군 기물을 훌쩍 뛰어넘을 수 있습니다. 방어벽을 무시하고 적의 허를 찌르는 데 특화되어 있습니다.\n");
                    break;

                // ♝ 비숍 (Bishop)
                case "Bishop":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 24f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.Gold;
                    rtbDescription.AppendText("♝ 비숍 (Bishop)\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 이동 및 공격: 대각선 방향으로 장애물이 없는 한 원하는 만큼 몇 칸이든 이동 및 공격할 수 있습니다.\n");
                    rtbDescription.AppendText("• 상세: 십자 방향(가로/세로)으로는 움직일 수 없지만, 복잡하게 얽힌 전장에서 대각선 틈새를 파고들어 적을 저격하는 데 탁월합니다.\n");
                    break;

                // ♜ 룩 (Rook)
                case "Rook":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 24f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.Gold;
                    rtbDescription.AppendText("♜ 룩 (Rook)\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 이동 및 공격: 가로 및 세로 방향으로 장애물이 없는 한 원하는 만큼 직선 이동 및 공격이 가능합니다.\n");
                    rtbDescription.AppendText("• 상세: 대각선으로는 움직일 수 없지만, 전장이 열렸을 때 일직선상의 모든 적을 위협하는 든든한 공성탑앗 같은 역할을 합니다.\n");
                    break;

                // ♛ 퀸 (Queen)
                case "Queen":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 24f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.Gold;
                    rtbDescription.AppendText("♛ 퀸 (Queen)\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 이동 및 공격: 가로, 세로, 대각선 모든 방향으로 장애물이 없는 한 원하는 만큼 이동 및 공격할 수 있습니다.\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 13f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.OrangeRed;
                    rtbDescription.AppendText("• 특수 능력 (최강의 화력): ");
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("룩의 직선 이동과 비숍의 대각선 이동을 모두 합친 최고의 기물입니다. 퀸을 얼마나 잘 보호하고 활용하느냐에 따라 승패가 갈립니다.\n");
                    break;

                // ♚ 킹 (King)
                case "King":
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 24f, FontStyle.Bold);
                    rtbDescription.SelectionColor = Color.Gold;
                    rtbDescription.AppendText("♚ 킹 (King)\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 14f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("• 이동 및 공격: 가로, 세로, 대각선 모든 방향으로 딱 1칸씩만 이동 및 공격할 수 있습니다.\n\n");

                    rtbDescription.SelectionFont = new Font("맑은 고딕", 13f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.OrangeRed;
                    rtbDescription.AppendText("• 핵심 규칙 (승패 조건): ");
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("가장 중요한 기물입니다. 나의 킹이 상대방에게 잡히면 즉시 패배하므로, 마법 카드와 아군 기물을 총동원하여 킹을 철저하게 지켜내야 합니다.\n");
                    break;

                // 🚨 에러 방지용 기본값
                default:
                    rtbDescription.SelectionFont = new Font("맑은 고딕", 13f, FontStyle.Regular);
                    rtbDescription.SelectionColor = Color.White;
                    rtbDescription.AppendText("기물 정보를 불러올 수 없습니다.\n");
                    break;
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
