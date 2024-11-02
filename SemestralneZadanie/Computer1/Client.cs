using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Computer1
{
    public class Client
    {
        private static UDP_server server;

        public void SendMessage(string udpIP, int localPort, int remotePort, string msg, Header.HeaderData headerData)
        {
            byte[] messageBytes = Encoding.ASCII.GetBytes(msg);
            byte[] headerBytes = new byte[2];
            headerBytes[0] = headerData.type;
            headerBytes[1] = headerData.msg;

            byte[] dataToSend = new byte[headerBytes.Length + messageBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length); 
            Buffer.BlockCopy(messageBytes, 0, dataToSend, headerBytes.Length, messageBytes.Length); 

            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
                sock.Bind(localEndPoint);

                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(udpIP), remotePort);

                sock.SendTo(dataToSend, remoteEndPoint);

                Console.WriteLine($"Sent message from port {localPort} to {udpIP} : {remotePort} {msg}");
            }
        }

        /*public bool WaitFor_SYN(int udpPort, string udpIp)
        {
            using (var udpListener = new UdpClient(udpPort))
            {
                // Set the ReuseAddress option
                udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = udpListener.Receive(ref remoteEP);

                    if (receivedBytes.Length > 0 && receivedBytes[0] == Header.HeaderData.SYN)
                    {
                        Console.WriteLine("SYN packet received");
                        return true; // Return true if SYN is received
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

                return false;
            }
        }

        public bool WaitFor_SYN_ACK(int udpPort, string udpIp)
        {
            using (var udpListener = new UdpClient(udpPort))
            {
                // Set the ReuseAddress option
                udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpListener.Client.ReceiveTimeout = 5000;

                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = udpListener.Receive(ref remoteEP);

                    if (receivedBytes.Length > 0 && receivedBytes[0] == Header.HeaderData.SYN_ACK)
                    {
                        Console.WriteLine("SYN-ACK received");
                        return true;
                    }
                }
                catch (SocketException)
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
                // Set the ReuseAddress option
                udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpListener.Client.ReceiveTimeout = 5000;

                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedBytes = udpListener.Receive(ref remoteEP);

                    if (receivedBytes.Length > 0 && receivedBytes[0] == Header.HeaderData.ACK)
                    {
                        Console.WriteLine("ACK received");
                        return true;
                    }
                }
                catch (SocketException)
                {
                    Console.WriteLine("Timeout waiting for ACK");
                }

                return false;
            }
        }*/
    }
}
