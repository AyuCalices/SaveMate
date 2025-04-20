using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace SaveMate.Runtime.Core.SaveStrategies.Serialization
{
    internal class JsonSerializeStrategy : ISerializationStrategy
    {
        private readonly Formatting _newtonsoftJsonFormatting;
        [CanBeNull] private readonly JsonSerializerSettings _settings;

        public JsonSerializeStrategy(Formatting newtonsoftJsonFormatting, [CanBeNull] JsonSerializerSettings settings)
        {
            _newtonsoftJsonFormatting = newtonsoftJsonFormatting;
            _settings = settings;
        }
        
        public async Task<byte[]> SerializeAsync(object data)
        {
            string jsonString = JsonConvert.SerializeObject(data, _newtonsoftJsonFormatting, _settings);

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
                    var obj = JsonConvert.DeserializeObject(jsonString, type);
                    return obj;
                }
            }
        }
    }
}
