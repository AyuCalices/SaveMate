using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SaveLoadSystem.Core.SerializeStrategy.Encryption;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public class AesEncryptSerializeStrategy : IEncryptionStrategy
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesEncryptSerializeStrategy(byte[] key, byte[] iv)
        {
            _key = key;
            _iv = iv;
        }
        
        public async Task<byte[]> EncryptAsync(byte[] data)
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

        public async Task<byte[]> DecryptAsync(byte[] data)
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
    
    public class NoneEncryptSerializeStrategy : IEncryptionStrategy
    {
        public Task<byte[]> EncryptAsync(byte[] data)
        {
            return Task.FromResult(data);
        }

        public Task<byte[]> DecryptAsync(byte[] data)
        {
            return Task.FromResult(data);
        }
    }
}
