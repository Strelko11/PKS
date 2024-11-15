using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using InvertedTomato.Crc;



namespace Computer1
{
    class Program
    {
        private static Client udpClient = new Client(udpServer);
        private static UDP_server udpServer = new UDP_server(udpClient);
        public static string destination_ip;
        private static string source_ip = "127.0.0.1";
        public static int destination_listening_port;
        public static int destination_sending_port;
        public static int source_listening_port;
        public static int source_sending_port;
        private static string message;
        public static bool isRunning;
        private static Header.HeaderData header = new Header.HeaderData();
        public static bool handshake_SYN = false;
        public static bool handshake_SYN_ACK = false;
        public static bool handshake_ACK = false;
        public static bool iniciator = false;
        public static bool handshake_complete = false;
        public static bool message_received = false;
        public static bool message_ACK = true;
        public static bool message_ACK_sent = false;
        public static bool message_sent = false;
        public static System.Timers.Timer hearbeatTimer;
        public static int hearBeat_count = 0;
        public static bool keep_alive_sent = false;
        public static byte[] headerBytes /*= new byte[7]*/;
        public static Stopwatch stopwatch = new Stopwatch();
        public static ushort packet_size;
        public static bool stop_wait_ACK = false;
        public static bool stop_wait_NACK = false;
        public static bool mistake = false;
        public static bool ACK = false;
        public static bool NACK = false;

        static int Main(string[] args)
        {
            //Console.WriteLine("Enter source IP address:");
            //source_ip = Console.ReadLine();

            // Console.WriteLine("Enter destination IP address:");
            // destination_ip = Console.ReadLine();

            // Console.WriteLine("Enter destination listening port:");
            // string input = Console.ReadLine();
            // destination_listening_port = int.Parse(input);

            // Console.WriteLine("Enter destination sending port:");
            // input = Console.ReadLine();
            // destination_sending_port = int.Parse(input);


            // Console.WriteLine("Enter source listening port:");
            // input = Console.ReadLine();
            // source_listening_port = int.Parse(input);

            // Console.WriteLine("Enter source sending port:");
            // input = Console.ReadLine();
            // source_sending_port = int.Parse(input);

            if (args.Length < 5)
            {
                Console.WriteLine("Usage: <destination_ip> <destination_listening_port> <destination_sending_port> <source_listening_port> <source_sending_port>");
                return 0;
            }

            destination_ip = args[0];
            destination_listening_port = int.Parse(args[1]);
            destination_sending_port = int.Parse(args[2]);
            source_listening_port = int.Parse(args[3]);
            source_sending_port = int.Parse(args[4]);
            string respone = args[5];

            isRunning = true;

            Thread receiveThread = new Thread(() => receive_thread(source_ip, source_listening_port));
            receiveThread.Start();
            Thread sendThread = new Thread(() => send_thread(destination_ip, destination_listening_port));
            sendThread.Start();



            // Console.WriteLine("Do you want to initiate the handshake? (y/n)");
            // input = Console.ReadLine();
            // if(input == "y"){
            //     iniciator = true;
            // }

            //Console.WriteLine("Is this device an initiator(y/n):");
            //var respone = Console.ReadLine();
            if (respone == "y")
            {
                iniciator = true;
            }

            Console.WriteLine("\n\n\n\n********************************************************************");
            Console.WriteLine("After both devices have been set up:");



            if (iniciator)
            {
                Console.WriteLine("Press Enter to initiate the handshake...");
                Console.ReadLine();
                handshake(destination_ip, destination_listening_port, true);
            }
            else
            {
                Console.WriteLine("Waiting for the other device to initiate the handshake...");
            }

            if (handshake_complete && iniciator)
            {
                //hearbeatTimer = new System.Timers.Timer(5000);  // 5000 milliseconds (5 seconds)
                //hearbeatTimer.Elapsed += OnHeartBeat;  // Subscribe to the Elapsed event
                //hearbeatTimer.AutoReset = true;  // Make the timer repeat
                //hearbeatTimer.Enabled = true;  // Start the timer

            }

            
            

            //Console.WriteLine("Press enter to exit");
            //Console.ReadLine();
            //isRunning = false;

            sendThread.Join();
            //Console.WriteLine("Exited send thread");
            receiveThread.Join();
            Console.WriteLine("Exiting");
            return 0;

        }

