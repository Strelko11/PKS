using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Computer2
{
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

    class UDP_server
    {
        private bool running = true;

        public void Start(string udpIP, int udpPort)
        {
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(udpIP), udpPort);
                sock.Bind(endPoint);
                Console.WriteLine("Listening for connections on " + udpIP + ":" + udpPort);
                
                byte[] buffer = new byte[1024];

                while (running)
                {
                    EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    int bytesReceived = sock.ReceiveFrom(buffer, ref senderEndPoint);
                    string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                    Console.WriteLine("Received message " + receivedMessage);
                }
                
            }
        }
        
    }

    
    class Program
    {   
        static void Main(string[] args)
        {
            string udpIP = "10.10.77.21";
            int UdpPort = 12345;
            UDP_server udpServer = new UDP_server();
            udpServer.Start(udpIP, UdpPort);
        }
    }
}

