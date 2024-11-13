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
        public static bool ACK_file = false;
        public static bool NACK_file = false;
        public string crc_result;
        public static  bool file_sent = false;

        public Client(UDP_server udpServer)
        {
            this.udpServer = udpServer;
        }
        public void SendMessage(string destination_IP, int source_Port, int destination_Port,ushort packet_size, string msg, bool mistake)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            using (UdpClient udpClient = new UdpClient(source_Port))
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
                if (messageBytes.Length < packet_size)
                {
                    // Calculate initial CRC and prepare message
                    crc_result = checksum_counter(messageBytes, 0);
                    if (mistake)
                    {
                        UInt16 crcValue = Convert.ToUInt16(crc_result, 16);
                        crcValue += 1;
                        crc_result = crcValue.ToString("X");
                    }

                    headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.LAST_FRAGMENT, 1, Convert.ToUInt16(crc_result, 16));

                    // Create the final byte array to send
                    byte[] dataToSend = new byte[headerBytes.Length + messageBytes.Length];
                    Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                    Buffer.BlockCopy(messageBytes, 0, dataToSend, headerBytes.Length, messageBytes.Length);

                    // Send message initially
                    udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                    Program.stop_wait_ACK = false;
                    while(!Program.stop_wait_ACK)
                    {
                        if (Program.stop_wait_NACK)
                        {
                            Thread.Sleep(1000);

                            // Reset crc_result to the original value if needed
                            UInt16 crcValue = Convert.ToUInt16(crc_result, 16);
                            crcValue -= 1;
                            crc_result = crcValue.ToString("X");
                            headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.LAST_FRAGMENT, 1, Convert.ToUInt16(crc_result, 16));
                            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                            Buffer.BlockCopy(messageBytes, 0, dataToSend, headerBytes.Length, messageBytes.Length);
                            // Resend the message with original header bytes and data
                            udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                        }
                    }
                    

                }
                else
                {
                    int total_packets = (int)Math.Ceiling(messageBytes.Length / (double)packet_size);
                    Console.WriteLine($"Sending message: {msg}, Total Chunks: {total_packets}");
                    for (int i = 0; i < total_packets; i++)
                    {
                        // The chunk size includes the packet size + 6 bytes for the header
                        int currentChunkSize = Math.Min(packet_size, messageBytes.Length - (i * packet_size));
                        Console.WriteLine($"Packet size: {currentChunkSize}");
                        // Allocate space for both the header and the chunk
                        chunk = new byte[packet_size + Header.HeaderData.header_size];
                   

                        // Copy the file data into the chunk, starting from byte 6 (skipping the header)
                        Array.Copy(messageBytes, i * packet_size, chunk, Header.HeaderData.header_size, currentChunkSize);
                        crc_result = checksum_counter(chunk, Header.HeaderData.header_size);
                    
                        if (total_packets == 1 || i == total_packets - 1)
                        {
                            if (mistake)
                            {
                                UInt16 crcValue = Convert.ToUInt16(crc_result,16);
                                crcValue += 1;
                                crc_result = crcValue.ToString("X");
                            }
                            headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT,Header.HeaderData.LAST_FRAGMENT,i + 1,Convert.ToUInt16(crc_result,16));
                        }
                        else
                        {
                            if (mistake && i == 0)
                            {
                                UInt16 crcValue = Convert.ToUInt16(crc_result,16);
                                crcValue += 1;
                                crc_result = crcValue.ToString("X");
                            }
                            headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT,Header.HeaderData.DATA,i + 1,Convert.ToUInt16(crc_result,16));

                        }
                        //headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT,Header.HeaderData.LAST_FRAGMENT,i,Convert.ToUInt16(crc_result,16));

                        Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);

                        // Send the chunk as a UDP packet
                        udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                        //ACK_file = false;

                        Console.WriteLine($"Sent chunk {i + 1}/{total_packets}");
                        Program.stop_wait_ACK = false;
                        while(!Program.stop_wait_ACK)
                        {
                            if (Program.stop_wait_NACK)
                            {
                                Thread.Sleep(1000);

                                // Reset crc_result to the original value if needed
                                UInt16 crcValue = Convert.ToUInt16(crc_result, 16);
                                crcValue -= 1;
                                crc_result = crcValue.ToString("X");
                                headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.DATA, 1, Convert.ToUInt16(crc_result, 16));
                                Array.Copy(messageBytes, i * packet_size, chunk, Header.HeaderData.header_size, currentChunkSize);
                                Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);
                                udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                            }
                        }
                    }

                }
                if (msg != "exit" && msg != "KEEP_Alive")
                {
                    Console.WriteLine(
                        $"Sent message from port {source_Port} to {destination_IP} with port {destination_Port} : {msg}");
                    Console.WriteLine($"Message sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                }
            }
        }

        public void SendFile(string destination_IP, int source_Port, int destination_Port, string filePath,
            ushort packet_size, bool mistake)
        {
            file_sent = true;

            Console.WriteLine($"File sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");

            byte[] fileBytes = File.ReadAllBytes(filePath);
            using (UdpClient udpClient = new UdpClient(source_Port))
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
                byte[] file_name = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
                crc_result = checksum_counter(file_name,0);
                Console.Write("CRC16 (current fragment): ");
                Console.WriteLine(crc.ToHexString());
                headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE, Header.HeaderData.FILE_NAME,1,Convert.ToUInt16(crc_result,16));

                byte[] dataToSend = new byte[headerBytes.Length + file_name.Length];

                Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                Buffer.BlockCopy(file_name, 0, dataToSend, Header.HeaderData.header_size, file_name.Length);
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
                    chunk = new byte[packet_size + Header.HeaderData.header_size];
                   

                    // Copy the file data into the chunk, starting from byte 6 (skipping the header)
                    Array.Copy(fileBytes, i * packet_size, chunk, Header.HeaderData.header_size, currentChunkSize);
                    crc_result = checksum_counter(chunk, Header.HeaderData.header_size);
                    
                    if (total_packets == 1 || i == total_packets - 1)
                    {
                        if (mistake)
                        {
                            UInt16 crcValue = Convert.ToUInt16(crc_result,16);
                            crcValue += 1;
                            crc_result = crcValue.ToString("X");
                        }
                        //header.setFlag(Header.HeaderData.MSG_FILE, Header.HeaderData.LAST_FRAGMENT);
                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE,Header.HeaderData.LAST_FRAGMENT,i + 1,Convert.ToUInt16(crc_result,16));
                    }
                    else
                    {
                        if (mistake && i == 0)
                        {
                            UInt16 crcValue = Convert.ToUInt16(crc_result,16);
                            crcValue += 1;
                            crc_result = crcValue.ToString("X");
                        }
                        //header.setFlag(Header.HeaderData.MSG_FILE, Header.HeaderData.DATA);
                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE,Header.HeaderData.DATA,i + 1,Convert.ToUInt16(crc_result,16));

                    }
                    //headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE,Header.HeaderData.LAST_FRAGMENT,i,Convert.ToUInt16(crc_result,16));

                    Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);

                    // Send the chunk as a UDP packet
                    udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                    ACK_file = false;

                    Console.WriteLine($"Sent chunk {i + 1}/{total_packets}");
                    ACK_file = false;
                    while (!ACK_file)
                    {
                        if (NACK_file)
                        {         
                            Thread.Sleep(1000);
                            UInt16 crcValue = Convert.ToUInt16(crc_result,16);
                            crcValue -= 1;
                            crc_result = crcValue.ToString("X");
                            if (total_packets == 1 || i == total_packets - 1)
                            {
                                headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE,Header.HeaderData.LAST_FRAGMENT,i + 1,Convert.ToUInt16(crc_result,16));
                            }
                            else
                            {
                                headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE,Header.HeaderData.DATA,i + 1,Convert.ToUInt16(crc_result,16));

                            }
                            Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);
                            udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                            mistake = false;
                        }
                    }
                }



                //Console.WriteLine("File sent successfully");
                Console.WriteLine($"File sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                FileInfo fileInfo = new FileInfo(filePath);

                long fileSizeInBytes = fileInfo.Length;
                byte[] final_crc = File.ReadAllBytes(filePath);
                crc_result = checksum_counter(final_crc,0);
                Console.WriteLine($"File Size: {fileSizeInBytes} bytes");
            }

            file_sent = false;
        }

        public void SendServiceMessage(string destination_IP, int source_Port, int destination_Port, byte[] headerBytes)
        {
            byte[] dataToSend = new byte[headerBytes.Length];

            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);


            using (UdpClient udpClient = new UdpClient(source_Port))
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);

                udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                
                Console.WriteLine(
                    $"Sent service message from port {source_Port} to {destination_IP}:{destination_Port}");
                Console.WriteLine($"Message sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");

            }
        }

        public string checksum_counter(byte[] crc_bytes, int padding_bytes)
        {
            crc = CrcAlgorithm.CreateCrc16CcittFalse();
            if (padding_bytes == 0)
            {
                crc.Append(crc_bytes);
                
            }
            else
            {
                crc.Append(crc_bytes, padding_bytes,crc_bytes.Length - padding_bytes);
            }
            Console.WriteLine($"CRC 16:{crc.ToHexString()}");
            return crc.ToHexString();
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
/*private byte[] WaitForAck(UdpClient udpClient, IPEndPoint remoteEndPoint)
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
   }*/            
   
        /*if (packet_size > MIN_PACKET_SIZE && currentChunkSize < packet_size)
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

                        }*/