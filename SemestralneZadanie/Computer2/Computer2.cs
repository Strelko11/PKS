using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;


namespace Computer2
{
    class Program
    {
        private static UDP_client udpClient = new UDP_client();
        private static UDP_server udpServer = new UDP_server();
        //private static string udpIP = "10.10.77.21"; // Change as needed
        private static string destination_ip;
        private static string source_ip = "192.168.1.2";
        private static int destination_port;
        private static int source_port;
        private static string message;
        public static bool isRunning;
        private static Header.HeaderData header = new Header.HeaderData();
        public static bool SYN = false;
        public static bool SYN_ACK = false;
        public static bool ACK = false;


        static void Main(string[] args)
        {
            Console.WriteLine("Enter destination IP address:");
            destination_ip = Console.ReadLine();
            
            Console.WriteLine("Enter destination port:");
            string input = Console.ReadLine();
            destination_port = int.Parse(input);
            
            Console.WriteLine("Enter source port:");
            input = Console.ReadLine();
            source_port = int.Parse(input);
            
            Console.WriteLine("Press enter if both devices are ready to be connected");
            Console.ReadLine();
            
            isRunning = true;
            
            Thread sendThread = new Thread(() => send_thread(destination_ip, destination_port));
            sendThread.Start();

            Thread receiveThread = new Thread(() => receive_thread(source_ip, source_port));
            receiveThread.Start();
            
            //Console.WriteLine("Press enter to exit");
            //Console.ReadLine();

            //isRunning = false;
            
            sendThread.Join();
            receiveThread.Join();
            

        }

        public static void send_thread(string destination_ip, int destination_port)
        {
            header.SetType(Header.HeaderData.SYN);
            header.SetMsg(Header.HeaderData.MSG_NONE);

            // Send initial SYN message
            udpClient.send_message(destination_ip, destination_port, "SYN", header);
            

            // Wait for SYN-ACK
            do
            {
                Console.WriteLine("Waiting for SYN ACK");
                Thread.Sleep(500);
            } while (!SYN_ACK);

            // Send ACK after receiving SYN-ACK
            udpClient.send_message(destination_ip, destination_port, "ACK", header);
            

            if (ACK && SYN_ACK && SYN)
            {
                // Message sending loop
                while (isRunning)
                {
                    Console.WriteLine("Enter message you want to send (type 'exit' to quit):");
                    message = Console.ReadLine();
                    if (message == "exit")
                    {
                        isRunning = false;
                        continue;
                    }

                    // Send the actual message
                    udpClient.send_message(destination_ip, destination_port, message, header);
                }
            }
        }

        public static void receive_thread(string source_ip, int source_port)
        {
            udpServer.Start(source_ip, source_port);
        }
    }




    /*Console.Write("Do you want to send or receive messages? (s/r)");
            string input = Console.ReadLine();

            if (input == "s")
            {
                Thread sendThread = new Thread(() => send_thread(udpIP, port_send, message));
                sendThread.Start();
                //send_thread(udpIP, port_send, message);
            }

            if (input == "r")
            {
                Thread receiveThread = new Thread(() => receive_thread(udpIP, port_listen));
                receiveThread.Start();
            }*/
}
/*// Start the receive thread
            Thread receiveThread = new Thread(() => receive_thread(receive_ip, port_listen));
            receiveThread.Start();

            // Start the send thread
            Thread sendThread = new Thread(() => send_thread(send_ip, port_send, message));
            sendThread.Start();

            // Wait for threads to finish (in this case, they won't unless stopped)
            sendThread.Join();
            receiveThread.Join();*/
