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
                Console.WriteLine($"Message beginning to send at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                
                if (messageBytes.Length < packet_size && msg != "exit")
                {
                    Program.ACK = false;
                    Program.NACK = false;
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

                    
                    //Program.ACK = false;
                    while(!Program.ACK)
                    {
                        Thread.Sleep(1);
                        if (Program.NACK)
                        {
                            //Thread.Sleep(1000);
                            crc_result = checksum_counter(dataToSend, Header.HeaderData.header_size);
                            
                            headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.LAST_FRAGMENT, 1, Convert.ToUInt16(crc_result, 16));
                            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                            Buffer.BlockCopy(messageBytes, 0, dataToSend, headerBytes.Length, messageBytes.Length);
                            // Resend the message with original header bytes and data
                            udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                        }
                    }
                    Console.WriteLine($"Sent message \"{msg}\" from {destination_IP} and {source_Port} to {destination_Port}.");
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
                        while(!Program.ACK)
                        {
                            Thread.Sleep(1);
                            if (Program.NACK)
                            {
                                //Thread.Sleep(1000);
                                Console.WriteLine($"Resent chunk {i + 1}/{total_packets} with payload size {currentChunkSize}: {sent_message}");
                                crc_result = checksum_counter(chunk, Header.HeaderData.header_size);
                                if (i == total_packets - 1 || total_packets == 1)
                                {
                                    headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.LAST_FRAGMENT, i+1, Convert.ToUInt16(crc_result, 16));

                                }
                                else
                                {
                                    headerBytes = header.ToByteArray(Header.HeaderData.MSG_TEXT, Header.HeaderData.DATA, i+1, Convert.ToUInt16(crc_result, 16));

                                }
                                Array.Copy(messageBytes, i * packet_size, chunk, Header.HeaderData.header_size, currentChunkSize);
                                Array.Copy(headerBytes, 0, chunk, 0, headerBytes.Length);
                                udpClient.Send(chunk, chunk.Length, remoteEndPoint);
                                mistake = false;
                            }
                        }
                        
                    }

                }
                if ( msg != "KEEP_Alive")
                {
                    Console.WriteLine(
                        $"\nSent message from port {source_Port} to {destination_IP} with port {destination_Port} : {msg}");
                }
            }
        }

        public void SendFile(string destination_IP, int source_Port, int destination_Port, string filePath,
            ushort packet_size, bool mistake)
        {
            //file_sent = true;

            Console.WriteLine($"File sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");

            byte[] fileBytes = File.ReadAllBytes(filePath);
            using (UdpClient udpClient = new UdpClient(source_Port))
            {
                Program.ACK = false;
                Program.NACK = false;
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
                while (!Program.ACK)
                {
                    Thread.Sleep(1);
                    if (Program.NACK)
                    {
                        //Thread.Sleep(1000);
                        Console.WriteLine("RESENT PACKET");
                        //Thread.Sleep(1000);
                        
                        crc_result = checksum_counter(dataToSend, Header.HeaderData.header_size);
                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_FILE,Header.HeaderData.FILE_NAME,1,Convert.ToUInt16(crc_result,16));
                       
                        Array.Copy(headerBytes, 0, dataToSend, 0, headerBytes.Length);
                        udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                    }
                }
                //udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                int total_packets = (int)Math.Ceiling(fileBytes.Length / (double)packet_size);
                Console.WriteLine($"Sending file: {filePath}, Total Chunks: {total_packets}");
                for (int i = 0; i < total_packets; i++)
                {
                    Program.ACK = false;
                    Program.NACK = false;
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
                    while (!Program.ACK)
                    {
                        Thread.Sleep(1);
                        if (Program.NACK)
                        {         
                            Console.WriteLine($"Resent chunk {i + 1}/{total_packets} with payload size {currentChunkSize}");
                            //Thread.Sleep(1000);
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
                Console.WriteLine($"\nFile sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                FileInfo fileInfo = new FileInfo(filePath);

                long fileSizeInBytes = fileInfo.Length;
                byte[] final_crc = File.ReadAllBytes(filePath);
                crc_result = checksum_counter(final_crc,0);
                Console.WriteLine($"Total file Size: {fileSizeInBytes} bytes");
            }

            //file_sent = false;
        }

        public void SendServiceMessage(string destination_IP, int source_Port, int destination_Port, byte[] headerBytes)
        {
            byte[] dataToSend = new byte[headerBytes.Length];

            Buffer.BlockCopy(headerBytes, 0, dataToSend, 0, headerBytes.Length);


            using (UdpClient udpClient = new UdpClient(source_Port))
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(destination_IP), destination_Port);

                udpClient.Send(dataToSend, dataToSend.Length, remoteEndPoint);
                
                //Console.WriteLine(
                    //$"Sent service message from port {source_Port} to {destination_IP}:{destination_Port}");
                //Console.WriteLine($"Message sent at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");

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
            //Console.WriteLine($"CRC 16:{crc.ToHexString()}");
            return crc.ToHexString();
        }
    }
}
