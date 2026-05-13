using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CardChess.Core
{
    internal class UDPprotocol
    {
        UdpClient UDPline;
        private int Targetport;
        private string Hostport = "";
        private bool IsConnected { get; private set; } = false;
        public event Action<string> OnMessage;
        private void Send(string msg)
        {
            byte[] buf = Encoding.UTF8.GetBytes(msg);
            UDPline.Send(buf, buf.Length, "127.0.0.1", Targetport);
        }
        private async Task ReceiveLoop()
        {
            try
            {
                while (UDPline != null)
                {
                    var res = await UDPline.ReceiveAsync();
                    string msg = Encoding.UTF8.GetString(res.Buffer);
                    if (msg.StartsWith("TRY:") && msg.Substring(4) == Hostport)
                    {
                        IsConnected = true;
                        Send("OK");
                        OnMessage?.Invoke("CONNECTED");
                    }
                    else if (msg == "OK")
                    {
                        IsConnected = true;
                        OnMessage?.Invoke("CONNECTED");
                    }
                    else
                    {
                        OnMessage?.Invoke(msg);
                    }
                }
            }
            catch { /* 소켓 종료시 예외 처리 */ }
        }
        public string Starthostip(int localport = 9000, int Targetport = 9001)
        {
            this.Targetport = Targetport;
            this.UDPline = new UdpClient(localport);
            this.Hostport = new Random().Next(100000,1000000).ToString();
            Task.Run(() => ReceiveLoop());
            return Hostport;
        }

        public void Joinguestip(string code, int localport = 9001, int Targetport = 9000)
        {
            this.Targetport = Targetport;
            this.UDPline = new UdpClient(localport);
            Task.Run(() => ReceiveLoop());
            Send("TRY:" + code);
        }
    }
}

//==================== 원래 사용했던 참조 코드

//namespace chessudp
//{
//    public partial class Form1 : Form
//    {
//        UdpClient udp;
//        int tPort;
//        string hostCode = "";

//        public Form1() => InitializeComponent();

//        private void Send(string msg)
//        {
//            byte[] buf = Encoding.UTF8.GetBytes(msg);
//            new UdpClient().Send(buf, buf.Length, "127.0.0.1", tPort);
//        }

//        private void button1_Click(object sender, EventArgs e)
//        {
//            tPort = 9001;
//            udp = new UdpClient(9000);
//            // 9000과 9001간의 통신

//            hostCode = new Random().Next(100000, 1000000).ToString();
//            textBox1.Text = hostCode;
//            // 무작위 난수 코드 생성해서 텍스트 박스에 출력

//            timer1.Start();
//            label1.Text = "호스트 대기 중";
//        }

//        private void button2_Click(object sender, EventArgs e)
//        {
//            if (udp != null) return;
//            tPort = 9000;
//            udp = new UdpClient(9001);
//            timer1.Start();
//            Send("TRY:" + textBox1.Text);
//        }

//        private void button3_Click(object sender, EventArgs e)
//        {
//            button3.Enabled = false;
//            Send("TURN");
//        }

//        private void timer1_Tick(object sender, EventArgs e)
//        {
//            if (udp.Available > 0)
//            {
//                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
//                string msg = Encoding.UTF8.GetString(udp.Receive(ref ep));

//                if (msg.StartsWith("TRY:") && msg.Substring(4) == hostCode)
//                {
//                    label1.Text = "연결됨";
//                    Send("OK");
//                    button3.Enabled = true;
//                }
//                else if (msg == "OK") label1.Text = "연결됨";
//                else if (msg == "TURN") button3.Enabled = true;
//            }
//        }
//    }
//}