        public static void handshake(string destination_ip, int destination_listening_port, bool iniciator)
        {
            Console.WriteLine("***************** HANDSHAKE *****************");
            if (iniciator)
            {
                Console.WriteLine("SYN packet sent");
                while (!handshake_SYN_ACK)
                {
                    headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.SYN, 1,0);
                    udpClient.SendServiceMessage(destination_ip,source_sending_port, destination_listening_port, headerBytes);
                }
                headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.ACK, 1,0);
                udpClient.SendServiceMessage(destination_ip,source_sending_port, destination_listening_port, headerBytes);
                Console.WriteLine("Handshake complete!");
                handshake_complete = true;
                Console.WriteLine("**************** HANDSHAKE COMPLETE *************\n");
                
            }

        }


        public static void send_thread(string destination_ip, int destination_listening_port)
        {
            while (isRunning)
            {
                if (handshake_complete && message_ACK)
                {
                    Console.WriteLine("********************************************************");
                    Console.WriteLine("Choose an operation(m,f,q)");
                    string command = Console.ReadLine();
                    if (command == "q")
                    {
                        udpClient.SendMessage(source_ip, source_sending_port, source_listening_port, 0,"exit", mistake);
                        isRunning = false;
                        continue;
                    }

                    if (command == "m")
                    {   
                        Console.WriteLine("Type message:");
                        message = Console.ReadLine();
                        Console.WriteLine("Do you want to create a mistake? (y/n)");
                        if (Console.ReadLine() == "y")
                        {
                            mistake = true;
                        }
                        else
                        {
                            mistake = false;
                        }
                        Console.WriteLine("Enter packet size(1-1465 bytes): ");
                        do
                        { 
                            packet_size = ushort.Parse(Console.ReadLine());
                            if (packet_size > 1465 || packet_size < 1)
                            {
                                Console.WriteLine("Invalid packet size. Enter again:");
                            }
                            
                        } while (packet_size > 1465 || packet_size < 1);
                        udpClient.SendMessage(destination_ip,source_sending_port, destination_listening_port,packet_size, message, mistake);
                        //udpClient.SendMessage(destination_ip, source_sending_port, destination_listening_port, message, headerBytes);
                        //Console.WriteLine("Waiting for ACK");
                        if (iniciator)
                        {
                            //ResetHeartBeatTimer();//TODO: Turned of for testing purposes
                        }
                    }

                    if (command == "f")
                    {
                        Console.WriteLine("Sending files");
                        Console.WriteLine("Do you want to create a mistake? (y/n)");
                        if (Console.ReadLine() == "y")
                        {
                            mistake = true;
                        }
                        else
                        {
                            mistake = false;
                        }
                        Console.WriteLine("Enter packet size(1-1465 bytes): ");
                        do
                        { 
                            packet_size = ushort.Parse(Console.ReadLine());
                            if (packet_size > 1465 || packet_size < 1)
                            {
                                Console.WriteLine("Invalid packet size. Enter again:");
                            }
                            
                        } while (packet_size > 1465 || packet_size < 1);
                        //string filePath = "/Users/macbook/Desktop/UI Strelec 2a.pdf";
                        string filePath = "/Users/macbook/Desktop/test.txt";
                        udpClient.SendFile(destination_ip,source_sending_port, destination_listening_port, filePath, packet_size,mistake);
                    }
                }
            }
            //Console.WriteLine("Exited program");
        }



        public static void receive_thread(string source_ip, int source_port)
        {
            //while (isRunning)
            //{
                //header.SetType(Header.HeaderData.TEST);
                udpServer.Start(source_ip, source_port);
               
                if (iniciator)
                {
                    //ResetHeartBeatTimer();//TODO: Turned of for testing purposes
                }
            //}
            //Console.WriteLine("Exited receive thread");

        }

        private static void OnHeartBeat(object sender, ElapsedEventArgs e)
        {
            if (hearBeat_count >= 3)
            {
                Console.WriteLine("Connection lost");
                return;
            }
            Console.WriteLine($"Hearbeat count: {hearBeat_count}");
            headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.KEEP_ALIVE, 1, 0);
            udpClient.SendServiceMessage(destination_ip, source_sending_port, destination_listening_port, headerBytes);
            //udpClient.SendMessage(destination_ip, source_sending_port, destination_listening_port, 0,"KEEP_Alive", mistake);
            keep_alive_sent = true;
            hearBeat_count++;
        }

        private static void ResetHeartBeatTimer()
        {
            if (isRunning)
            {
                hearbeatTimer.Stop();
                hearbeatTimer.Start();
            }
        }
    }
}