using System.Threading.Tasks;

namespace SaveLoadSystem.Core.SerializeStrategy.Encryption
{
    public interface IEncryptionStrategy
    {
        Task<byte[]> EncryptAsync(byte[] data);
        Task<byte[]> DecryptAsync(byte[] data);
    }
}
