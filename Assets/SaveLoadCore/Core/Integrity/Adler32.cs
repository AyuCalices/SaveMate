using System;
using System.IO;
using System.Security.Cryptography;

namespace SaveLoadCore.Core.Integrity
{
    /*
    Adler-32

    Advantages:

    - Speed: Adler-32 is faster to compute than CRC-32 and cryptographic hashes, making it suitable for applications where performance is critical.
    - Simplicity: The algorithm is simpler to implement compared to CRC-32 and cryptographic hashes.

    Disadvantages:

    - Error Detection: Adler-32 is less reliable in detecting errors compared to CRC-32 and cryptographic hashes, especially for small data sets or simple error patterns.
    - Security: It is not suitable for cryptographic purposes as it does not provide resistance against intentional data tampering.
    */
    public static class Adler32
    {
        private const uint ModAdler = 65521;

        public static uint ComputeChecksum(byte[] data)
        {
            uint a = 1, b = 0;
            foreach (var t in data)
            {
                a = (a + t) % ModAdler;
                b = (b + a) % ModAdler;
            }
            return (b << 16) | a;
        }
        
        public static uint ComputeChecksum(Stream stream)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                Span<byte> buffer = default;
                _ = stream.Read(buffer);

                return ComputeChecksum(buffer.ToArray());
            }
        }
    }
}
