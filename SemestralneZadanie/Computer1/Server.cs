namespace Computer1;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDP_server
{

    public byte type;
    private static Client client;

    public void Start(string udpIP, int udpPort)
    {
        using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(udpIP), udpPort);
            sock.Bind(endPoint);
            //Console.WriteLine("Listening for connections on " + udpIP + ":" + udpPort);

            byte[] buffer = new byte[1024];

            while (Program.isRunning)
            {
                EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int bytesReceived = sock.ReceiveFrom(buffer, ref senderEndPoint);
                byte type = buffer[0]; // First byte for header type
                byte msgState = buffer[1]; // Second byte for message state
                string receivedMessage = Encoding.ASCII.GetString(buffer, 2, bytesReceived - 2);
                Console.WriteLine(
                    "Received message " + receivedMessage + "with type" + type + "and msgState" + msgState);
                ProcessMessage(type);
            }
        }
    }

    

    public void ProcessMessage(byte receivedType)
    {
        switch (receivedType)
        {
            case 0x00:
                Console.WriteLine("SYN packet received");
                Program.SYN = true; // Update the flag in the Program class
                Console.WriteLine($"SYN state: {Program.SYN}");
                // Respond with SYN-ACK
                RespondToSYN();
                break;

            case 0x02:
                Console.WriteLine("SYN_ACK packet received");
                Program.SYN_ACK = true; // Update the flag in the Program class
                Console.WriteLine($"SYN_ACK state: {Program.SYN_ACK}");
                break;

            case 0x03:
                Console.WriteLine("ACK packet received");
                Program.ACK = true; // Update the flag in the Program class
                Console.WriteLine($"ACK state: {Program.ACK}");
                break;

            default:
                Console.WriteLine("Unknown message type received.");
                break;
        }
    }

// Example: Method to respond to SYN with SYN-ACK
    private void RespondToSYN()
    {
        Console.WriteLine("Sending SYN-ACK in response to SYN...");
        // Create a new header or modify the current one to represent SYN-ACK
        Header.HeaderData responseHeader = new Header.HeaderData();
        responseHeader.SetType(Header.HeaderData.SYN_ACK);
        responseHeader.SetMsg(Header.HeaderData.MSG_NONE); // No additional message payload
        // Send the SYN-ACK response back to the client
        client.SendMessage(Program.destination_ip, Program.destination_port, "SYN_ACK", responseHeader);
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

