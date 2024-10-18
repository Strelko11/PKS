namespace Computer1;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDP_server
{

    public byte type;
    public void Start(string udpIP, int udpPort)
    {
        using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            
            
        
            // Set the socket option to reuse the address
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        
            
            
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(udpIP), udpPort);
            sock.Bind(endPoint);
            Console.WriteLine($"Listening for connections on {udpIP}:{udpPort}");
                
            byte[] buffer = new byte[1024];

            while (Program.isRunning)
            {
                EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int bytesReceived = sock.ReceiveFrom(buffer, ref senderEndPoint);

                // Extract the header and message
                byte type = buffer[0];  // First byte for header type
                byte msgState = buffer[1];  // Second byte for message state

                // Convert the remaining bytes to a message
                string receivedMessage = Encoding.ASCII.GetString(buffer, 2, bytesReceived - 2); // Adjust for header size

                // Process the message based on the header type
                ProcessMessage(type);
            }
        }
    }


    public void ProcessMessage(byte receivedType)
    {
        switch (receivedType)
        {
            case Header.HeaderData.SYN:
                Console.WriteLine("SYN packet received");
                Program.SYN = true; // Update the flag in the Program class
                break;

            case Header.HeaderData.SYN_ACK:
                Console.WriteLine("SYN ACK packet received");
                Program.SYN_ACK = true; // Update the flag in the Program class
                break;

            case Header.HeaderData.ACK:
                Console.WriteLine("ACK packet received");
                Program.ACK = true; // Update the flag in the Program class
                break;

            default:
                Console.WriteLine("Unknown message type received.");
                break;
        }

        // You can also handle the message content based on msgState here if needed.
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
}
