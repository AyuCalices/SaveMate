using System.Threading.Tasks;

namespace SaveMate.Core.SaveStrategies.Encryption
{
    internal interface IEncryptionStrategy
    {
        Task<byte[]> EncryptAsync(byte[] data);
        Task<byte[]> DecryptAsync(byte[] data);
    }
}
