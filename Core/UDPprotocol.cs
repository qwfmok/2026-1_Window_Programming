using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

/// 통신 프로토콜 구현은 여기서

namespace CardChess.Core
{
    public class UDPprotocol
    {
        private UdpClient UDPline;
        private int targetPort;
        private string targetIP = "255.255.255.255";

        public bool IsConnected { get; private set; } = false;
        public event Action<string> OnMessage;

        private const int SIO_UDP_CONNRESET = -1744830452;

        public void Send(string msg)
        {
            try
            {
                if (UDPline == null) return;
                byte[] buf = Encoding.UTF8.GetBytes(msg);
                UDPline.Send(buf, buf.Length, targetIP, targetPort);
            }
            catch { }
        }

        private async Task ReceiveLoop()
        {
            while (UDPline != null)
            {
                try
                {
                    var res = await UDPline.ReceiveAsync();
                    string msg = Encoding.UTF8.GetString(res.Buffer);
                    string senderIP = res.RemoteEndPoint.Address.ToString();

                    // 호스트: UI와 코드가 불일치하는 버그를 해결하기 위해 
                    // 게스트의 신호(TRY:)가 오면 묻지도 따지지도 않고 즉시 IP 고정 및 연결
                    if (msg.StartsWith("TRY:"))
                    {
                        targetIP = senderIP;
                        IsConnected = true;
                        Send("OK");
                        OnMessage?.Invoke("CONNECTED");
                    }
                    // 게스트: 호스트의 수락 신호를 받으면 즉시 연결 완료
                    else if (msg == "OK")
                    {
                        targetIP = senderIP;
                        IsConnected = true;
                        OnMessage?.Invoke("CONNECTED");
                    }
                    else
                    {
                        OnMessage?.Invoke(msg);
                    }
                }
                catch (SocketException)
                {
                    continue;
                }
                catch
                {
                    if (UDPline == null) break;
                }
            }
        }

        public string Starthostip(int localport = 9000, int targetport = 9001)
        {
            if (UDPline != null) UDPline.Close();

            this.targetPort = targetport;
            this.UDPline = new UdpClient(localport);
            this.UDPline.EnableBroadcast = true;

            try { this.UDPline.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null); } catch { }

            Task.Run(() => ReceiveLoop());
            return "READY";
        }
        // 끝나면 돌아오게
        public void Close()
        {
            if (UDPline != null)
            {
                try { UDPline.Close(); } catch { }
                UDPline = null;
            }
            IsConnected = false;
        }

        public void Joinguestip(string code, int localport = 9001, int targetport = 9000)
        {
            if (UDPline != null) UDPline.Close();

            this.targetPort = targetport;
            this.UDPline = new UdpClient(localport);
            this.UDPline.EnableBroadcast = true;

            try { this.UDPline.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null); } catch { }

            Task.Run(() => ReceiveLoop());

            byte[] buf = Encoding.UTF8.GetBytes("TRY:" + code);

            // 윈도우가 가상 어댑터로 패킷을 빼돌리지 못하도록 PC에 있는 모든 네트워크망에 동시 살포
            try { UDPline.Send(buf, buf.Length, "255.255.255.255", targetPort); } catch { }
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                string localIP = ip.Address.ToString();
                                int lastDot = localIP.LastIndexOf('.');
                                if (lastDot > 0)
                                {
                                    string bcast = localIP.Substring(0, lastDot) + ".255";
                                    UDPline.Send(buf, buf.Length, bcast, targetPort);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}