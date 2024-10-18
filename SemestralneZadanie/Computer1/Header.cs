namespace Computer1;

public class Header
{
    public struct HeaderData
    {   
        public byte type;
        public byte msg;
        
        // Constants for header types
        public const byte SYN = 0x00;          // Inicializácia spojenia
        public const byte AUTH = 0x01;         // Autentifikácia
        public const byte SYN_ACK = 0x02;      // Potvrdenie spojenia
        public const byte ACK = 0x03;          // Potvrdenie prijatia
        public const byte DATA = 0x04;         // Odoslané dáta
        public const byte FIN = 0x05;          // Ukončenie spojenia
        public const byte FIN_ACK = 0x06;      // Potvrdenie ukončenia spojenia
        public const byte KEEP_ALIVE = 0x08;   // Keep-alive informácia
        public const byte NACK = 0x09;         // Negatívne potvrdenie
        public const byte LAST_FRAGMENT = 0x0F; // Posledný fragment

        // Constants for message states
        public const byte MSG_NONE = 0x00;     // Žiadne dáta (napr. Keep-alive)
        public const byte MSG_TEXT = 0x01;     // Text (obyčajná tečxtová správa)
        public const byte MSG_FILE = 0x02;     // Súbor

        public void SetType(byte type)
        {
            this.type = type;
        }

        public void SetMsg(byte msg)
        {
            this.msg = msg;
        }
    }
}