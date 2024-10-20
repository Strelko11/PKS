﻿using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;


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
        private static int source_listening_port;
        private static int source_sending_port;
        private static string message;
        public static bool isRunning;
        private static Header.HeaderData header = new Header.HeaderData();
        public static bool SYN = false;
        public static bool SYN_ACK = false;
        public static bool ACK = false;

//TODO: Vytvorit program cisto pre server a cisto pre klienta
        static void Main(string[] args)
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

            Console.WriteLine("Press enter if both devices are ready to be connected");
            Console.ReadLine();



            isRunning = true;

            Thread sendThread = new Thread(() => send_thread(destination_ip, destination_listening_port));
            sendThread.Start();

            Thread receiveThread = new Thread(() => receive_thread(source_ip, source_listening_port));
            receiveThread.Start();

            //Console.WriteLine("Press enter to exit");
            //Console.ReadLine();

            //isRunning = false;

            sendThread.Join();
            receiveThread.Join();


        }

        public static void send_thread(string destination_ip, int destination_listening_port)
        {
            Console.WriteLine("SYN packet sent");
            while (!SYN_ACK)
            {
                header.SetType(Header.HeaderData.SYN);
                header.SetMsg(Header.HeaderData.MSG_NONE);
                udpClient.SendMessage(destination_ip, destination_listening_port, "SYN", header);
                //Console.WriteLine("SYN packet sent. Waiting for SYN_ACK...");
                Thread.Sleep(1000); // Poll every 1 second
        
                
            }

            // Second step: Send ACK after receiving SYN_ACK
            header.SetType(Header.HeaderData.ACK);
            header.SetMsg(Header.HeaderData.MSG_NONE);
            udpClient.SendMessage(destination_ip, destination_listening_port, "ACK", header);
            Console.WriteLine("ACK packet sent. Handshake complete!");

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
                udpClient.SendMessage(destination_ip, destination_listening_port, message, header);
            }
        }

        public static void receive_thread(string source_ip, int source_port)
        {
            header.SetType(Header.HeaderData.TEST);
            udpServer.Start(source_ip, source_port);

            while (!SYN_ACK)
            {
                // Check for SYN and respond with SYN_ACK
                if (!SYN_ACK && SYN)
                {
                    Console.WriteLine("SYN received, sending SYN_ACK...");
                    Header.HeaderData responseHeader = new Header.HeaderData();
                    responseHeader.SetType(Header.HeaderData.SYN_ACK);
                    responseHeader.SetMsg(Header.HeaderData.MSG_NONE); // No additional message payload
                    udpClient.SendMessage(destination_ip, destination_listening_port, "SYN_ACK", responseHeader);
                }
                else if (SYN && ACK && SYN_ACK)
                {
                    Console.WriteLine("ACK received, handshake complete. Ready for communication.");
                    // Handshake is complete, now listen for actual messages
                }
            }
        }

    }

    
}