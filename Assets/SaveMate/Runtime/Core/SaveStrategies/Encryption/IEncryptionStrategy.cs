using System.Threading.Tasks;

namespace SaveMate.Runtime.Core.SaveStrategies.Encryption
{
    internal interface IEncryptionStrategy
    {
        Task<byte[]> EncryptAsync(byte[] data);
        Task<byte[]> DecryptAsync(byte[] data);
    }
}
