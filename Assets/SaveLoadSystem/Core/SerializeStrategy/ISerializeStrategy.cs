using System;
using System.Threading.Tasks;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public interface ISerializationStrategy
    {
        Task<byte[]> SerializeAsync(object data);
        Task<object> DeserializeAsync(byte[] data, Type type);
    }

    public interface IEncryptionStrategy
    {
        Task<byte[]> EncryptAsync(byte[] data);
        Task<byte[]> DecryptAsync(byte[] data);
    }

    public interface ICompressionStrategy
    {
        Task<byte[]> CompressAsync(byte[] data);
        Task<byte[]> DecompressAsync(byte[] data);
    }
}
