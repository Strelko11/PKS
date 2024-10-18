namespace Computer2;
using System.Net;
using System.Net.Sockets;
using System.Text;

class UDP_client
{
    public void send_message(string udpIP, int udpPort,string msg)
    {
        byte[] data = Encoding.ASCII.GetBytes(msg);
        using(Socket sock = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp))
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(udpIP), udpPort);
            sock.SendTo(data,endPoint);
            Console.WriteLine("Sent message to " + udpIP + ":" + udpPort);
                
        }
    }
}
