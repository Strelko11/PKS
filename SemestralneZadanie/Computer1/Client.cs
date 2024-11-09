using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Computer1
{
    public class Client
    {
        private static UDP_server server;
        public static DateTime startTime; // Declare startTime as DateTime

        public void SendMessage(string destination_IP, int source_Port, int destination_Port, string msg, byte[] headerBytes)
        {
            // Convert the message (string) to ASCII byte array
            byte[] messageBytes = Encoding.ASCII.GetBytes(msg);

           
            // Create the final byte array to send, with enough space for the flag and the message
            byte[] dataToSend = new byte[headerBytes.Length + messageBytes.Length];

            // Copy the flag and the message into the final byte array
            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length); 
            Buffer.BlockCopy(messageBytes, 0, dataToSend, headerBytes.Length, messageBytes.Length);

            
            using (UdpClient udpClient = new UdpClient(source_Port))
            {
               /* IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, source_Port);
                sock.Bind(localEndPoint);

                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);

                sock.SendTo(dataToSend, remoteEndPoint);*/
               IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);

               // Send the data
               udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                if(msg != "exit" && msg != "KEEP_Alive"){
                    Console.WriteLine($"Sent message from port {source_Port} to {destination_IP} with port {destination_Port} : {msg}");
                }

                /*if (msg == "KEEP_Alive")
                {
                    Console.WriteLine("Keep alive message sent");
                }*/

            }
        }

        public void SendFile(string destination_IP, int source_Port, int destination_Port, byte[] headerBytes, string filePath)
        {
            
            startTime = DateTime.UtcNow;

            byte[] fileBytes = File.ReadAllBytes(filePath);

            // Combine the header and the file bytes
            byte[] dataToSend = new byte[headerBytes.Length + fileBytes.Length];
    
            // Copy the header into the dataToSend array
            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
    
            // Copy the file content after the header in the dataToSend array
            Buffer.BlockCopy(fileBytes, 0, dataToSend, headerBytes.Length, fileBytes.Length);

            // Send the combined data (header + file) using UDP
            using (UdpClient udpClient = new UdpClient(source_Port))
            {
                // Create the remote endpoint using the destination IP and port
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);

                // Send the dataToSend array (header + file)
                udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                Console.WriteLine("File sent successfully");
                Console.WriteLine($"File sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                FileInfo fileInfo = new FileInfo(filePath);

                // Get the file size in bytes
                long fileSizeInBytes = fileInfo.Length;

                Console.WriteLine($"File Size: {fileSizeInBytes} bytes");
            }
            /*UdpClient udpClient = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
            Console.WriteLine($"Attempting to read file: {filePath}");
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File {filePath} does not exist");
                return;
            }
            Console.WriteLine($"File {filePath} does existtttt WOHOOOOOOOOO");
            
            byte[] fileData = File.ReadAllBytes(filePath); // Read file into byte array
            int chunkSize = 1024; // Size of each chunk (in bytes)
            int totalChunks = (int)Math.Ceiling(fileData.Length / (double)chunkSize); // Calculate total chunks
            Console.WriteLine($"Sending file: {filePath}, Total Chunks: {totalChunks}");
            for (int i = 0; i < totalChunks; i++)
            {
                int currentChunkSize = Math.Min(chunkSize, fileData.Length - (i * chunkSize));
                byte[] chunk = new byte[currentChunkSize];
                Array.Copy(fileData, i * chunkSize, chunk, 0, currentChunkSize);

                // Send chunk as UDP packet
                int bytesSent = udpClient.Send(chunk, chunk.Length, endPoint);
                if (bytesSent <= 0)
                {
                    Console.WriteLine("Error sending chunk.");
                    return; // Return false if there is an issue sending the chunk
                }

                Console.WriteLine($"Sent chunk {i + 1}/{totalChunks}");
            }
            Console.WriteLine("File sent successfully.");*/
            
        }

        public void SendServiceMessage(string destination_IP, int source_Port, int destination_Port, byte[] headerBytes)
        {
            
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
        // Create an array to hold only the flag, which is 1 byte
        /*byte[] headerBytes = new byte[7];
        headerBytes[0] = headerData.flag;  // Store the flag byte
        headerBytes[1] = (byte)(headerData.sequence_number >> 8);  // High byte (most significant byte)
        headerBytes[2] = (byte)(headerData.sequence_number & 0xFF); // Low byte (least significant byte)
        headerBytes[3] = (byte)(headerData.acknowledgment_number >> 8);  // High byte (most significant byte)
        headerBytes[4] = (byte)(headerData.acknowledgment_number & 0xFF); // Low byte (least significant byte)
        headerBytes[5] = (byte)(headerData.checksum >> 8);  // High byte (most significant byte)
        headerBytes[6] = (byte)(headerData.checksum & 0xFF); // Low byte (least significant byte)*/

    }
}
