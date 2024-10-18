using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Computer1
{
    public class Client
    {
        // Method to send a message using UDP
        public void SendMessage(string udpIP, int udpPort, string msg, Header.HeaderData headerData)
        {
            // Convert the message to a byte array
            byte[] messageBytes = Encoding.ASCII.GetBytes(msg);
            byte[] headerBytes = new byte[2];
            headerBytes[0] = headerData.type;
            headerBytes[1] = headerData.msg;
            
            // Combine header and message into one byte array
            byte[] dataToSend = new byte[headerBytes.Length + messageBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length); // Copy header to the beginning
            Buffer.BlockCopy(messageBytes, 0, dataToSend, headerBytes.Length, messageBytes.Length); // Copy message after header

            // Create a new UDP socket
            using(Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                // Set up the remote endpoint using the IP and port
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(udpIP), udpPort);
                
                // Send the data to the remote endpoint
                sock.SendTo(dataToSend, endPoint);

                // Output the result to the console
                Console.WriteLine($"Sent message to {udpIP}:{udpPort}");
            }
        }
        
        public bool WaitFor_SYN(int udpPort, string udpIp)
        {
            using (var udpListener = new UdpClient(udpPort))
            {
                //udpListener.Client.ReceiveTimeout = 5000;
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = udpListener.Receive(ref remoteEP); // Wait indefinitely for a packet

                // Check if we received a SYN packet
                if (receivedBytes.Length > 0 && receivedBytes[0] == Header.HeaderData.SYN) 
                {
                    Console.WriteLine("SYN packet received");
                    return true; // Return true if SYN is received
                }

                return false;
            }
        }

        public bool WaitFor_SYN_ACK(int udpPort, string udpIp)
        {
            using (var udpListener = new UdpClient(udpPort))
            {
                udpListener.Client.ReceiveTimeout = 5000;
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = udpListener.Receive(ref remoteEP);

                    if (receivedBytes.Length > 0 || receivedBytes[0] == Header.HeaderData.SYN_ACK) 
                    {
                        Console.WriteLine("SYN-ACK received");
                        return true;
                    }
                }
                catch(SocketException)
                {
                    Console.WriteLine("Timeout waiting for SYN-ACK");
                }

                return false;   
            }
        }
        public bool WaitFor_ACK(int udpPort, string udpIp)
        {
            using (var udpListener = new UdpClient(udpPort))
            {
                udpListener.Client.ReceiveTimeout = 5000;
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = udpListener.Receive(ref remoteEP);

                    if (receivedBytes.Length > 0 || receivedBytes[0] == Header.HeaderData.ACK) 
                    {
                        Console.WriteLine("ACK received");
                        return true;
                    }
                }
                catch(SocketException)
                {
                    Console.WriteLine("Timeout waiting for ACK");
                }

                return false;   
            }
        }
    }
}