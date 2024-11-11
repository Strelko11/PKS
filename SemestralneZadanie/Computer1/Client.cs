using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using InvertedTomato.Crc;

namespace Computer1
{
    public class Client
    {
        public const int MIN_PACKET_SIZE = 12;

        private static UDP_server server;
        public static DateTime startTime; // Declare startTime as DateTime
        private Header.HeaderData header;
        public static byte[] headerBytes /*= new byte[7]*/;
        public static byte[] chunk;
        private CrcAlgorithm crc;
        private UDP_server udpServer;
        public static bool ACK_file = true;

        public Client(UDP_server udpServer)
        {
            this.udpServer = udpServer;
        }
        public void SendMessage(string destination_IP, int source_Port, int destination_Port, string msg,
            byte[] headerBytes)
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


                if (msg != "exit" && msg != "KEEP_Alive")
                {
                    Console.WriteLine(
                        $"Sent message from port {source_Port} to {destination_IP} with port {destination_Port} : {msg}");
                    Console.WriteLine($"Message sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                }

                /*if (msg == "KEEP_Alive")
                {
                    Console.WriteLine("Keep alive message sent");
                }*/

            }
        }

        public void SendFile(string destination_IP, int source_Port, int destination_Port, string filePath,
            ushort packet_size)
        {
            Console.WriteLine($"File sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");

            byte[] fileBytes = File.ReadAllBytes(filePath);
            //byte[] dataToSend = new byte[headerBytes.Length + fileBytes.Length];
            //Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
            //Buffer.BlockCopy(fileBytes, 0, dataToSend, headerBytes.Length, fileBytes.Length);
            using (UdpClient udpClient = new UdpClient(source_Port))
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
                byte[] file_name = Encoding.ASCII.GetBytes(Path.GetFileName(filePath));
                header.setFlag(Header.HeaderData.MSG_FILE, Header.HeaderData.FILE_NAME);
                header.sequence_number = 1;
                crc = CrcAlgorithm.CreateCrc16CcittFalse();
                crc.Append(file_name);
                header.checksum = Convert.ToUInt16(crc.ToHexString(), 16);
                Console.Write("CRC16 (current fragment): ");
                Console.WriteLine(crc.ToHexString());
                header.payload_size = 9;
                headerBytes = header.ToByteArray();

                byte[] dataToSend = new byte[headerBytes.Length + file_name.Length];

                Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                Buffer.BlockCopy(file_name, 0, dataToSend, 8, file_name.Length);
                udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                //udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                int total_packets = (int)Math.Ceiling(fileBytes.Length / (double)packet_size);
                Console.WriteLine($"Sending file: {filePath}, Total Chunks: {total_packets}");
                for (int i = 0; i < total_packets; i++)
                {

                    // The chunk size includes the packet size + 6 bytes for the header
                    int currentChunkSize = Math.Min(packet_size, fileBytes.Length - (i * packet_size));
                    Console.WriteLine($"Packet size: {currentChunkSize}");
                    // Allocate space for both the header and the chunk
                    if (packet_size > MIN_PACKET_SIZE && currentChunkSize < packet_size)
                    {
                        Console.WriteLine($"Added padding {packet_size - currentChunkSize} bytes");
                        chunk = new byte[packet_size];
                    }

                    if (packet_size < MIN_PACKET_SIZE)
                    {
                        chunk = new byte[MIN_PACKET_SIZE + 8];
                        Console.WriteLine($"Added padding {chunk.Length - packet_size - 8} bytes");
                    }
                    else
                    {
                        chunk = new byte[packet_size + 8]; // packet_size for data + 6 for header

                    }

                    // Copy the file data into the chunk, starting from byte 6 (skipping the header)
                    Array.Copy(fileBytes, i * packet_size, chunk, 8, currentChunkSize);

                    // Copy the header into the first 6 bytes of the chunk


                    // Print the current chunk as a string (excluding the header)
                    //string receivedText = Encoding.ASCII.GetString(chunk, 6, currentChunkSize);
                    //Console.WriteLine(receivedText);  // Print the actual text received (excluding the header)

                    // Calculate CRC16 over the data part (excluding the header)
                    crc = CrcAlgorithm.CreateCrc16CcittFalse();
                    crc.Append(chunk, 8, currentChunkSize); // Append the data part of the chunk (without the header)

                    // Print the CRC16 for the chunk
                    Console.Write("CRC16 (current fragment): ");
                    Console.WriteLine(crc.ToHexString());
                    if (total_packets == 1 || i == total_packets - 1)
                    {
                        header.setFlag(Header.HeaderData.MSG_FILE, Header.HeaderData.LAST_FRAGMENT);
                    }
                    else
                    {
                        header.setFlag(Header.HeaderData.MSG_FILE, Header.HeaderData.DATA);
                    }

                    header.sequence_number = i;
                    header.checksum = Convert.ToUInt16(crc.ToHexString(), 16);
                    header.payload_size = Convert.ToUInt16(currentChunkSize);
                    headerBytes = header.ToByteArray();

                    Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);

                    // Send the chunk as a UDP packet
                    udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                    ACK_file = false;

                    Console.WriteLine($"Sent chunk {i + 1}/{total_packets}");
                    while (!ACK_file){}
                    
                    /*byte[] ackData = WaitForAck(udpClient, remoteEndPoint);
                    if (ackData != null)
                    {
                        Console.WriteLine($"Received ACK for chunk {i + 1}");
                    }
                    else
                    {
                        Console.WriteLine("Timeout waiting for ACK");
                    }*/
                }



