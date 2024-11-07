using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Computer1
{
    public class Client
    {
        private static UDP_server server;

        public void SendMessage(string destination_IP, int source_Port, int destination_Port, string msg, byte[] headerBytes)
        {
            // Convert the message (string) to ASCII byte array
            byte[] messageBytes = Encoding.ASCII.GetBytes(msg);

            // Create an array to hold only the flag, which is 1 byte
            /*byte[] headerBytes = new byte[7];
            headerBytes[0] = headerData.flag;  // Store the flag byte
            headerBytes[1] = (byte)(headerData.sequence_number >> 8);  // High byte (most significant byte)
            headerBytes[2] = (byte)(headerData.sequence_number & 0xFF); // Low byte (least significant byte)
            headerBytes[3] = (byte)(headerData.acknowledgment_number >> 8);  // High byte (most significant byte)
            headerBytes[4] = (byte)(headerData.acknowledgment_number & 0xFF); // Low byte (least significant byte)
            headerBytes[5] = (byte)(headerData.checksum >> 8);  // High byte (most significant byte)
            headerBytes[6] = (byte)(headerData.checksum & 0xFF); // Low byte (least significant byte)*/

            // Create the final byte array to send, with enough space for the flag and the message
            byte[] dataToSend = new byte[headerBytes.Length + messageBytes.Length];

            // Copy the flag and the message into the final byte array
            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length); 
            Buffer.BlockCopy(messageBytes, 0, dataToSend, headerBytes.Length, messageBytes.Length);

            
            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, source_Port);
                sock.Bind(localEndPoint);

                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);

                sock.SendTo(dataToSend, remoteEndPoint);
                if(msg != "exit" && msg != "KEEP_Alive"){
                    Console.WriteLine($"Sent message from port {source_Port} to {destination_IP} with port {destination_Port} : {msg}");
                }

                /*if (msg == "KEEP_Alive")
                {
                    Console.WriteLine("Keep alive message sent");
                }*/

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
