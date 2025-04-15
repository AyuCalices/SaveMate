using System;
using System.Threading.Tasks;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public interface ISerializationStrategy
    {
        Task<byte[]> SerializeAsync(object data);
        Task<object> DeserializeAsync(byte[] data, Type type);
    }
}
