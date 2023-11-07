using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

using UnityEngine;
using System.Threading.Tasks;
public class UDP : IDisposable
{
    private UdpClient Dest { get; set; }
    private UdpClient Source { get; set; }

    private int DstPort{ get; set; }
    private string IP{ get; set; }
    private Action<string> OnReceive { get; set; }

    private bool WaitingForResponse = true;
    private int MaxWaitTime = 300;

    public void Set(string ip, int srcPort, int dstPort) {
        Dispose();

        IP = ip;
        DstPort = dstPort;

        Dest = new UdpClient();
        Source = new UdpClient(srcPort);
    }

    public void Send(string data, Action<string> onReceive = null) {
        OnReceive = onReceive;
        Source.BeginReceive(OnDataReceive, Source);

        var buffer = Encoding.UTF8.GetBytes(data);
        Dest.Send(buffer, buffer.Length, IP, DstPort);
    }

    public void SendAndWait(string data, Action<string> onReceive = null) {
        WaitingForResponse = true;
        OnReceive = onReceive;
        Source.BeginReceive(OnDataReceive, Source);

        var buffer = Encoding.UTF8.GetBytes(data);
        Dest.Send(buffer, buffer.Length, IP, DstPort);

        for (int i = 0; i < MaxWaitTime; i++) {
            if(WaitingForResponse) {
                System.Threading.Thread.Sleep(1);
            } else {
                //Debug.Log("Now Waited for A Python Response for "+ i + " Milliseconds");
                break;
            }
        }

        if(WaitingForResponse){
            throw new TimeoutException("still waiting for response");
        }
    }

    private void OnDataReceive(IAsyncResult res) {
        WaitingForResponse = false;
        var client = (UdpClient)res.AsyncState;
        IPEndPoint ipEnd = null;

        try {
            var data = client.EndReceive(res, ref ipEnd);
            var message = Encoding.UTF8.GetString(data);
            OnReceive?.Invoke(message);
            WaitingForResponse = false;
        }
        catch(SocketException ex) {
            Dispose();
            return;
        }
        catch(ObjectDisposedException ex) {
            Dispose();
            return;
        }
    }

    public void Dispose() {
        Dest?.Close();
        Dest?.Dispose();
        Dest = null;

        Source?.Close();
        Source?.Dispose();
        Source = null;
    }
}
