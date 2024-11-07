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
        public static byte flag;
        public static byte type;
        public static byte msg;
        public ushort sequence_number;
        public ushort acknowledgment_number;
        public ushort checksum;

        // Constants for message types
        public const byte MSG_NONE = 0b0000; // Žiadne dáta (napr. Keep-alive)
        public const byte MSG_TEXT = 0b0001; // Text (obyčajná textová správa)
        public const byte MSG_FILE = 0b0010; // Súbor

        // Constants for header states
        public const byte SYN = 0b0000; // Inicializácia spojenia
        public const byte AUTH = 0b0001; // Autentifikácia
        public const byte SYN_ACK = 0b0010; // Potvrdenie spojenia
        public const byte ACK = 0b0011; // Potvrdenie prijatia
        public const byte DATA = 0b0100; // Odoslané dáta
        public const byte FIN = 0b0101; // Ukončenie spojenia
        public const byte FIN_ACK = 0b0110; // Potvrdenie ukončenia spojenia
        public const byte KEEP_ALIVE = 0b1000; // Keep-alive informácia
        public const byte NACK = 0b1001; // Negatívne potvrdenie
        public const byte LAST_FRAGMENT = 0b1111; // Posledný fragment
        public const byte TEST = 0b1011; //For testing purposes only
        static void Main(string[] args)
        {   
            type = MSG_TEXT;
            msg = ACK;
            flag = (byte)((type << 4) | msg);
            Console.Write("Nastaveny flag: ");
            Console.WriteLine(Convert.ToString(flag, 2).PadLeft(8, '0')); // Výsledok ako binárny reťazec
        



            
        }
    }
}

          


   
