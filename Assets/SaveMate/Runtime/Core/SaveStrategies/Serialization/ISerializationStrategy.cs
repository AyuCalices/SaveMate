using System;
using System.Threading.Tasks;

namespace SaveMate.Runtime.Core.SaveStrategies.Serialization
{
    internal interface ISerializationStrategy
    {
        Task<byte[]> SerializeAsync(object data);
        Task<object> DeserializeAsync(byte[] data, Type type);
    }
}
