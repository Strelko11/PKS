namespace Computer1;

public class Header
{
    public struct HeaderData
    {
        public byte flag;
        //public byte type;
        //public byte msg;
        public int sequence_number;
        //public ushort acknowledgment_number;
        public ushort checksum;
        public ushort payload_size;
        public const int header_size = 6;
        
        // Constants for message types
        public const byte MSG_NONE = 0b0000;     // Žiadne dáta (napr. Keep-alive)
        public const byte MSG_TEXT = 0b0001;     // Text (obyčajná textová správa)
        public const byte MSG_FILE = 0b0010;     // Súbor
        
        // Constants for header states
        public const byte SYN = 0b0000;          // Inicializácia spojenia
        public const byte AUTH = 0b0001;         // Autentifikácia
        public const byte SYN_ACK = 0b0010;      // Potvrdenie spojenia
        public const byte ACK = 0b0011;          // Potvrdenie prijatia
        public const byte DATA = 0b0100;         // Odoslané dáta
        public const byte FIN = 0b0101;          // Ukončenie spojenia
        public const byte FIN_ACK = 0b0110;      // Potvrdenie ukončenia spojenia
        public const byte KEEP_ALIVE = 0b1000;   // Keep-alive informácia
        public const byte NACK = 0b1001;         // Negatívne potvrdenie
        public const byte LAST_FRAGMENT = 0b1111; //Posledný fragment
        public const byte FILE_NAME = 0b1011;         //Pre prvy packet pri posielani suboru ktory posle nazov suboru

        

        /*public void SetType(byte type)
        {
            this.type = type;
        }

        public void SetMsg(byte msg)
        {
            this.msg = msg;
        }

        public byte GetType()
        {
            return type;
        }*/
        
        public byte setFlag(byte type, byte msg)
        {
            flag = (byte)((type << 4) | msg);
            if (msg != KEEP_ALIVE)
            {
                Console.Write("Nastaveny flag: ");
                Console.WriteLine(Convert.ToString(flag, 2).PadLeft(8, '0')); // Výsledok ako binárny reťazec
            }
            return flag;
        }
        public byte[] ToByteArray(byte type,byte msg, int sequenceNumber, ushort checksum)
        {
            // 7-byte header
            byte[] headerBytes = new byte[header_size];
            flag = (byte)((type << 4) | msg);

            // Set flag (combining type and msg into 1 byte)
            headerBytes[0] = flag;  // flag is already packed with type and msg

            // Split sequence_number (ushort) into two bytes
            //sequence_number = 0;
            headerBytes[1] = (byte)(sequenceNumber >> 16);  // High byte
            headerBytes[2] = (byte)(sequenceNumber >> 8); // Low byte
            headerBytes[3] = (byte)(sequenceNumber);
            

            // Split acknowledgment_number (ushort) into two bytes
            //acknowledgment_number = 0;
            //headerBytes[3] = (byte)(acknowledgment_number >> 8);  // High byte
            //headerBytes[4] = (byte)(acknowledgment_number & 0xFF); // Low byte

            // Split checksum (ushort) into two bytes
            //checksum = 0;
            headerBytes[4] = (byte)(checksum >> 8);  // High byte
            headerBytes[5] = (byte)(checksum & 0xFF); // Low byte
            
            //headerBytes[6] = (byte)(payloadSize >> 8);
            //headerBytes[7] = (byte)(payloadSize & 0xFF);

            return headerBytes;
        }

    }
}