using System;
using System.IO;

namespace SaveLoadSystem.Core.Integrity
{
    /// <summary>
    /// CRC-32
    /// 
    /// Advantages:
    /// - Error Detection: CRC-32 provides better error detection capabilities than Adler-32, making it more suitable for detecting accidental changes to raw data.
    /// - Widely Used: It is a well-established standard used in many networking protocols and file formats.
    ///
    /// Disadvantages:
    /// - Performance: CRC-32 is slower to compute than Adler-32.
    /// - Security: Like Adler-32, CRC-32 is not suitable for cryptographic purposes as it does not protect against intentional data tampering.
    /// </summary>
    public class CRC32IntegrityStrategy : IIntegrityStrategy
    {
        private readonly uint[] Table;

        public CRC32IntegrityStrategy()
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
        
        public string ComputeChecksum(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            uint crc = 0xffffffff;

            int bufferLength = 1024;
            byte[] buffer = new byte[bufferLength];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    byte index = (byte)(((crc) & 0xff) ^ buffer[i]);
                    crc = (uint)((crc >> 8) ^ Table[index]);
                }
            }

            crc ^= 0xffffffff;
            return crc.ToString("X8");
        }
    }
}
