
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
    public byte[] fileBytes;
    public string crc_result;

    public UDP_server(Client client)
    {
        this.client = client;
    }

    public void Start(string source_IP, int source_Port)
    {
        using (UdpClient udpClient = new UdpClient(source_Port))
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, source_Port);
            //sock.Bind(endPoint);
            //Console.WriteLine("Listening for connections on " + udpIP + ":" + udpPort);

            //byte[] buffer = new byte[1024];

            while (Program.isRunning)
            {
                //IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                //int bytesReceived = sock.ReceiveFrom(buffer, ref senderEndPoint);
                byte[] buffer = udpClient.Receive(ref endPoint);

                //byte type = buffer[0]; 
                //byte msgState = buffer[1];
                byte flag = buffer[0];
                byte type_flag = (byte)((flag >> 4) & 0b1111);
                byte msg_flag = (byte)(flag & 0b1111);
                
                /*receivedMessage = Encoding.ASCII.GetString(buffer, 7, buffer.Length - 7);//TODO: Zmenit accordingly k dlzke celej hlavicky

                if(receivedMessage == "exit"){
                    //Console.WriteLine("Exiting");
                    break;
                }
                else
                {
                    Console.WriteLine(
                        "Received message " + receivedMessage +" "+ type_flag +" "+ msg_flag);
                    Program.message_received = true;

                    ProcessMessageFlag(type_flag, msg_flag);
                }*/

                ProcessMessageFlag(type_flag, msg_flag, buffer);


            }
            //Console.WriteLine("Exited while");
        }
        //Console.WriteLine("Exited receive thread");
    }

    public void ProcessMessageFlag(byte type_flag, byte msg_flag, byte[] buffer)
    {
        switch (type_flag)
        {
            case 0b0000:
                Console.WriteLine("Servisna sprava");
                ProcessServiceMessages(msg_flag);
                break;
            case 0b0001:
                Console.WriteLine("Textova sprava");
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
                Program.SYN = true;
                Console.WriteLine("Servisna sprava SYN");
                RespondToSYN();
                break;
            case 0b0010:
                Program.SYN_ACK = true;
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
                    //Thread.Sleep(1000);
                    Program.message_ACK = true;
                    //Console.WriteLine("Sent ACK packet for message");
                }

                if (Program.keep_alive_sent)
                {
                    Program.keep_alive_sent = false;
                    Program.hearBeat_count--;
                }

                if (!Client.ACK_file)
                {
                    Client.ACK_file = true;
                }

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
                Console.WriteLine("Sprava bola prijata. Odosielam potvrdenie");
                receivedMessage = Encoding.ASCII.GetString(buffer, 8, buffer.Length - 8);
                var receiveTime = DateTime.UtcNow;
                Console.WriteLine($"Message received at: {receiveTime.ToString("HH:mm:ss.fff")}");
                if (receivedMessage != "exit")
                {
                    Console.WriteLine(
                        "Received message " + receivedMessage);
                    Program.message_received = true;
                }

                ACK_message();
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
            case 0b1011://####################################################################FILE NAME
                file_name = Encoding.ASCII.GetString(buffer, 8, buffer.Length - 8);
                Console.WriteLine($"File name is : {file_name}");
                file_path += file_name;
                Console.WriteLine($"File path is : {file_path}");
                if (File.Exists(file_path))
                {
                    File.Delete(file_path);
                    Console.WriteLine("Deleted file at destination address");
                }
                var header = extract_header(buffer);
               
                byte[] fileNameBytes = Encoding.ASCII.GetBytes(file_name); // Convert the file name to a byte array
                crc_result = checksum_counter(fileNameBytes, 0);
                
                Console.WriteLine("Calculated CRC for file name: " + crc_result);
                if (Convert.ToUInt16(crc.ToHexString(),16) == header.checksum)
                {
                    Console.WriteLine("Checksum is EQUAL");
                    Client.ACK_file = true;
                    ACK_message();
                }
                else
                {
                    Console.WriteLine("Checksum is NOT EQUAL");
                }
                break;
            case 0b0100://###################################################################################DATA
            // Extract the file data (payload) from the buffer (assuming the first 6 bytes are the header)
            header = extract_header(buffer); 
            
            fileBytes = new byte[header.payload];
            Buffer.BlockCopy(buffer, 8, fileBytes, 0, fileBytes.Length);
            crc_result = checksum_counter(fileBytes, 0);
            //crc.Append(fileBytes);

            Console.Write("CRC16 (current fragment): ");
            Console.WriteLine(crc_result);
            
            
            file_bytes.Add(fileBytes);

            count++; // Increment the fragment count

            Console.WriteLine($"Received fragment and appended to the file. Fragment {count} received.");
            ACK_message();
                break;
            
            case 0b1111://###################################################################################LAST FRAGMENT
                header = extract_header(buffer);
                Console.WriteLine("All fragments received. Final file information:");
                fileBytes = new byte[header.payload];
                Buffer.BlockCopy(buffer, 8, fileBytes, 0, fileBytes.Length);
                
                crc_result = checksum_counter(fileBytes, 0);
                Console.Write("CRC16 (current fragment): ");
                Console.WriteLine(crc_result);
                
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
                ACK_message();
                break;
            default:
                Console.WriteLine("Neznamy typ pre subor");
                break;
        }
    }


    public (uint sequenceNumber, ushort checksum, ushort payload) extract_header(byte[] header_bytes)
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
        byte[] data_size = new byte[2];
        data_size[0] = header_bytes[6];
        data_size[1] = header_bytes[7];
        combined_payload = (ushort)(data_size[0] << 8 | data_size[1]);
        
        Console.WriteLine($"Sequence number: {combined_sequence_number}");
        Console.WriteLine($"Checksum: {combined_checksum:X4}"); // Print checksum in hexadecimal format
        Console.WriteLine($"Payload size: {combined_payload}");
        return(combined_sequence_number, combined_checksum, combined_payload);
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
        //responseHeader.setFlag(Header.HeaderData.MSG_NONE, Header.HeaderData.SYN_ACK);
        //Console.WriteLine($"Data nastavene na {Header.HeaderData.MSG_NONE} + {Header.HeaderData.SYN_ACK}");
        //responseHeader.SetType(Header.HeaderData.SYN_ACK);
        //responseHeader.SetMsg(Header.HeaderData.MSG_NONE); 
        //responseHeader.sequence_number = 0;
        //responseHeader.checksum = 0;
        // Convert to byte array
        byte[] headerBytes = responseHeader.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.SYN_ACK, 1,0,0);
        client.SendServiceMessage(Program.destination_ip,Program.source_sending_port, Program.destination_listening_port, headerBytes);
        //Thread.Sleep(2000);
    }

    private void ACK_message(){
        Console.WriteLine("Sent ACK for received message");
        Header.HeaderData responseHeader = new Header.HeaderData();
        //responseHeader.setFlag(Header.HeaderData.MSG_NONE, Header.HeaderData.ACK);
        //responseHeader.SetType(Header.HeaderData.ACK);
        //responseHeader.SetMsg(Header.HeaderData.MSG_NONE);
        //responseHeader.sequence_number = 0;
        //responseHeader.checksum = 0;
        // Convert to byte array
        byte[] headerBytes = responseHeader.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.ACK,1,0,0);
        client.SendServiceMessage(Program.destination_ip,Program.source_sending_port, Program.destination_listening_port, headerBytes);
        Program.message_ACK_sent = true;
        Console.WriteLine("********************************************************");
        Console.WriteLine("Choose an operation(m,f,q)");

    }

    private void ACK_with_checksum()
    {
        Header.HeaderData responseHeader = new Header.HeaderData();
        responseHeader.setFlag(Header.HeaderData.MSG_NONE, Header.HeaderData.ACK);
    }
    
}


