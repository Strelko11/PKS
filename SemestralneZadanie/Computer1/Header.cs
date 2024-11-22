namespace Computer1;

public class Header
{
    public struct HeaderData
    {
        public byte flag;
        //public int sequence_number;
        //public ushort checksum;
        //public ushort payload_size;
        public const int header_size = 6;
        
        // Constants for message types
        public const byte MSG_NONE = 0b0000;     // Žiadne dáta (napr. Keep-alive)
        public const byte MSG_TEXT = 0b0001;     // Text (obyčajná textová správa)
        public const byte MSG_FILE = 0b0010;     // Súbor
        
        // Constants for header states
        public const byte SYN = 0b0000;          // Inicializácia spojenia
        public const byte SYN_ACK = 0b0010;      // Potvrdenie spojenia
        public const byte ACK = 0b0011;          // Potvrdenie prijatia
        public const byte DATA = 0b0100;         // Odoslané dáta
        public const byte FIN = 0b0101;          // Ukončenie spojenia
        public const byte FIN_ACK = 0b0110;      // Potvrdenie ukončenia spojenia
        public const byte KEEP_ALIVE = 0b1000;   // Keep-alive informácia
        public const byte NACK = 0b1001;         // Negatívne potvrdenie
        public const byte LAST_FRAGMENT = 0b1111; //Posledný fragment
        public const byte FILE_NAME = 0b1011;      //Pre prvy packet pri posielani suboru ktory posle nazov suboru

        

        
        public byte[] ToByteArray(byte type,byte msg, int sequenceNumber, ushort checksum)
        {
           
            byte[] headerBytes = new byte[header_size];
            flag = (byte)((type << 4) | msg);

            
            headerBytes[0] = flag;  // flag is already packed with type and msg
            
            headerBytes[1] = (byte)(sequenceNumber >> 16);  // High byte
            headerBytes[2] = (byte)(sequenceNumber >> 8); // Low byte
            headerBytes[3] = (byte)(sequenceNumber);
            
            headerBytes[4] = (byte)(checksum >> 8);  // High byte
            headerBytes[5] = (byte)(checksum & 0xFF); // Low byte
            
            

            return headerBytes;
        }

    }
}