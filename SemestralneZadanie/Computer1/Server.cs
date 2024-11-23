namespace Computer1;

using System.Net;
using System.Net.Sockets;
using System.Text;
using InvertedTomato.Crc;

public class UDP_server
{
    private Client client;
    public string receivedMessage;
    public int count;
    private CrcAlgorithm crc;
    public uint combined_sequence_number;
    public ushort combined_checksum;
    public string file_name;
    public string path = "~/Downloads/";
    public string file_path;
    public List<byte[]> file_bytes = new List<byte[]>();
    public List<byte[]> message_bytes = new List<byte[]>();
    public byte[] fileBytes;
    public string crc_result;
    public byte[] messageBytes;
    public byte[] complete_message;
    public string formattedCrcResult;
    public string formattedHeaderChecksum;
    private DateTime startTime;
    public uint lastSequenceNumber;


    public UDP_server(Client client)
    {
        this.client = client;
    }

    public void Start(int source_Port)
    {
        using (UdpClient udpClient = new UdpClient(source_Port))
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, source_Port);
            startTime = DateTime.Now;

            while (Program.isRunning)
            {
                if (udpClient.Available > 0)
                {
                    byte[] buffer = udpClient.Receive(ref endPoint);
                    byte flag = buffer[0];
                    byte type_flag = (byte)((flag >> 4) & 0b1111);
                    byte msg_flag = (byte)(flag & 0b1111);
                    ProcessMessageFlag(type_flag, msg_flag, buffer);
                    startTime = DateTime.Now;
                }
                else if ((DateTime.Now - startTime).TotalMilliseconds >= 15000 && !Program.iniciator)
                {
                    Program.isRunning = false;
                    Console.WriteLine("Waiting too long for some message. Connection lost");
                    Console.WriteLine("(You can press ENTER to exit...)");
                }
            }
        }
    }

    public void ProcessMessageFlag(byte type_flag, byte msg_flag, byte[] buffer)
    {
        switch (type_flag)
        {
            case 0b0000:
                ProcessServiceMessages(msg_flag);
                break;
            case 0b0001:
                ProcessTextMessage(msg_flag, buffer);
                break;
            case 0b0010:
                ProcessFileMessage(buffer, msg_flag);
                break;
        }
    }

    public void ProcessServiceMessages(byte msg_flag)
    {
        switch (msg_flag)
        {
            case 0b0000:
                if (!Program.handshake_complete)
                {
                    Program.handshake_SYN = true;
                    Console.WriteLine("SYN packet received");
                    RespondToSYN();
                }
                else
                {
                    Console.WriteLine("Handshake already initiated");
                }

                break;
            case 0b0010:
                Program.handshake_SYN_ACK = true;
                Console.WriteLine("SYN_ACK received");
                break;
            
            case 0b0011:
                if (!Program.handshake_complete)
                {
                    Console.WriteLine("ACK packet received");
                    Program.handshake_ACK = true;
                    Console.WriteLine("**************** HANDSHAKE COMPLETE *************\n\n");
                    Program.handshake_complete = true;
                }
                else if (Program.keep_alive_sent)
                {
                    Program.keep_alive_sent = false;
                    Program.KEEP_ALIVE_ACK = true;
                    Program.heartBeat_count--;
                    if (Program.heartBeat_count >= 1)
                    {
                        Program.heartBeat_count = 1;
                    }
                }
                else if (Program.FIN_ACK && (Program.FIN_received || Program.FIN_sent))
                {
                    Program.isRunning = false;
                    Console.WriteLine("Connection closed");
                    Console.WriteLine("Press ENTER to exit...");
                    Program.StopHeartBeatTimer();
                }
                else
                {
                    Program.NACK = false;
                    Program.ACK = true;
                }
                break;
            
            case 0b1001:
                Program.NACK = true;
                Program.ACK = false;
                break;
            
            case 0b0101:
                Console.WriteLine("Received FIN");
                Program.FIN_received = true;
                if (!Program.is_sending)
                {
                    send_FIN_ACK();
                    Program.FIN_ACK = true;
                }
                break;
            
            case 0b0110:
                Console.WriteLine("Received FIN_ACK");
                ACK_message();
                Console.WriteLine("Connection closed");
                Console.WriteLine("Press ENTER to exit...");
                Program.isRunning = false;
                break;
            
            case 0b1000:
                ACK_message();
                break;
        }
    }


    public void ProcessTextMessage(byte msg_flag, byte[] buffer)
    {
        switch (msg_flag)
        {
            case 0b0100:
                
                messageBytes = new byte[buffer.Length - Header.HeaderData.header_size];
                Buffer.BlockCopy(buffer, Header.HeaderData.header_size, messageBytes, 0, messageBytes.Length);
                crc_result = checksum_counter(messageBytes, 0);
                var header = extract_header(buffer);
                formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
                formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();
                Console.Write(
                    $"\nRecived text message \"{Encoding.UTF8.GetString(messageBytes, 0, messageBytes.Length)}\" with sequence number {header.sequenceNumber}");

                
                if (formattedCrcResult == formattedHeaderChecksum)
                {
                    ACK_message();
                    Console.WriteLine("\nCORRECT information. Sending ACK");
                    if (header.sequenceNumber != lastSequenceNumber)
                    {
                        message_bytes.Add(messageBytes);
                        lastSequenceNumber = header.sequenceNumber;
                    }
                }
                else
                {
                    Console.WriteLine("\nINCORRECT information. Sending NACK");
                    send_NACK();
                }
                

                break;
            case 0b1111:

                messageBytes = new byte[buffer.Length - Header.HeaderData.header_size];
                Buffer.BlockCopy(buffer, Header.HeaderData.header_size, messageBytes, 0, messageBytes.Length);
                header = extract_header(buffer);
                crc_result = checksum_counter(messageBytes, 0);
                formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
                formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();
                Console.WriteLine(
                    $"\nRecived text message \"{Encoding.UTF8.GetString(messageBytes, 0, messageBytes.Length)}\" with sequence number {header.sequenceNumber}");


                if (formattedCrcResult == formattedHeaderChecksum)
                {
                    Console.WriteLine("\nCORRECT information. Sending ACK");
                    ACK_message();
                    if (header.sequenceNumber != lastSequenceNumber)
                    {
                        message_bytes.Add(messageBytes);
                        lastSequenceNumber = header.sequenceNumber;
                    }
                }
                else
                {
                    Console.WriteLine("\nINCORRECT information. Sending NACK");
                    send_NACK();
                    break;
                }

                message_bytes.Add(messageBytes);
                int totalLength = message_bytes.Sum(b => b.Length);
                complete_message = new byte[totalLength];
                int offset = 0;

                foreach (byte[] bytes in message_bytes)
                {
                    Buffer.BlockCopy(bytes, 0, complete_message, offset, bytes.Length);
                    offset += bytes.Length;
                }

                receivedMessage = Encoding.UTF8.GetString(complete_message);
                Console.WriteLine("\nReceived complete message: " + receivedMessage);
                Console.WriteLine($"Message received at: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                Console.WriteLine("********************************************************");
                Console.WriteLine("Choose an operation(m,f,q)");
                
                message_bytes.Clear();
                lastSequenceNumber = 0;
                break;
            default:
                Console.WriteLine("Neznamy typ spravy");
                break;
        }
    }

    public void ProcessFileMessage(byte[] buffer, byte msg_flag)
    {
        switch (msg_flag)
        {
            case 0b1011
                : //####################################################################FILE NAME //TODO: Osetrit chybu ale nevytvarat
                Program.is_sending = true;
                file_name = Encoding.UTF8.GetString(buffer, Header.HeaderData.header_size,
                    buffer.Length - Header.HeaderData.header_size);

                var header = extract_header(buffer);

                byte[] fileNameBytes = Encoding.UTF8.GetBytes(file_name); 
                crc_result = checksum_counter(fileNameBytes, 0);
                formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
                formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();

                if (formattedCrcResult == formattedHeaderChecksum)
                {
                    ACK_message();
                    //Console.WriteLine("\nCORRECT information. Sending ACK");
                    if (header.sequenceNumber != lastSequenceNumber)
                    {
                        message_bytes.Add(messageBytes);
                        lastSequenceNumber = header.sequenceNumber;
                    }
                }
                else
                {
                    send_NACK();
                    break;
                }

                Console.WriteLine($"File name is : {file_name}");
                file_path = path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                file_path += file_name;
                Console.WriteLine($"File path is : {file_path}");
                if (File.Exists(file_path))
                {
                    File.Delete(file_path);
                    Console.WriteLine("Deleted file at destination address");
                }

                break;
            case 0b0100: //###################################################################################DATA
                header = extract_header(buffer);

                fileBytes = new byte[buffer.Length - Header.HeaderData.header_size];
                Buffer.BlockCopy(buffer, Header.HeaderData.header_size, fileBytes, 0, fileBytes.Length);
                crc_result = checksum_counter(fileBytes, 0);
                formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
                formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();


                if (formattedCrcResult == formattedHeaderChecksum)
                {
                    Console.WriteLine($"Checksum is EQUAL. Sending ACK for packet {header.sequenceNumber}");
                    ACK_message();
                    if (header.sequenceNumber != lastSequenceNumber)
                    {
                        file_bytes.Add(fileBytes);
                        lastSequenceNumber = header.sequenceNumber;
                    }
                }
                else
                {
                    Console.WriteLine($"Checksum is NOT EQUAL. Sending NACK for packet {header.sequenceNumber}");
                    send_NACK();
                    break;
                }


                

                count++; // Increment the fragment count
                break;

            case 0b1111: //###################################################################################LAST FRAGMENT
                header = extract_header(buffer);
                fileBytes = new byte[buffer.Length - Header.HeaderData.header_size];
                Buffer.BlockCopy(buffer, Header.HeaderData.header_size, fileBytes, 0, fileBytes.Length);

                crc_result = checksum_counter(fileBytes, 0);
                formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
                formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();


                if (formattedCrcResult == formattedHeaderChecksum)
                {
                    Console.WriteLine($"Checksum is EQUAL. Sending ACK for packet {header.sequenceNumber}");
                    ACK_message();
                    if (header.sequenceNumber != lastSequenceNumber)
                    {
                        file_bytes.Add(fileBytes);
                        lastSequenceNumber = header.sequenceNumber;
                    }
                }
                else
                {
                    Console.WriteLine($"Checksum is NOT EQUAL. Sending NACK for packet {header.sequenceNumber}");
                    send_NACK();
                    break;
                }


                

                count++;
                Console.WriteLine($"Accessing: {file_path}");

                using (FileStream fs = new FileStream(file_path, FileMode.Create, FileAccess.Write))
                {
                    foreach (byte[] chunk in file_bytes)
                    {
                        fs.Write(chunk, 0, chunk.Length);
                    }
                }
                file_bytes.Clear();

                FileInfo fileInfo = new FileInfo(file_path);


                // Get the file size in bytes
                long fileSizeInBytes = fileInfo.Length;
                Console.WriteLine("All fragments received. Final file information:");
                Console.WriteLine($"File Size: {fileSizeInBytes} bytes");

                var receiveTime = DateTime.UtcNow;
                Console.WriteLine($"File received at: {receiveTime.ToString("HH:mm:ss.fff")}");

                // Optionally, you can calculate the final CRC of the entire file
                Console.Write($"Final CRC16 of the entire file {file_path}: ");
                fileBytes = File.ReadAllBytes(file_path);
                crc_result = checksum_counter(fileBytes, 0);
                Console.WriteLine(crc_result);
                path = "~/Downloads/";
                Console.WriteLine("********************************************************");
                Console.WriteLine("Choose an operation(m,f,q)");

                Program.is_sending = false;
                file_bytes.Clear();
                lastSequenceNumber = 0;
                break;
            default:
                Console.WriteLine("Neznamy typ pre subor");
                break;
        }
    }


    public (uint sequenceNumber, ushort checksum) extract_header(byte[] header_bytes)
    {
        byte[] sequence_number = new byte[3];
        sequence_number[0] = header_bytes[1];
        sequence_number[1] = header_bytes[2];
        sequence_number[2] = header_bytes[3];
        combined_sequence_number =
            (uint)(sequence_number[0] << 16 | sequence_number[1] << 8 | sequence_number[2]);
        byte[] checksum = new byte[2];
        checksum[0] = header_bytes[4];
        checksum[1] = header_bytes[5];
        combined_checksum = (ushort)(checksum[0] << 8 | checksum[1]);
        return (combined_sequence_number, combined_checksum);
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


    private void RespondToSYN()
    {
        Console.WriteLine("Sending SYN-ACK in response to SYN...");
        Header.HeaderData responseHeader = new Header.HeaderData();
        byte[] headerBytes = responseHeader.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.SYN_ACK, 1, 0);
        client.SendServiceMessage(Program.destination_ip, Program.source_sending_port,
            Program.destination_listening_port, headerBytes, Program.udpClient);
    }

    private void ACK_message()
    {
        Header.HeaderData responseHeader = new Header.HeaderData();
        byte[] headerBytes = responseHeader.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.ACK, 1, 0);
        client.SendServiceMessage(Program.destination_ip, Program.source_sending_port,
            Program.destination_listening_port, headerBytes, Program.udpClient);
    }

    private void send_NACK()
    {
        Header.HeaderData responseHeader = new Header.HeaderData();
        byte[] headerBytes = responseHeader.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.NACK, 1, 0);
        client.SendServiceMessage(Program.destination_ip, Program.source_sending_port,
            Program.destination_listening_port, headerBytes, Program.udpClient);
    }

    public void send_FIN_ACK()
    {
        Header.HeaderData responseHeader = new Header.HeaderData();
        byte[] headerBytes = responseHeader.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.FIN_ACK, 1, 0);
        client.SendServiceMessage(Program.destination_ip, Program.source_sending_port,
            Program.destination_listening_port, headerBytes, Program.udpClient);
        Program.FIN_ACK = true;
    }
}