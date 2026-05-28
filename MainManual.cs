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
    public partial class MainManual : Form
    {
        private Image backgroundBg;

        // 메뉴 이동 버튼 이미지들
        private Image imgGameManual;
        private Image imgCardManual;
        private Image imgPieceManual;
        private Image imgBack;

        public MainManual()
        {
            InitializeComponent();
            // 창 크기 및 기본 설정
            this.Width = 1600;
            this.Height = 900;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "설명서 메인 메뉴";
            this.DoubleBuffered = true; // 깜빡임 방지

            LoadGameAssets();
            ApplyButtonImages(); // 버튼에 이미지를 씌우는 로직 호출
        }
        private void LoadGameAssets()
        {
            try
            {
                string assetsPath = Path.Combine(Application.StartupPath, "Assets");

                backgroundBg = Image.FromFile(Path.Combine(assetsPath, "bg_remove_text.png"));

                // [주의] 아래 이미지 파일명들은 실제 Assets 폴더에 있는 버튼 이름으로 맞춰주세요!
                imgGameManual = Image.FromFile(Path.Combine(assetsPath, "button_game_manual.png"));
                imgCardManual = Image.FromFile(Path.Combine(assetsPath, "button_card_manual.png"));
                imgPieceManual = Image.FromFile(Path.Combine(assetsPath, "button_piece_manual.png"));
                imgBack = Image.FromFile(Path.Combine(assetsPath, "button_back.png"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("메인 매뉴얼 이미지 로드 실패: " + ex.Message);
            }
        }
        // 💡 [신규 추가] 폼의 배경과 버튼들에 이미지를 입히고 투명하게 깎아주는 함수
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
            SetupImageButton(btnGameManual, imgGameManual);
            SetupImageButton(btnCardManual, imgCardManual);
            SetupImageButton(btnPieceManual, imgPieceManual);
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
        // 버튼 클릭 이벤트 (화면 전환 로직)
        private void btnPieceManual_Click(object sender, EventArgs e)
        {
            this.Hide();
            // 네임스페이스 경로를 정확하게 지정하여 폼 호출
            using (CardChess.PieceManual pieceForm = new CardChess.PieceManual())
            {
                pieceForm.ShowDialog();
            }
            this.Show();
        }

        private void btnCardManual_Click(object sender, EventArgs e)
        {
            this.Hide();
            using (CardChess.CardManual cardForm = new CardChess.CardManual())
            {
                cardForm.ShowDialog();
            }
            this.Show();
        }

        private void btnGameManual_Click(object sender, EventArgs e)
        {
            this.Hide();
            using (CardChess.GameManual gameForm = new CardChess.GameManual())
            {
                gameForm.ShowDialog();
            }
            this.Show();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