/*private void ProcessMessage(byte type, byte msgState, string message)
{
    // Handle based on the header type
    switch (type)
    {
        case Header.HeaderData.SYN:
            Console.WriteLine("Received SYN. Initiating connection.");
            // Respond with SYN-ACK
            break;

        case Header.HeaderData.SYN_ACK:
            Console.WriteLine("Received SYN-ACK. Connection established.");
            // Respond with ACK
            break;

        case Header.HeaderData.ACK:
            Console.WriteLine("Received ACK.");
            // Handle ACK
            break;

        case Header.HeaderData.DATA:
            // Handle data messages based on msgState
            switch (msgState)
            {
                case Header.HeaderData.MSG_TEXT:
                    Console.WriteLine("Received text message: " + message);
                    break;

                case Header.HeaderData.MSG_FILE:
                    Console.WriteLine("Received file.");
                    // Handle file data here
                    break;

                default:
                    Console.WriteLine("Unknown message state.");
                    break;
            }
            break;

        case Header.HeaderData.FIN:
            Console.WriteLine("Received FIN. Closing connection.");
            // Respond with FIN-ACK
            break;

        case Header.HeaderData.FIN_ACK:
            Console.WriteLine("Received FIN-ACK.");
            // Handle closing acknowledgment
            break;

        case Header.HeaderData.KEEP_ALIVE:
            Console.WriteLine("Received Keep-Alive message.");
            // Handle keep-alive
            break;

        case Header.HeaderData.NACK:
            Console.WriteLine("Received NACK.");
            // Handle negative acknowledgment
            break;

        case Header.HeaderData.LAST_FRAGMENT:
            Console.WriteLine("Received last fragment.");
            // Handle last fragment
            break;

        default:
            Console.WriteLine("Unknown header type.");
            break;
    }
}*/
/*public void ProcessMessage(byte receivedType)
    {
        switch (receivedType)
        {
            case 0x00:
                //Console.WriteLine("SYN packet received");
                Program.SYN = true;
                //Console.WriteLine($"SYN state: {Program.SYN}");
                RespondToSYN();
                break;

            case 0x02:
                //Console.WriteLine("SYN_ACK packet received");
                Program.SYN_ACK = true;
                //Console.WriteLine($"SYN_ACK state: {Program.SYN_ACK}");
                break;

            case 0x03:
                //Console.WriteLine("ACK packet received");
                if(!Program.handshake_complete){
                    Program.handshake_ACK = true;
                    Console.WriteLine("**************** HANDSHAKE COMPLETE *************\n\n");
                    Program.handshake_complete = true;
                }
                else{
                    //Thread.Sleep(1000);
                    Program.message_ACK = true;
                    //Console.WriteLine("Sent ACK packet for message");
                }

                if (Program.keep_alive_sent)
                {
                    Program.keep_alive_sent = false;
                    Program.hearBeat_count--;
                }

                //Console.WriteLine($"ACK state: {Program.ACK}");
                break;
            case 0x08:
                break;

            default:
                //Console.WriteLine("Message received");
                break;
        }
    }*/
    
