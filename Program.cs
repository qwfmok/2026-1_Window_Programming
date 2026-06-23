using CardChess.Core;
using CardChess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CardChess
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1());
            /* ▲ 주 진입점을 MainForm으로 바꾸는 경우 꼭 이것을 주석처리!!! */

            //UDPprotocol defaultUdp = new UDPprotocol();
            //Application.Run(new MainForm(defaultUdp, PlayerType.Player1));

            /* ▲ MainForm은 UDP Protocol을 상속?받아서 쓰고 있으므로 참조시켜줘야 함
            !!!매개변수에 인수 두 개를 넣어주지 않으면 실행되지 않을 것이므로 위 코드를 주석 해제하여 사용할 것!!! */
        }
    }
}
