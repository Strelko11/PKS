namespace Computer1;
using System.Net;
using System.Net.Sockets;
using System.Text;
public class UDP_server
{
    private bool running = true;

    public void Start(string udpIP, int udpPort)
    {
        using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(udpIP), udpPort);
            sock.Bind(endPoint);
            //Console.WriteLine("Listening for connections on " + udpIP + ":" + udpPort);
                
            byte[] buffer = new byte[1024];

            while (Program.isRunning)
            {
                EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int bytesReceived = sock.ReceiveFrom(buffer, ref senderEndPoint);
                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                Console.WriteLine("Received message " + receivedMessage);
            }
        }
    }
}