/*private void respondToKeepAlive()
{
   Header.HeaderData responseHeader = new Header.HeaderData();
   responseHeader.SetType(Header.HeaderData.KEEP_ALIVE);
   responseHeader.SetMsg(Header.HeaderData.MSG_NONE);
   client.SendMessage(Program.destination_ip,Program.source_sending_port, Program.destination_listening_port, "ACK", responseHeader);
}*/
/*string filePath = "/Users/macbook/Desktop/received.txt";  // Define the file path where you want to save the received file
        byte[] fileBytes = new byte[buffer.Length - 6];
        Buffer.BlockCopy(buffer, 6, fileBytes, 0, fileBytes.Length);
        string receivedText = Encoding.ASCII.GetString(fileBytes);
        Console.WriteLine(receivedText);  // Print the actual text received
        crc = CrcAlgorithm.CreateCrc16CcittFalse();
        crc.Append(fileBytes);

        Console.Write("CRC16 (current fragment): ");
        Console.WriteLine(crc.ToHexString());
        // Save the received file data to a file
        File.WriteAllBytes(filePath, fileBytes);
        Console.WriteLine("File received and saved successfully to " + filePath);
        FileInfo fileInfo = new FileInfo(filePath);

        // Get the file size in bytes
        long fileSizeInBytes = fileInfo.Length;

        Console.WriteLine($"File Size: {fileSizeInBytes} bytes");
        var receiveTime = DateTime.UtcNow;
        Console.WriteLine($"File received at: {receiveTime.ToString("HH:mm:ss.fff")}");*/
        
/*byte[] sequence_number = new byte[3];
                sequence_number[0] = buffer[1];
                sequence_number[1] = buffer[2];
                sequence_number[2] = buffer[3];
                combined_sequence_number =
                    (uint)(sequence_number[0] << 16 | sequence_number[1] << 8 | sequence_number[2]);
                byte[] checksum = new byte[2];
                checksum[0] = buffer[4];
                checksum[1] = buffer[5];
                combined_checksum = (ushort)( checksum[0] << 8 | checksum[1]);
                byte[] data_size = new byte[2];
                data_size[0] = buffer[6];
                data_size[1] = buffer[7];
                combined_payload = (ushort)(data_size[0] << 8 | data_size[1]);*/        
        