using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public class JsonSerializeStrategy : ISerializationStrategy
    {
        public async Task<byte[]> SerializeAsync(object data)
        {
            string jsonString = JsonConvert.SerializeObject(data);

            // Using a memory stream to write bytes asynchronously
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
                await memoryStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
                return memoryStream.ToArray();
            }
        }

        public async Task<object> DeserializeAsync(byte[] data, Type type)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                // Reading data asynchronously
                using (StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8))
                {
                    string jsonString = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject(jsonString, type);
                }
            }
        }
    }
}
