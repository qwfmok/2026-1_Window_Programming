using CardChess.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CardChess.Networking;

// 행복 버튼 ㅋ

namespace CardChess.Menu
{
    internal class Surrender
    {
        public static void AddSurrenderButton(Form mainForm, SignalRProtocol networkProtocol)
        {
            // 버튼 크기와 위치 좌표 정의 및 폼 디자인 설정
            Button btnSurrender = new Button();
            btnSurrender.Name = "btnSurrender";
            btnSurrender.Size = new Size(100, 40);
            btnSurrender.Location = new Point(1085, 452);

            btnSurrender.FlatStyle = FlatStyle.Flat;
            btnSurrender.FlatAppearance.BorderSize = 0;
            btnSurrender.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnSurrender.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnSurrender.BackColor = Color.Transparent;

            // 해당 위치에 이미지 에셋 button_sur.png를 입힘 마찬가지로 경로는 바이너리의 디버그 에셋 폴더
            string imgPath = Path.Combine(Application.StartupPath, "Assets", "button_sur.png");
            if (File.Exists(imgPath))
            {
                btnSurrender.BackgroundImage = Image.FromFile(imgPath);
                btnSurrender.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                // 이미지가 없으면 그냥 서렌더 라는 텍스트만 나옴
                btnSurrender.Text = "SURRENDER";
                btnSurrender.Font = new Font("맑은 고딕", 9, FontStyle.Bold);
                btnSurrender.ForeColor = Color.White;
            }

            // 서렌더 버튼 클릭 시 이벤트 처리
            btnSurrender.Click += (sender, e) =>
            {
                MessageBox.Show("항복했습니다.", "게임 종료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (networkProtocol != null)
                {
                    try
                    {
                        // 만약 상대방에게 나 나간다 라고 항복 패킷을 쏴줘야 한다면 아래 주석 해제
                        // if (networkProtocol.IsConnected) networkProtocol.Send("SURRENDER");

                        // 게임 화면이 연결 종료를 담당하므로 여기서는 직접 닫지 않는다.
                        // (클래스 내부에 명칭이 다를 수 있으니 확인 후 소켓 Close 함수를 여기에 적어주세요)
                        // networkProtocol.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"네트워크 종료 중 오류 발생: {ex.Message}");
                    }
                }

                // 서렌더 클릭의 블록 안에서 수행되므로 누르면 그대로 메인폼 닫힘
                mainForm.Close();
            };
            mainForm.Controls.Add(btnSurrender);
            btnSurrender.BringToFront();
        }
    }
}
