namespace Computer2;
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
            Console.WriteLine($"Listening for connections on {udpIP}:{udpPort}");

            byte[] buffer = new byte[1024];

            while (Program.isRunning)
            {
                EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int bytesReceived = sock.ReceiveFrom(buffer, ref senderEndPoint);

                // Extract the header and message
                byte type = buffer[0]; // First byte for header type
                byte msgState = buffer[1]; // Second byte for message state

                // Convert the remaining bytes to a message
                string receivedMessage =
                    Encoding.ASCII.GetString(buffer, 2, bytesReceived - 2); // Adjust for header size

                // Process the message based on the header type
                ProcessMessage(type, msgState, receivedMessage);
            }
        }
    }


    private void ProcessMessage(byte type, byte msgState, string receivedMessage)
    {
        switch (type)
        {
            case Header.HeaderData.SYN:
                Console.WriteLine("SYN packet received");
                break;
            case Header.HeaderData.SYN_ACK:
                Console.WriteLine("SYN ACK packet received");
                break;
            case Header.HeaderData.ACK:
                Console.WriteLine("ACK packet received");
                break;
        }
    }
}