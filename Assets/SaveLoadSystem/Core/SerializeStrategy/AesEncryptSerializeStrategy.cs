using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public class AesEncryptSerializeStrategy : ISerializeStrategy
    {
        public ISerializeStrategy SerializeStrategy { get; set; }
        
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesEncryptSerializeStrategy(ISerializeStrategy serializeStrategy, byte[] key, byte[] iv)
        {
            SerializeStrategy = serializeStrategy;
            
            _key = key;
            _iv = iv;
        }

        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            using var ms = new MemoryStream();
            await SerializeStrategy.SerializeAsync(ms, data);
            var encryptedData = Encrypt(ms.ToArray());
            await stream.WriteAsync(encryptedData, 0, encryptedData.Length);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var decryptedData = Decrypt(ms.ToArray());
            using var decryptedStream = new MemoryStream(decryptedData);
            return await SerializeStrategy.DeserializeAsync<T>(decryptedStream);
        }

        private byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        private byte[] Decrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var resultStream = new MemoryStream();
            cs.CopyTo(resultStream);
            return resultStream.ToArray();
        }
    }
}
