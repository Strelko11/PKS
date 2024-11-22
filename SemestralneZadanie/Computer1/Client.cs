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
        //public static DateTime startTime; // Declare startTime as DateTime
        private Header.HeaderData header;
        public static byte[] headerBytes /*= new byte[7]*/;
        public static byte[] chunk;
        private CrcAlgorithm crc;
        private UDP_server udpServer;
        //public static bool ACK_file = false;
        //public static bool NACK_file = false;
        public string crc_result;
        //public static  bool file_sent = false;
        //private CrcAlgorithm crc = CrcAlgorithm.CreateCrc16CcittFalse();
        byte[] lastSentPacket = null;
        private DateTime startTime;


        public Client(UDP_server udpServer)
        {
            this.udpServer = udpServer;
        }
        public void SendMessage(string destination_IP, int source_Port, int destination_Port,ushort packet_size, string msg, bool mistake, UdpClient udpClient)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            Program.is_sending = true;
           
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
                Console.WriteLine($"Message beginning to send at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                Console.WriteLine($"Sent message \"{msg}\" from {destination_IP} and {source_Port} to {destination_Port}.");
                if (messageBytes.Length < packet_size && msg != "exit")
                {
                    Program.ACK = false;
                    Program.NACK = false;
                    Program.KEEP_ALIVE_ACK = false;
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

                    startTime = DateTime.Now;
                    //Program.ACK = false;
                    //Console.WriteLine($"Current thread ID: {Thread.CurrentThread.ManagedThreadId}");

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

                                //try
                                //{
                                //if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                                //{
                                if (!Program.KEEP_ALIVE_ACK)
                                {
                                    Console.WriteLine("Sending keep-alive...");
                                    keep_alive(udpClient, startTime, remoteEndPoint);
                                    Program.keep_alive_sent = true;
                                    Program.heartBeat_count++;
                                    startTime = DateTime.Now; // Reset the timeout timer
                                }
                                //}
                                
                                //}
                                /*catch (Exception ex)
                                {
                                    Console.WriteLine($"ERROR: {ex.Message}");
                                }*/
                            }
                        
                            if(Program.NACK ||  Program.KEEP_ALIVE_ACK)
                            {
                                Program.KEEP_ALIVE_ACK = false;
                                Program.NACK = false;
                                //Thread.Sleep(1000);
                                Console.WriteLine("RESENT PACKET XD");
                                //Thread.Sleep(1000);
                        
                                crc_result = checksum_counter(dataToSend, Header.HeaderData.header_size);
                                headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT,Header.HeaderData.DATA, 1,Convert.ToUInt16(crc_result,16));
                       
                                Array.Copy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                                udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                                startTime = DateTime.Now;

                                //lastSentPacket = (byte[])dataToSend.Clone()
                            }  
                    }  
                    if (Program.FIN_received)
                    {
                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.FIN_ACK, 1,0);
                        udpClient.Send(headerBytes, headerBytes.Length, remoteEndPoint);
                        Program.FIN_ACK = true;
                        //udpClient.Send(destination_IP,source_Port, destination_Port, headerBytes);
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
                        // The chunk size includes the packet size + 6 bytes for the header
                        int currentChunkSize = Math.Min(packet_size, messageBytes.Length - (i * packet_size));
                        //Console.WriteLine($"Packet size: {currentChunkSize}");
                        // Allocate space for both the header and the chunk
                        chunk = new byte[packet_size + Header.HeaderData.header_size];
                   

                        // Copy the file data into the chunk, starting from byte 6 (skipping the header)
                        Array.Copy(messageBytes, i * packet_size, chunk, Header.HeaderData.header_size, currentChunkSize);
                        crc_result = checksum_counter(chunk, Header.HeaderData.header_size);
                    
                        if (total_packets == 1 || i == total_packets - 1)
                        {
                            if (mistake && total_packets == 1)
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
                        byte[] sentMessage = new byte[chunk.Length - Header.HeaderData.header_size];
                        Buffer.BlockCopy(chunk, Header.HeaderData.header_size, sentMessage, 0, sentMessage.Length);
                        string sent_message = Encoding.UTF8.GetString(sentMessage);

                        Console.WriteLine($"\nSent chunk {i + 1}/{total_packets} with payload size {currentChunkSize}: {sent_message}");
                        startTime = DateTime.Now;
                          // To store the last sent packet before disconnection
                        //lastSentPacket = chunk;
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

                                //try
                                //{
                                //if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                                //{
                                if (!Program.KEEP_ALIVE_ACK)
                                {
                                    Console.WriteLine("Sending keep-alive...");
                                    keep_alive(udpClient, startTime, remoteEndPoint);
                                    Program.keep_alive_sent = true;
                                    Program.heartBeat_count++;
                                    startTime = DateTime.Now; // Reset the timeout timer
                                }
                                //}
                                
                                //}
                                /*catch (Exception ex)
                                {
                                    Console.WriteLine($"ERROR: {ex.Message}");
                                }*/
                            }
                        
                            if(Program.NACK ||  Program.KEEP_ALIVE_ACK)
                            {
                                Program.KEEP_ALIVE_ACK = false;
                                Program.NACK = false;
                                //Thread.Sleep(1000);
                                Console.WriteLine("RESENT PACKET XD");
                                //Thread.Sleep(1000);
                        
                                crc_result = checksum_counter(chunk, Header.HeaderData.header_size);
                                headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT,Header.HeaderData.DATA, i + 1,Convert.ToUInt16(crc_result,16));
                       
                                Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);
                                udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                                startTime = DateTime.Now;

                                //lastSentPacket = (byte[])dataToSend.Clone()
                            }  
                         }   
                    }

                }
                if ( msg != "KEEP_Alive" && Program.isRunning)
                {
                    Console.WriteLine(
                        $"\nSent message from port {source_Port} to {destination_IP} with port {destination_Port} : {msg}");
                }
                if (Program.FIN_received)
                {
                    headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.FIN_ACK, 1,0);
                    udpClient.Send(headerBytes, headerBytes.Length, remoteEndPoint);
                    Program.FIN_ACK = true;
                    //udpClient.Send(destination_IP,source_Port, destination_Port, headerBytes);
                }
            Program.is_sending = false;
        }

        public void SendFile(string destination_IP, int source_Port, int destination_Port, string filePath,
            ushort packet_size, bool mistake, UdpClient udpClient)
        {
            //file_sent = true;
            Program.is_sending = true;

            Console.WriteLine($"File sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");

            byte[] fileBytes = File.ReadAllBytes(filePath);
            
                Program.ACK = false;
                Program.NACK = false;
                Program.KEEP_ALIVE_ACK = false;
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
                byte[] file_name = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
                crc_result = checksum_counter(file_name,0);
                //Console.Write("CRC16 (current fragment): ");
                //Console.WriteLine(crc.ToHexString());
                headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE, Header.HeaderData.FILE_NAME,9,Convert.ToUInt16(crc_result,16));

                byte[] dataToSend = new byte[headerBytes.Length + file_name.Length];

                Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                Buffer.BlockCopy(file_name, 0, dataToSend, Header.HeaderData.header_size, file_name.Length);
                udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                byte[] lastSentPacket = null;  // To store the last sent packet before disconnection
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

                            //try
                            //{
                            //if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                            //{
                            if (!Program.KEEP_ALIVE_ACK)
                            {
                                Console.WriteLine("Sending keep-alive...");
                                keep_alive(udpClient, startTime, remoteEndPoint);
                                Program.keep_alive_sent = true;
                                Program.heartBeat_count++;
                                startTime = DateTime.Now; // Reset the timeout timer
                            }
                            //}
                                
                            //}
                            /*catch (Exception ex)
                            {
                                Console.WriteLine($"ERROR: {ex.Message}");
                            }*/
                        }
                        
                        if(Program.NACK ||  Program.KEEP_ALIVE_ACK)
                        {
                            Program.KEEP_ALIVE_ACK = false;
                            Program.NACK = false;
                            //Thread.Sleep(1000);
                            Console.WriteLine("RESENT PACKET XD");
                            //Thread.Sleep(1000);
                        
                            crc_result = checksum_counter(dataToSend, Header.HeaderData.header_size);
                            headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE,Header.HeaderData.FILE_NAME, 1,Convert.ToUInt16(crc_result,16));
                       
                            Array.Copy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                            udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                            startTime = DateTime.Now;

                            //lastSentPacket = (byte[])dataToSend.Clone()
                        }  
                    }

                if (Program.isRunning)
                {
                    //udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                int total_packets = (int)Math.Ceiling(fileBytes.Length / (double)packet_size);
                Console.WriteLine($"Sending file: {filePath}, Total Chunks: {total_packets}");
                for (int i = 0; i < total_packets; i++)
                {
                    Program.ACK = false;
                    Program.NACK = false;
                    Program.KEEP_ALIVE_ACK = false;
                    // The chunk size includes the packet size + 6 bytes for the header
                    int currentChunkSize = Math.Min(packet_size, fileBytes.Length - (i * packet_size));
                    //Console.WriteLine($"Packet size: {currentChunkSize}");
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
                    //ACK_file = false;

                    Console.WriteLine($"\nSent chunk {i + 1}/{total_packets} with payload size {currentChunkSize}");
                    //ACK_file = false;
                    startTime = DateTime.Now;
                    DateTime lastKeepAliveTime = DateTime.Now; // Add this at the beginning of your method

                    while (!Program.ACK)
                    {
                        //Thread.Sleep(3000); TODO: Odkomentovat ak bude treba testovat rychlost pripojenia
                        
                        if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                        {
                            if (Program.heartBeat_count > 3)
                            {
                                Console.WriteLine("Connestion lost");
                                Program.isRunning = false;
                                break;
                            }

                            //try
                            //{
                            //if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                            //{
                            if (!Program.KEEP_ALIVE_ACK)
                            {
                                Console.WriteLine("Sending keep-alive...");
                                keep_alive(udpClient, startTime, remoteEndPoint);
                                Program.keep_alive_sent = true;
                                Program.heartBeat_count++;
                                startTime = DateTime.Now; // Reset the timeout timer
                            }
                            //}
                                
                            //}
                            /*catch (Exception ex)
                            {
                                Console.WriteLine($"ERROR: {ex.Message}");
                            }*/
                        }
                        
                        if(Program.NACK ||  Program.KEEP_ALIVE_ACK)
                        {
                            Program.KEEP_ALIVE_ACK = false;
                            Program.NACK = false;
                            //Thread.Sleep(1000);
                            Console.WriteLine("RESENT PACKET XD");
                            //Thread.Sleep(1000);
                        
                            crc_result = checksum_counter(chunk, Header.HeaderData.header_size);
                            headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE,Header.HeaderData.DATA,i + 1,Convert.ToUInt16(crc_result,16));
                       
                            Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);
                            udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                            startTime = DateTime.Now;

                            //lastSentPacket = (byte[])dataToSend.Clone()
                        }  
                    }

                    if (!Program.isRunning)
                    {
                        break;
                    }
                }
                
                if (Program.isRunning)
                {
                    //Console.WriteLine("File sent successfully");
                    Console.WriteLine($"\nFile sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                    FileInfo fileInfo = new FileInfo(filePath);

                    long fileSizeInBytes = fileInfo.Length;
                    byte[] final_crc = File.ReadAllBytes(filePath);
                    crc_result = checksum_counter(final_crc,0);
                    Console.WriteLine($"Total file Size: {fileSizeInBytes} bytes");
                }
                }

                if (Program.FIN_received)
                {
                    headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.FIN_ACK, 1,0);
                    udpClient.Send(headerBytes, headerBytes.Length, remoteEndPoint);
                    Program.FIN_ACK = true;
                    //udpClient.Send(destination_IP,source_Port, destination_Port, headerBytes);
                }

            //file_sent = false;
            Program.is_sending = false;
        }

        public void SendServiceMessage(string destination_IP, int source_Port, int destination_Port, byte[] headerBytes, UdpClient udpClient)
        {
            byte[] dataToSend = new byte[headerBytes.Length];

            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);
            udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                
                //Console.WriteLine(
                    //$"Sent service message from port {source_Port} to {destination_IP}:{destination_Port}");
                //Console.WriteLine($"Message sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");

            
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
            //Console.WriteLine($"CRC 16:{crc.ToHexString()}");
            return crc.ToHexString();
        }

        public void keep_alive(UdpClient udpClient, DateTime startTime, IPEndPoint remoteEndPoint)
        {
            //byte[] keepAliveMessage = Encoding.UTF8.GetBytes("keep_alive");
            
            headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.KEEP_ALIVE, 1, 0);
            byte[] dataToSend = new byte[headerBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
            udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
            Console.WriteLine($"Sent keep_alive message to maintain connection. {Program.heartBeat_count}");
            Program.KEEP_ALIVE_ACK = false;
            // Reset the start time to avoid continuously sending keep-alive messages
            //startTime = DateTime.Now;  // Optional, to avoid sending keep_alive repeatedly every 5 seconds
        }
    }
}