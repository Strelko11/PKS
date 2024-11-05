﻿﻿using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;



namespace Computer1
{
    class Program
    {
        private static Client udpClient = new Client();
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
        public static bool SYN = false;
        public static bool SYN_ACK = false;
        public static bool handshake_ACK = false;
        public static bool iniciator = false;
        public static bool handshake_complete = false;
        public static bool message_received = false;
        public static bool message_ACK = true;
        public static bool message_ACK_sent = false;
        public static bool message_sent = false;

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
                while (!SYN_ACK)
                {
                    header.SetType(Header.HeaderData.SYN);
                    header.SetMsg(Header.HeaderData.MSG_NONE);
                    udpClient.SendMessage(destination_ip, source_sending_port, destination_listening_port, "SYN", header);
                    //Console.WriteLine("SYN packet sent. Waiting for SYN_ACK...");
                }
                header.SetType(Header.HeaderData.ACK);
                header.SetMsg(Header.HeaderData.MSG_NONE);
                udpClient.SendMessage(destination_ip, source_sending_port, destination_listening_port, "ACK", header);
                Console.WriteLine("Handshake complete!");
                handshake_complete = true;
                Console.WriteLine("**************** HANDSHAKE COMPLETE *************\n\n");
            }

        }


        public static void send_thread(string destination_ip, int destination_listening_port)
        {

            // header.SetType(Header.HeaderData.TEST);
            // header.SetMsg(Header.HeaderData.MSG_NONE);
            // udpClient.SendMessage(destination_ip, source_sending_port, destination_listening_port, message, header);

            while (isRunning)
            {
                if (handshake_complete && message_ACK)
                {
                    Console.WriteLine("Enter message you want to send (type 'exit' to quit):");
                    message = Console.ReadLine();
                    if (message == "exit")
                    {
                        header.SetType(Header.HeaderData.TEST);
                        header.SetMsg(Header.HeaderData.MSG_NONE);
                        udpClient.SendMessage(source_ip, source_sending_port, source_listening_port, message, header);
                        isRunning = false;
                        continue;
                    }

                    header.SetType(Header.HeaderData.TEST);
                    header.SetMsg(Header.HeaderData.MSG_NONE);
                    message_ACK = false;
                    message_sent = true;
                    udpClient.SendMessage(destination_ip, source_sending_port, destination_listening_port, message, header);
                    Console.WriteLine("Waiting for ACK");
                    
                }
            }
            //Console.WriteLine("Exited program");
        }



        public static void receive_thread(string source_ip, int source_port)
        {
            //while (isRunning)
            //{
                header.SetType(Header.HeaderData.TEST);
                udpServer.Start(source_ip, source_port);
            //}
            //Console.WriteLine("Exited receive thread");

        }

    }


}