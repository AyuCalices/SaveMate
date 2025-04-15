using System;
using System.IO;
using System.Security.Cryptography;
using SaveLoadSystem.Utility;

namespace SaveLoadSystem.Core.Integrity
{
    /// <summary>
    /// Hashing (SHA-256)
    /// 
    /// Advantages:
    /// - Security: Cryptographic hashes like SHA-256 are designed to be secure against intentional data tampering, providing strong guarantees of data integrity and authenticity.
    /// - Collision Resistance: Cryptographic hashes are designed to minimize the likelihood of two different inputs producing the same hash output (collisions).
    /// - Versatility: They are widely used in various security applications, including digital signatures, data integrity checks, and password hashing.
    ///
    /// Disadvantages:
    /// - Performance: Cryptographic hashes are computationally more intensive than Adler-32 and CRC-32, which can be a disadvantage in performance-critical applications.
    /// - Complexity: Implementing cryptographic hash functions correctly can be more complex due to the need for understanding security properties and ensuring resistance against various attack vectors.
    /// </summary>
    public class Sha256HashingIntegrityStrategy : IIntegrityStrategy
    {
        public string ComputeChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(data);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
