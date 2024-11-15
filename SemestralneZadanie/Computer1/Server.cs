
namespace Computer1;
using System.Net;
using System.Net.Sockets;
using System.Text;
using InvertedTomato.Crc;

public class UDP_server
{

    public byte type;
    private Client client;
    public string receivedMessage;
    public int count = 0;
    private CrcAlgorithm crc;
    public uint combined_sequence_number;
    public ushort combined_checksum;
    public ushort combined_payload;
    public string file_name;
    public string file_path = "/Users/macbook/Downloads/";
    public byte[] file;
    public List<byte[]> file_bytes = new List<byte[]>();
    public List<byte[]> message_bytes = new List<byte[]>();
    public byte[] fileBytes;
    public string crc_result;
    public byte[] messageBytes;
    public byte[] complete_message;
    public string formattedCrcResult;
    public string formattedHeaderChecksum;

    public UDP_server(Client client)
    {
        this.client = client;
    }

    public void Start(string source_IP, int source_Port)
    {
        using (UdpClient udpClient = new UdpClient(source_Port))
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, source_Port);

            while (Program.isRunning)
            {
                byte[] buffer = udpClient.Receive(ref endPoint);
                byte flag = buffer[0];
                byte type_flag = (byte)((flag >> 4) & 0b1111);
                byte msg_flag = (byte)(flag & 0b1111);
                ProcessMessageFlag(type_flag, msg_flag, buffer);
            }
        }
    }

    public void ProcessMessageFlag(byte type_flag, byte msg_flag, byte[] buffer)
    {
        switch (type_flag)
        {
            case 0b0000:
                //Console.WriteLine("Servisna sprava");
                ProcessServiceMessages(msg_flag);
                break;
            case 0b0001:
                //Console.WriteLine("Textova sprava");
                ProcessTextMessage(msg_flag, buffer);
                break;
            case 0b0010:
                Console.WriteLine("Subor");
                ProcessFileMessage(buffer, msg_flag);
                break;
        }
    }

    public void ProcessServiceMessages(byte msg_flag)
    {
        switch (msg_flag)
        {
            case 0b0000:
                Program.handshake_SYN = true;
                Console.WriteLine("Servisna sprava SYN");
                RespondToSYN();
                break;
            case 0b0010:
                Program.handshake_SYN_ACK = true;
                Console.WriteLine("Servisna sprava SYN_ACK");

                //Console.WriteLine("Tu som sa dostal");
                break;
            case 0b0011:
                Console.WriteLine("Servisna sprava ACK");
                if (!Program.handshake_complete)
                {
                    Program.handshake_ACK = true;
                    Console.WriteLine("**************** HANDSHAKE COMPLETE *************\n\n");
                    Program.handshake_complete = true;
                }
                else
                {
                    Program.NACK = false;
                    Program.ACK = true;
                    //Console.WriteLine("TA SAK TU SOM");
                    //Console.WriteLine("Sent ACK packet for message");
                }

                if (Program.keep_alive_sent)
                {
                    Program.keep_alive_sent = false;
                    Program.hearBeat_count--;
                }

                
                break;
            case 0b1001:
                Console.WriteLine("Recived NACK");
                Program.NACK = true;
                Program.ACK = false;
                //Console.WriteLine("TI DRBE");
                break;
            case 0b1000:
                Console.WriteLine("Servisna sprava KEEP_ALIVE");
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
                crc_result = checksum_counter(messageBytes,0);
                var header = extract_header(buffer);
                Console.WriteLine($"Calculated CRC16: {crc_result}");
                Console.WriteLine($"Header Checksum: {header.checksum.ToString("X4")}");
                formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
                formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();
                
                if (formattedCrcResult == formattedHeaderChecksum)
                {
                    Console.WriteLine("Checksum is EQUAL");
                    ACK_message();
                    message_bytes.Add(messageBytes);
                }
                else
                {
                    Console.WriteLine("Checksum is NOT EQUAL");
                    send_NACK();
                }
                Console.WriteLine("**************************************");
                break;
            case 0b1111:
                messageBytes = new byte[buffer.Length - Header.HeaderData.header_size];
                Buffer.BlockCopy(buffer, Header.HeaderData.header_size, messageBytes, 0, messageBytes.Length);
                header = extract_header(buffer);
                crc_result = checksum_counter(messageBytes,0);
                Console.WriteLine($"Calculated CRC16: {crc_result}");
                Console.WriteLine($"Header Checksum: {header.checksum.ToString("X4")}");
                formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
                formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();
                
                
                if (formattedCrcResult == formattedHeaderChecksum)
                {
                    Console.WriteLine("Checksum is EQUAL");
                    ACK_message();
                }
                else
                {
                    Console.WriteLine("Checksum is NOT EQUAL");
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

                // Print or use the complete message
                receivedMessage = Encoding.UTF8.GetString(complete_message);
                Console.WriteLine("Received complete message: " + receivedMessage);
                Program.message_received = true;

                // Clear the list after processing the complete message
                message_bytes.Clear();
                
                
                if (receivedMessage != "exit")
                {
                    Console.WriteLine(
                        "Received message " + receivedMessage);
                    Program.message_received = true;
                }
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
            case 0b1011://####################################################################FILE NAME //TODO: Osetrit chybu ale nevytvarat
                file_name = Encoding.UTF8.GetString(buffer, Header.HeaderData.header_size, buffer.Length - Header.HeaderData.header_size);
                
                var header = extract_header(buffer);
               
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(file_name); // Convert the file name to a byte array
                crc_result = checksum_counter(fileNameBytes, 0);
                
                Console.WriteLine($"Calculated CRC16: {crc_result}");
                Console.WriteLine($"Header Checksum: {header.checksum.ToString("X4")}");
                formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
                formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();
                
                if (formattedCrcResult == formattedHeaderChecksum)
                {
                    Console.WriteLine("Checksum is EQUAL");
                    ACK_message();
                    
                }
                else
                {
                    Console.WriteLine("Checksum is NOT EQUAL");
                    send_NACK();
                    break;
                }
                Console.WriteLine($"File name is : {file_name}");
                file_path += file_name;
                Console.WriteLine($"File path is : {file_path}");
                if (File.Exists(file_path))
                {
                    File.Delete(file_path);
                    Console.WriteLine("Deleted file at destination address");
                }
                break;
            case 0b0100://###################################################################################DATA
            // Extract the file data (payload) from the buffer (assuming the first 6 bytes are the header)
            header = extract_header(buffer); 
            
            fileBytes = new byte[buffer.Length - Header.HeaderData.header_size];
            Buffer.BlockCopy(buffer, Header.HeaderData.header_size, fileBytes, 0, fileBytes.Length);
            crc_result = checksum_counter(fileBytes, 0);
            //crc.Append(fileBytes);

            Console.WriteLine($"Calculated CRC16: {crc_result}");
            Console.WriteLine($"Header Checksum: {header.checksum.ToString("X4")}");
            formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
            formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();

           
            if (formattedCrcResult == formattedHeaderChecksum)
            {
                Console.WriteLine("Checksum is EQUAL");
                Console.WriteLine($"Sending ACK for packet {header.sequenceNumber}");
                ACK_message();
            }
            else
            {
                Console.WriteLine("Checksum is NOT EQUAL");
                Console.WriteLine($"Sending NACK for packet {header.sequenceNumber}");
                send_NACK();
                break;
            }
            
            
            file_bytes.Add(fileBytes);

            count++; // Increment the fragment count

            Console.WriteLine($"Received fragment and appended to the file. Fragment {count} received.");
           
                break;
            
            case 0b1111://###################################################################################LAST FRAGMENT
                header = extract_header(buffer);
                Console.WriteLine("All fragments received. Final file information:");
                fileBytes = new byte[buffer.Length - Header.HeaderData.header_size];
                Buffer.BlockCopy(buffer, Header.HeaderData.header_size, fileBytes, 0, fileBytes.Length);
                
                crc_result = checksum_counter(fileBytes, 0);
                Console.Write("CRC16 (current fragment): ");
                Console.WriteLine(crc_result);
                Console.WriteLine($"Calculated CRC16: {crc_result}");
                Console.WriteLine($"Header Checksum: {header.checksum.ToString("X4")}");
                formattedCrcResult = crc_result.PadLeft(4, '0').ToUpper();
                formattedHeaderChecksum = header.checksum.ToString("X4").ToUpper();

                
                
                if (formattedCrcResult == formattedHeaderChecksum)
                {
                    Console.WriteLine("Checksum is EQUAL");
                    ACK_message();
                }
                else
                {
                    Console.WriteLine("Checksum is NOT EQUAL");
                    send_NACK();
                    break;
                }

                
                file_bytes.Add(fileBytes);

                count++;

                Console.WriteLine($"Received fragment and appended to the file. Fragment {count} received.");
                using (FileStream fs = new FileStream(file_path, FileMode.Create, FileAccess.Write))
                {
                    foreach (byte[] file_bytes in file_bytes)
                    {
                        fs.Write(file_bytes, 0, file_bytes.Length);
                    }
                }
                //File.WriteAllBytes(file_path, file_bytes);
                FileInfo fileInfo = new FileInfo(file_path);
                

                // Get the file size in bytes
                long fileSizeInBytes = fileInfo.Length;

                Console.WriteLine($"File Size: {fileSizeInBytes} bytes");

                var receiveTime = DateTime.UtcNow;
                Console.WriteLine($"File received at: {receiveTime.ToString("HH:mm:ss.fff")}");

                // Optionally, you can calculate the final CRC of the entire file
                Console.Write($"Final CRC16 of the entire file {file_path}: ");
                fileBytes = File.ReadAllBytes(file_path);
                crc_result = checksum_counter(fileBytes, 0);
                Console.WriteLine(crc_result);
                
                break;
            default:
                Console.WriteLine("Neznamy typ pre subor");
                break;
        }
    }


    public (uint sequenceNumber, ushort checksum) extract_header(byte[] header_bytes)
    {
        byte []sequence_number = new byte[3];
        sequence_number[0] = header_bytes[1];
        sequence_number[1] = header_bytes[2];
        sequence_number[2] = header_bytes[3];
        combined_sequence_number =
            (uint)(sequence_number[0] << 16 | sequence_number[1] << 8 | sequence_number[2]);
        byte[] checksum = new byte[2];
        checksum[0] = header_bytes[4];
        checksum[1] = header_bytes[5];
        combined_checksum = (ushort)( checksum[0] << 8 | checksum[1]);
        
        Console.WriteLine($"Sequence number: {combined_sequence_number}");
        Console.WriteLine($"Checksum: {combined_checksum:X4}"); // Print checksum in hexadecimal format
        return(combined_sequence_number, combined_checksum);
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
        return crc.ToHexString();
    }




    private void RespondToSYN()
    {
        Console.WriteLine("Sending SYN-ACK in response to SYN...");
        Header.HeaderData responseHeader = new Header.HeaderData();
        byte[] headerBytes = responseHeader.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.SYN_ACK, 1,0);
        client.SendServiceMessage(Program.destination_ip,Program.source_sending_port, Program.destination_listening_port, headerBytes);
    }

    private void ACK_message(){
        //Console.WriteLine("Sent ACK for received message");
        Header.HeaderData responseHeader = new Header.HeaderData();
        byte[] headerBytes = responseHeader.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.ACK,1,0);
        client.SendServiceMessage(Program.destination_ip,Program.source_sending_port, Program.destination_listening_port, headerBytes);
        Program.message_ACK_sent = true;
        //Console.WriteLine("********************************************************");
        //Console.WriteLine("Choose an operation(m,f,q)");
    }

    private void send_NACK()
    {
        //Console.WriteLine("Sending NACK");
        Header.HeaderData responseHeader = new Header.HeaderData();
        byte[] headerBytes = responseHeader.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.NACK, 1,0);
        client.SendServiceMessage(Program.destination_ip,Program.source_sending_port, Program.destination_listening_port, headerBytes);
    }

    private void ACK_with_checksum()
    {
        Header.HeaderData responseHeader = new Header.HeaderData();
        responseHeader.setFlag(Header.HeaderData.MSG_NONE, Header.HeaderData.ACK);
    }
    
}

/*if (!Client.ACK_file)
                {
                    Client.ACK_file = true;
                }

                if (Program.stop_wait_NACK)
                {
                    Program.stop_wait_NACK = false;
                    Program.stop_wait_ACK = true;
                }

                if (Client.file_sent)
                {
                    Client.NACK_file = false;
                    Client.ACK_file = true;
                }*/

        