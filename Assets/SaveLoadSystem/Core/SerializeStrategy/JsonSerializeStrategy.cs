using System.IO;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public class JsonSerializeStrategy : ISerializeStrategy
    {
        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            await using var writer = new StreamWriter(stream);
            var json = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(json);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
