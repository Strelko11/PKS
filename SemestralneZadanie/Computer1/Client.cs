using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Computer1
{
    public class Client
    {
        // Method to send a message using UDP
        public void SendMessage(string udpIP, int udpPort, string msg)
        {
            // Convert the message to a byte array
            byte[] data = Encoding.ASCII.GetBytes(msg);

            // Create a new UDP socket
            using(Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                // Set up the remote endpoint using the IP and port
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(udpIP), udpPort);
                
                // Send the data to the remote endpoint
                sock.SendTo(data, endPoint);

                // Output the result to the console
                Console.WriteLine($"Sent message to {udpIP}:{udpPort}");
            }
        }
    }
}