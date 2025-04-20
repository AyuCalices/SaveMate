using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SaveMate.Runtime.Core.SaveStrategies.Encryption
{
    internal class AesEncryptSerializeStrategy : IEncryptionStrategy
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        internal AesEncryptSerializeStrategy(byte[] key, byte[] iv)
        {
            _key = key;
            _iv = iv;
        }
        
        async Task<byte[]> IEncryptionStrategy.EncryptAsync(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        await cryptoStream.WriteAsync(data, 0, data.Length);
                    }

                    return memoryStream.ToArray();
                }
            }
        }

        async Task<byte[]> IEncryptionStrategy.DecryptAsync(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (MemoryStream decryptedStream = new MemoryStream())
                        {
                            await cryptoStream.CopyToAsync(decryptedStream);
                            return decryptedStream.ToArray();
                        }
                    }
                }
            }
        }
    }
}
