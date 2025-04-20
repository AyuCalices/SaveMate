using System;

namespace SaveMate.Runtime.Core.SaveStrategies.Integrity
{
    /// <summary>
    /// Adler-32
    /// 
    /// Advantages:
    /// - Speed: Adler-32 is faster to compute than CRC-32 and cryptographic hashes, making it suitable for applications where performance is critical.
    /// - Simplicity: The algorithm is simpler to implement compared to CRC-32 and cryptographic hashes.
    ///
    /// Disadvantages:
    /// - Error Detection: Adler-32 is less reliable in detecting errors compared to CRC-32 and cryptographic hashes, especially for small data sets or simple error patterns.
    /// - Security: It is not suitable for cryptographic purposes as it does not provide resistance against intentional data tampering.
    /// </summary>
    internal class Adler32IntegrityStrategy : IIntegrityStrategy
    {
        private const uint ModAdler = 65521;
        
        string IIntegrityStrategy.ComputeChecksum(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            uint a = 1, b = 0;

            foreach (byte bValue in data)
            {
                a = (a + bValue) % ModAdler;
                b = (b + a) % ModAdler;
            }

            var checksum = (b << 16) | a;
            return checksum.ToString("X8");
        }
    }
}
