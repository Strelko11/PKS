using System.Net.Sockets;
using System.Timers;


namespace Computer1
{
    class Program
    {
        private static Client client = new Client(server);
        private static UDP_server server = new UDP_server(client);
        public static UdpClient udpClient;
        public static string destination_ip;
        private static string source_ip = "192.168.1.2";
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
        public static System.Timers.Timer hearbeatTimer;
        public static int heartBeat_count = 1;
        public static bool keep_alive_sent;
        public static byte[] headerBytes;
        public static ushort packet_size;
        public static bool mistake;
        public static bool ACK = true;
        public static bool NACK = false;
        public static bool KEEP_ALIVE_ACK = false;
        public static Thread receiveThread;
        public static Thread sendThread;
        public static bool FIN_received = false;
        public static bool FIN_sent = false;
        public static bool FIN_ACK = false;
        public static bool is_sending = false;
        public static string input;
        static DateTime lastSentTime = DateTime.MinValue;
        public static string filePath;



        static int Main(string[] args)
        {
            Console.WriteLine("Enter source IP address:");
            source_ip = Console.ReadLine();

            Console.WriteLine("Enter destination IP address:");
            destination_ip = Console.ReadLine();

            Console.WriteLine("Enter destination listening port:");
            string input = Console.ReadLine();
            destination_listening_port = int.Parse(input);

            Console.WriteLine("Enter destination sending port:");
            input = Console.ReadLine();
            destination_sending_port = int.Parse(input);


            Console.WriteLine("Enter source listening port:");
            input = Console.ReadLine();
            source_listening_port = int.Parse(input);

            Console.WriteLine("Enter source sending port:");
            input = Console.ReadLine();
            source_sending_port = int.Parse(input);


            /*if (args.Length < 5)
            {
                Console.WriteLine(
                    "Usage: <destination_ip> <destination_listening_port> <destination_sending_port> <source_listening_port> <source_sending_port>");
                return 0;
            }

            destination_ip = args[0];
            destination_listening_port = int.Parse(args[1]);
            destination_sending_port = int.Parse(args[2]);
            source_listening_port = int.Parse(args[3]);
            source_sending_port = int.Parse(args[4]);
            string respone = args[5];*/

            isRunning = true;
            udpClient = new UdpClient(source_sending_port);

            receiveThread = new Thread(() => receive_thread(source_listening_port));
            receiveThread.Start();
            sendThread = new Thread(() => send_thread(destination_ip, destination_listening_port));
            sendThread.Start();


            Console.WriteLine("Do you want to initiate the handshake? (y/n)");
            input = Console.ReadLine();
            if(input == "y"){
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
                hearbeatTimer = new System.Timers.Timer(5000); 
                hearbeatTimer.Elapsed += OnHeartBeat; 
                hearbeatTimer.AutoReset = true; 
                hearbeatTimer.Enabled = true; 
            }


            sendThread.Join();
            receiveThread.Join();
            udpClient.Close();
            Console.WriteLine("Bye Bye");
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
                    // Check if 5 seconds have passed since the last message was sent
                    if ((DateTime.Now - lastSentTime).TotalSeconds >= 5)
                    {
                        headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.SYN, 1, 0);
                        client.SendServiceMessage(destination_ip, source_sending_port, destination_listening_port,
                            headerBytes, udpClient);
                        lastSentTime = DateTime.Now; 
                    }

                    Thread.Sleep(1); 
                }

                headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.ACK, 1, 0);
                client.SendServiceMessage(destination_ip, source_sending_port, destination_listening_port, headerBytes,
                    udpClient);
                Console.WriteLine("ACK packet sent");
                Console.WriteLine("Handshake complete!");
                handshake_complete = true;
                Console.WriteLine("**************** HANDSHAKE COMPLETE *************\n");
            }
        }


        public static void send_thread(string destination_ip, int destination_listening_port)
        {
            StartHeartBeatTimer();
            while (isRunning)
            {
                {
                    if (handshake_complete && ACK)
                    {
                        
                        Console.WriteLine("********************************************************");
                        Console.WriteLine("Choose an operation(m,f,q)");
                        string command = Console.ReadLine();
                        if (command == "q")
                        {
                            headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.FIN, 1, 0);
                            client.SendServiceMessage(destination_ip, source_sending_port, destination_listening_port,
                                headerBytes, udpClient);
                            FIN_sent = true;
                        }

                        if (command == "m")
                        {
                            Console.WriteLine("Type message:");
                            message = Console.ReadLine();
                            do
                            {
                                Console.WriteLine("Do you want to create a mistake? (y/n):");
                                input = Console.ReadLine()?.ToLower(); 

                                if (input == "y")
                                {
                                    mistake = true;
                                    break; 
                                }
                                if (input == "n")
                                {
                                    mistake = false;
                                    break; 
                                }
                                else
                                {
                                    Console.WriteLine("Invalid input. Try again!");
                                }
                            } while (true);
                            Console.WriteLine("Enter packet size(1-1466 bytes): ");
                            do
                            {
                                input = Console.ReadLine();
                                if (ushort.TryParse(input, out packet_size) && packet_size >= 1 && packet_size <= 1465)
                                {
                                    break; 
                                }
                                Console.WriteLine("Invalid packet size. Enter again:");
                            } while (true);

                            client.SendMessage(destination_ip, source_sending_port, destination_listening_port,
                                packet_size, message, mistake, udpClient);
                        }

                        if (command == "f")
                        {
                            Console.WriteLine("Sending files");
                            do
                            {
                                Console.WriteLine("Do you want to create a mistake? (y/n):");
                                input = Console.ReadLine()?.ToLower(); 

                                if (input == "y")
                                {
                                    mistake = true;
                                    break; 
                                }
                                if (input == "n")
                                {
                                    mistake = false;
                                    break; 
                                }
                                else
                                {
                                    Console.WriteLine("Invalid input. Try again!");
                                }
                            } while (true);

                            Console.WriteLine("Enter packet size(1-1466 bytes): ");
                            do
                            {
                               input = Console.ReadLine();
                                if (ushort.TryParse(input, out packet_size) && packet_size >= 1 && packet_size <= 1465)
                                {
                                    break; 
                                }
                                Console.WriteLine("Invalid packet size. Enter again:");
                            } while (true);

                            do
                            {
                                Console.WriteLine("Enter the path to the file you want to send: ");
                                filePath = Console.ReadLine();

                                if (File.Exists(filePath))
                                {
                                    Console.WriteLine("File found. Ready to send.");
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("File not found. Please try again.");
                                }
                            } while (true);
                            //string filePath = "/Users/macbook/Desktop/Umela_inteligencia_1.pdf"; 
                            client.SendFile(destination_ip, destination_listening_port, filePath,
                                packet_size, mistake, udpClient);
                        }
                        
                    }
                }
            }

        }


        public static void receive_thread(int source_port)
        {
            while (isRunning)
            {
                server.Start(source_port);

                if (iniciator)
                {
                    ResetHeartBeatTimer();
                }
            }
        }

        private static void OnHeartBeat(object sender, ElapsedEventArgs e)
        {
            if (heartBeat_count > 3)
            {
                Console.WriteLine("Connection lost!");
                Console.WriteLine("Press ENTER to exit...");
                isRunning = false;
                sendThread.Join();
                receiveThread.Join();
                return;
            }

            headerBytes = header.ToByteArray(Header.HeaderData.MSG_NONE, Header.HeaderData.KEEP_ALIVE, 1, 0);
            client.SendServiceMessage(destination_ip, source_sending_port, destination_listening_port, headerBytes,
                udpClient);
            keep_alive_sent = true;
            heartBeat_count++;
        }

        public static void ResetHeartBeatTimer()
        {
            if (isRunning)
            {
                hearbeatTimer.Stop();
                hearbeatTimer.Start();
            }
        }

        public static void StopHeartBeatTimer()
        {
            if (hearbeatTimer != null && hearbeatTimer.Enabled)
            {
                hearbeatTimer.Stop();
            }
        }


        public static void StartHeartBeatTimer()
        {
            if (hearbeatTimer != null && !hearbeatTimer.Enabled)
            {
                heartBeat_count = 1;
                hearbeatTimer.Start();
            }
        }
    }
}