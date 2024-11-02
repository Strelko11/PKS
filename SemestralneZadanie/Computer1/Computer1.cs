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
        public static int source_listening_port;
        public static int source_sending_port;
        private static string message;
        public static bool isRunning;
        private static Header.HeaderData header = new Header.HeaderData();
        public static bool SYN = false;
        public static bool SYN_ACK = false;
        public static bool ACK = false;
        public static bool iniciator = false;
        public static bool handshake_complete = false; 
        
        static void Main(string[] args)
        {
            //Console.WriteLine("Enter source IP address:");
           //source_ip = Console.ReadLine();
            
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

            isRunning = true;

            Thread receiveThread = new Thread(() => receive_thread(source_ip, source_listening_port));
            receiveThread.Start();
            Thread sendThread = new Thread(() => send_thread(destination_ip, destination_listening_port));
            sendThread.Start();


            Console.WriteLine("Do you want to initiate the handshake? (y/n)");
            input = Console.ReadLine();
            if(input == "y"){
                iniciator = true;
            }
        
            

            Console.WriteLine("Press enter on handshake initiator side if both devices are ready to be connected");
            Console.ReadLine();

            handshake(destination_ip,destination_listening_port,iniciator);
            
            


            //Console.WriteLine("Press enter to exit");
            //Console.ReadLine();
            //isRunning = false;

            sendThread.Join();
            receiveThread.Join();

        }

        public static void handshake(string destination_ip, int destination_listening_port, bool iniciator){
            if(iniciator){
                Console.WriteLine("SYN packet sent");
                while (!SYN_ACK){
                    header.SetType(Header.HeaderData.SYN);
                    header.SetMsg(Header.HeaderData.MSG_NONE);
                    udpClient.SendMessage(destination_ip,source_sending_port, destination_listening_port, "SYN", header);
                    //Console.WriteLine("SYN packet sent. Waiting for SYN_ACK...");
                    Thread.Sleep(2000);         
                }
                header.SetType(Header.HeaderData.ACK);
                header.SetMsg(Header.HeaderData.MSG_NONE);
                udpClient.SendMessage(destination_ip, source_sending_port,destination_listening_port, "ACK", header);
                Console.WriteLine("ACK packet sent. Handshake complete!");
                handshake_complete = true;

                
            }
            
        }

        public static void send_thread(string destination_ip, int destination_listening_port)
        {   
            while (isRunning)
            {   
                if(handshake_complete){
                    header.SetType(Header.HeaderData.TEST);
                    header.SetMsg(Header.HeaderData.MSG_NONE);
                    Console.WriteLine("Enter message you want to send (type 'exit' to quit):");
                    message = Console.ReadLine();
                    if (message == "exit")
                    {
                        isRunning = false;
                        continue;
                    }

                    udpClient.SendMessage(destination_ip, source_sending_port,destination_listening_port, message, header);
                }
                
            }
            
        }

        public static void receive_thread(string source_ip, int source_port)
        {
            header.SetType(Header.HeaderData.TEST);
            udpServer.Start(source_ip, source_port);

           
        }

    }

    
}