using System.Threading.Tasks;

namespace SaveMate.Runtime.Core.SaveStrategies.Encryption
{
    internal class NoneEncryptSerializeStrategy : IEncryptionStrategy
    {
        Task<byte[]> IEncryptionStrategy.EncryptAsync(byte[] data)
        {
            return Task.FromResult(data);
        }

        Task<byte[]> IEncryptionStrategy.DecryptAsync(byte[] data)
        {
            return Task.FromResult(data);
        }
    }
}