                //Console.WriteLine("File sent successfully");
                Console.WriteLine($"File sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                FileInfo fileInfo = new FileInfo(filePath);

                long fileSizeInBytes = fileInfo.Length;
                byte[] final_crc = File.ReadAllBytes(filePath);
                crc = CrcAlgorithm.CreateCrc16CcittFalse();
                crc.Append(final_crc);
                Console.Write("CRC16: ");
                Console.WriteLine(crc.ToHexString());

                Console.WriteLine($"File Size: {fileSizeInBytes} bytes");
            }


        }

        public void SendServiceMessage(string destination_IP, int source_Port, int destination_Port, byte[] headerBytes)
        {
            //byte[] messageBytes = Encoding.ASCII.GetBytes(msg);


            // Create the final byte array to send, with enough space for the flag and the message
            byte[] dataToSend = new byte[headerBytes.Length];

            // Copy the flag and the message into the final byte array
            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
            //Buffer.BlockCopy(messageBytes, 0, dataToSend, headerBytes.Length, messageBytes.Length);


            using (UdpClient udpClient = new UdpClient(source_Port))
            {
                /* IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, source_Port);
                 sock.Bind(localEndPoint);

                 IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);

                 sock.SendTo(dataToSend, remoteEndPoint);*/
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);

                // Send the data
                udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);



                Console.WriteLine(
                    $"Sent service message from port {source_Port} to {destination_IP}:{destination_Port}");
                Console.WriteLine($"Message sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");


                /*if (msg == "KEEP_Alive")
                {
                    Console.WriteLine("Keep alive message sent");
                }*/

            }
        }
        private byte[] WaitForAck(UdpClient udpClient, IPEndPoint remoteEndPoint)
        {
            int timeout = 5000; // Timeout in milliseconds
            DateTime startTime = DateTime.Now;

            while (true)
            {
                // Check if the timeout period has passed
                if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                {
                    Console.WriteLine("Timeout waiting for ACK.");
                    return null; // Timeout occurred, return null
                }

                // Check if data is available to be read
                if (udpClient.Available > 0)
                {
                    byte[] receivedData = udpClient.Receive(ref remoteEndPoint);
                    byte flag = receivedData[0];
                    byte msg_flag = (byte)(flag & 0b1111); // Extract message flag

                    // Check if it's an ACK message
                    if (msg_flag == Header.HeaderData.ACK)
                    {
                        Console.WriteLine("Received ACK");
                        return receivedData;
                    }
                    else
                    {
                        Console.WriteLine("Received non-ACK message.");
                        return null; // Not an ACK, return null
                    }
                }

                // Optional: Add a small delay to avoid 100% CPU usage in the loop
                System.Threading.Thread.Sleep(10);
            }
        }


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
        // Create an array to hold only the flag, which is 1 byte
        /*byte[] headerBytes = new byte[7];
        headerBytes[0] = headerData.flag;  // Store the flag byte
        headerBytes[1] = (byte)(headerData.sequence_number >> 8);  // High byte (most significant byte)
        headerBytes[2] = (byte)(headerData.sequence_number & 0xFF); // Low byte (least significant byte)
        headerBytes[3] = (byte)(headerData.acknowledgment_number >> 8);  // High byte (most significant byte)
        headerBytes[4] = (byte)(headerData.acknowledgment_number & 0xFF); // Low byte (least significant byte)
        headerBytes[5] = (byte)(headerData.checksum >> 8);  // High byte (most significant byte)
        headerBytes[6] = (byte)(headerData.checksum & 0xFF); // Low byte (least significant byte)*/
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