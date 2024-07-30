using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public class BinarySerializeStrategy : ISerializeStrategy
    {
        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            var formatter = new BinaryFormatter();
            await Task.Run(() => formatter.Serialize(stream, data));
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            var formatter = new BinaryFormatter();
            return await Task.Run(() => formatter.Deserialize(stream) as T);
        }
    }
}
