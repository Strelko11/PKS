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
        private static UDP_server udpServer = new UDP_server();
        private static System.Timers.Timer timer;
        private static int countDown = 10;
        private static string udpIP = "10.10.77.21"; // Change as needed
        private static int port_listen = 12345;
        private static int port_send = 12346;
        private static string message = "Hello World!";
        static void Main(string[] args)
        {
            // Start the receive thread
            Thread receiveThread = new Thread(() => receive_thread(udpIP, port_listen));
            receiveThread.Start();

            // Start the send thread
            Thread sendThread = new Thread(() => send_thread(udpIP, port_send, message));
            sendThread.Start();

            // Wait for threads to finish (in this case, they won't unless stopped)
            sendThread.Join();
            receiveThread.Join();
        }

        public static void send_thread(string udpIP, int port_send, string message)
        {
            while (true)
            {
                udpClient.SendMessage(udpIP, port_send, message);
                Thread.Sleep(1000); // Send message every second
            }
        }

        public static void receive_thread(string udpIP, int port_listen)
        {
            udpServer.Start(udpIP, port_listen);
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

