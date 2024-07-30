using System.IO;

//chatGPT
namespace SaveLoadSystem.Core.Integrity
{
    /*
    CRC-32

    Advantages:

    - Error Detection: CRC-32 provides better error detection capabilities than Adler-32, making it more suitable for detecting accidental changes to raw data.
    - Widely Used: It is a well-established standard used in many networking protocols and file formats.

    Disadvantages:

    - Performance: CRC-32 is slower to compute than Adler-32.
    - Security: Like Adler-32, CRC-32 is not suitable for cryptographic purposes as it does not protect against intentional data tampering.
    */
    public static class CRC32
    {
        private static readonly uint[] Table;

        static CRC32()
        {
            uint polynomial = 0xedb88320;
            Table = new uint[256];

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (uint j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                    {
                        crc = (crc >> 1) ^ polynomial;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
                Table[i] = crc;
            }
        }

        public static uint Compute(Stream stream)
        {
            uint crc = 0xffffffff;
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                byte tableIndex = (byte)(((crc) & 0xff) ^ (byte)b);
                crc = (crc >> 8) ^ Table[tableIndex];
            }
            return ~crc;
        }
    }
}
