using System;

namespace SaveMate.Runtime.Core.SaveStrategies.Integrity
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
    internal class CRC32IntegrityStrategy : IIntegrityStrategy
    {
        private readonly uint[] _table;

        internal CRC32IntegrityStrategy()
        {
            var polynomial = 0xedb88320;
            _table = new uint[256];

            for (uint i = 0; i < 256; i++)
            {
                var crc = i;
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
                _table[i] = crc;
            }
        }
    
        string IIntegrityStrategy.ComputeChecksum(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var crc = 0xffffffff;

            foreach (var b in data)
            {
                var index = (byte)((crc & 0xff) ^ b);
                crc = (crc >> 8) ^ _table[index];
            }

            crc ^= 0xffffffff;
            return crc.ToString("X8");
        }
    }
}
