
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
        private Header.HeaderData header;
        public static byte[] headerBytes;
        public static byte[] chunk;
        private CrcAlgorithm crc;
        private UDP_server udpServer;
        public string crc_result;
        byte[] lastSentPacket = null;
        private DateTime startTime;


        public Client(UDP_server udpServer)
        {
            this.udpServer = udpServer;
        }

        public void SendMessage(string destination_IP, int source_Port, int destination_Port, ushort packet_size,
            string msg, bool mistake, UdpClient udpClient)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            Program.is_sending = true;
            Program.StopHeartBeatTimer();

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
            Console.WriteLine($"Message beginning to send at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
            Console.WriteLine($"Sent message \"{msg}\" from {destination_IP} and {source_Port} to {destination_Port}.");
            if (messageBytes.Length < packet_size && msg != "exit")
            {
                Program.ACK = false;
                Program.NACK = false;
                Program.KEEP_ALIVE_ACK = false;
                crc_result = checksum_counter(messageBytes, 0);
                if (mistake)
                {
                    UInt16 crcValue = Convert.ToUInt16(crc_result, 16);
                    crcValue += 1;
                    crc_result = crcValue.ToString("X");
                }

                headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.LAST_FRAGMENT, 1,
                    Convert.ToUInt16(crc_result, 16));

                byte[] dataToSend = new byte[headerBytes.Length + messageBytes.Length];
                Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                Buffer.BlockCopy(messageBytes, 0, dataToSend, headerBytes.Length, messageBytes.Length);

                udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);

                startTime = DateTime.Now;

                while (!Program.ACK)
                {
                    //Thread.Sleep(3000); //TODO: Odkomentovat ak bude treba testovat rychlost pripojenia

                    if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                    {
                        if (Program.heartBeat_count > 3)
                        {
                            Console.WriteLine("Connestion lost");
                            Program.isRunning = false;
                            break;
                        }

                        if (!Program.KEEP_ALIVE_ACK)
                        {
                            Console.WriteLine("Sending keep-alive...");
                            keep_alive(udpClient, startTime, remoteEndPoint);
                            Program.keep_alive_sent = true;
                            Program.heartBeat_count++;
                            startTime = DateTime.Now; // Reset the timeout timer
                        }
                    }

                    if (Program.NACK || Program.KEEP_ALIVE_ACK)
                    {
                        Program.KEEP_ALIVE_ACK = false;
                        Program.NACK = false;
                        Console.WriteLine($"RESENT PACKET with seguence number {1} and payload size {packet_size}"); 

                        crc_result = checksum_counter(dataToSend, Header.HeaderData.header_size);
                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.DATA, 1,
                            Convert.ToUInt16(crc_result, 16));

                        Array.Copy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                        udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                        startTime = DateTime.Now;
                    }
                }

                if (Program.FIN_received)
                {
                    headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.FIN_ACK, 1, 0);
                    udpClient.Send(headerBytes, headerBytes.Length, remoteEndPoint);
                    Program.FIN_ACK = true;
                }
            }

            else
            {
                int total_packets = (int)Math.Ceiling(messageBytes.Length / (double)packet_size);
                Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
                Console.WriteLine($"\nSending message: \"{msg}\", Total Chunks: {total_packets}");
                for (int i = 0; i < total_packets; i++)
                {
                    Program.ACK = false;
                    Program.NACK = false;
                    Program.KEEP_ALIVE_ACK = false;
                    int currentChunkSize = Math.Min(packet_size, messageBytes.Length - (i * packet_size));
                    chunk = new byte[currentChunkSize + Header.HeaderData.header_size];


                    // Copy the file data into the chunk, starting from byte 6 (skipping the header)
                    Array.Copy(messageBytes, i * packet_size, chunk, Header.HeaderData.header_size, currentChunkSize);
                    crc_result = checksum_counter(chunk, Header.HeaderData.header_size);

                    if (total_packets == 1 || i == total_packets - 1)
                    {
                        if (mistake && total_packets == 1)
                        {
                            UInt16 crcValue = Convert.ToUInt16(crc_result, 16);
                            crcValue += 1;
                            crc_result = crcValue.ToString("X");
                        }

                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.LAST_FRAGMENT,
                            i + 1, Convert.ToUInt16(crc_result, 16));
                    }
                    else
                    {
                        if (mistake && i == 0)
                        {
                            UInt16 crcValue = Convert.ToUInt16(crc_result, 16);
                            crcValue += 1;
                            crc_result = crcValue.ToString("X");
                        }

                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.DATA, i + 1,
                            Convert.ToUInt16(crc_result, 16));
                    }

                    Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);

                    // Send the chunk as a UDP packet
                    udpClient.Send(chunk, chunk.Length, remoteEndPoint);

                    byte[] sentMessage = new byte[chunk.Length - Header.HeaderData.header_size];
                    Buffer.BlockCopy(chunk, Header.HeaderData.header_size, sentMessage, 0, sentMessage.Length);
                    string sent_message = Encoding.UTF8.GetString(sentMessage);

                    Console.WriteLine(
                        $"\nSent chunk {i + 1}/{total_packets} with payload size {currentChunkSize}: {sent_message}");
                    startTime = DateTime.Now;
                    while (!Program.ACK)
                    {
                        //Thread.Sleep(3000); //TODO: Odkomentovat ak bude treba testovat rychlost pripojenia

                        if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                        {
                            if (Program.heartBeat_count > 3)
                            {
                                Console.WriteLine("Connestion lost");
                                Program.isRunning = false;
                                break;
                            }

                            if (!Program.KEEP_ALIVE_ACK)
                            {
                                Console.WriteLine("Sending keep-alive...");
                                keep_alive(udpClient, startTime, remoteEndPoint);
                                Program.keep_alive_sent = true;
                                Program.heartBeat_count++;
                                startTime = DateTime.Now; // Reset the timeout timer
                            }
                        }

                        if (Program.NACK || Program.KEEP_ALIVE_ACK)
                        {
                            Program.KEEP_ALIVE_ACK = false;
                            Program.NACK = false;
                            Console.WriteLine($"RESENT PACKET with seguence number {i + 1} and payload size {currentChunkSize}"); 

                            crc_result = checksum_counter(chunk, Header.HeaderData.header_size);
                            headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.DATA, i + 1,
                                Convert.ToUInt16(crc_result, 16));

                            Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);
                            udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                            startTime = DateTime.Now;
                        }
                    }
                }
            }

            if (msg != "KEEP_Alive" && Program.isRunning)
            {
                Console.WriteLine(
                    $"\nSent message from port {source_Port} to {destination_IP} with port {destination_Port} : {msg}");
            }

            if (Program.FIN_received)
            {
                headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.FIN_ACK, 1, 0);
                udpClient.Send(headerBytes, headerBytes.Length, remoteEndPoint);
                Program.FIN_ACK = true;
            }

            Program.is_sending = false;
            Program.StartHeartBeatTimer();
        }

        public void SendFile(string destination_IP, int source_Port, int destination_Port, string filePath,
            ushort packet_size, bool mistake, UdpClient udpClient)
        {
            Program.is_sending = true;
            Program.StopHeartBeatTimer();

            Console.WriteLine($"File sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");

            byte[] fileBytes = File.ReadAllBytes(filePath);

            Program.ACK = false;
            Program.NACK = false;
            Program.KEEP_ALIVE_ACK = false;
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
            byte[] file_name = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
            crc_result = checksum_counter(file_name, 0);
            headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE, Header.HeaderData.FILE_NAME, 9,
                Convert.ToUInt16(crc_result, 16));

            byte[] dataToSend = new byte[headerBytes.Length + file_name.Length];

            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
            Buffer.BlockCopy(file_name, 0, dataToSend, Header.HeaderData.header_size, file_name.Length);
            udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
            byte[] lastSentPacket = null; // To store the last sent packet before disconnection
            startTime = DateTime.Now;

            while (!Program.ACK)
            {
                //Thread.Sleep(3000); //TODO: Odkomentovat ak bude treba testovat rychlost pripojenia
                Thread.Sleep(1);
                if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                {
                    if (Program.heartBeat_count > 3)
                    {
                        Console.WriteLine("Connestion lost");
                        Program.isRunning = false;
                        break;
                    }

                    if (!Program.KEEP_ALIVE_ACK)
                    {
                        Console.WriteLine("Sending keep-alive...");
                        keep_alive(udpClient, startTime, remoteEndPoint);
                        Program.keep_alive_sent = true;
                        Program.heartBeat_count++;
                        startTime = DateTime.Now; // Reset the timeout timer
                    }
                }

                if (Program.NACK || Program.KEEP_ALIVE_ACK)
                {
                    Program.KEEP_ALIVE_ACK = false;
                    Program.NACK = false;
                    Console.WriteLine($"RESENT PACKET with seguence number {1} and payload size {dataToSend.Length}"); 

                    crc_result = checksum_counter(dataToSend, Header.HeaderData.header_size);
                    headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE, Header.HeaderData.FILE_NAME, 1,
                        Convert.ToUInt16(crc_result, 16));

                    Array.Copy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                    udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                    startTime = DateTime.Now;
                }
            }

            if (Program.isRunning)
            {
                int total_packets = (int)Math.Ceiling(fileBytes.Length / (double)packet_size);
                Console.WriteLine($"Sending file: {filePath}, Total Chunks: {total_packets}");
                for (int i = 0; i < total_packets; i++)
                {
                    Program.ACK = false;
                    Program.NACK = false;
                    Program.KEEP_ALIVE_ACK = false;
                    int currentChunkSize = Math.Min(packet_size, fileBytes.Length - (i * packet_size));
                    chunk = new byte[currentChunkSize + Header.HeaderData.header_size];


                    Array.Copy(fileBytes, i * packet_size, chunk, Header.HeaderData.header_size, currentChunkSize);
                    crc_result = checksum_counter(chunk, Header.HeaderData.header_size);

                    if (total_packets == 1 || i == total_packets - 1)
                    {
                        if (mistake)
                        {
                            UInt16 crcValue = Convert.ToUInt16(crc_result, 16);
                            crcValue += 1;
                            crc_result = crcValue.ToString("X");
                        }

                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE, Header.HeaderData.LAST_FRAGMENT,
                            i + 1, Convert.ToUInt16(crc_result, 16));
                    }
                    else
                    {
                        if (mistake && i == 0)
                        {
                            UInt16 crcValue = Convert.ToUInt16(crc_result, 16);
                            crcValue += 1;
                            crc_result = crcValue.ToString("X");
                        }

                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE, Header.HeaderData.DATA, i + 1,
                            Convert.ToUInt16(crc_result, 16));
                    }

                    Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);

                    udpClient.Send(chunk, chunk.Length, remoteEndPoint);

                    Console.WriteLine($"\nSent chunk {i + 1}/{total_packets} with payload size {currentChunkSize}");
                    startTime = DateTime.Now;

                    while (!Program.ACK)
                    {
                        //Thread.Sleep(3000); TODO: Odkomentovat ak bude treba testovat rychlost pripojenia
                        Thread.Sleep(1);
                        if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                        {
                            if (Program.heartBeat_count > 3)
                            {
                                Console.WriteLine("Connestion lost");
                                Program.isRunning = false;
                                break;
                            }

                            if (!Program.KEEP_ALIVE_ACK)
                            {
                                Console.WriteLine("Sending keep-alive...");
                                keep_alive(udpClient, startTime, remoteEndPoint);
                                Program.keep_alive_sent = true;
                                Program.heartBeat_count++;
                                startTime = DateTime.Now; // Reset the timeout timer
                            }
                        }

                        if (Program.NACK || Program.KEEP_ALIVE_ACK)
                        {
                            Program.KEEP_ALIVE_ACK = false;
                            Program.NACK = false;
                            Console.WriteLine($"RESENT PACKET with seguence number {i + 1} and payload size {currentChunkSize}"); 

                            crc_result = checksum_counter(chunk, Header.HeaderData.header_size);
                            headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE, Header.HeaderData.DATA, i + 1,
                                Convert.ToUInt16(crc_result, 16));

                            Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);
                            udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                            startTime = DateTime.Now;
                        }
                    }

                    if (!Program.isRunning)
                    {
                        break;
                    }
                }

                if (Program.isRunning)
                {
                    FileInfo fileInfo = new FileInfo(filePath);

                    long fileSizeInBytes = fileInfo.Length;
                    byte[] final_crc = File.ReadAllBytes(filePath);
                    crc_result = checksum_counter(final_crc, 0);
                    Console.WriteLine("CRC of the file: " + crc_result);
                    Console.WriteLine($"Total packets: {total_packets}");
                    Console.WriteLine($"Total file Size: {fileSizeInBytes} bytes");
                }
            }

            if (Program.FIN_received)
            {
                headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.FIN_ACK, 1, 0);
                udpClient.Send(headerBytes, headerBytes.Length, remoteEndPoint);
                Program.FIN_ACK = true;
            }

            Program.is_sending = false;
            Program.StartHeartBeatTimer();
        }

        public void SendServiceMessage(string destination_IP, int source_Port, int destination_Port, byte[] headerBytes,
            UdpClient udpClient)
        {
            byte[] dataToSend = new byte[headerBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
            udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
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
                crc.Append(crc_bytes, padding_bytes, crc_bytes.Length - padding_bytes);
            }

            return crc.ToHexString();
        }

        public void keep_alive(UdpClient udpClient, DateTime startTime, IPEndPoint remoteEndPoint)
        {
            headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.KEEP_ALIVE, 1, 0);
            byte[] dataToSend = new byte[headerBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
            udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
            Program.KEEP_ALIVE_ACK = false;
        }
    }
}