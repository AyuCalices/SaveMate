using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public class GzipCompressionSerializationStrategy : ISerializeStrategy
    {
        public ISerializeStrategy SerializeStrategy { get; set; }

        public GzipCompressionSerializationStrategy(ISerializeStrategy serializeStrategy)
        {
            SerializeStrategy = serializeStrategy;
        }

        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            using var memoryStream = new MemoryStream();
            await SerializeStrategy.SerializeAsync(memoryStream, data);
            await using var gzipStream = new GZipStream(stream, CompressionMode.Compress);
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(gzipStream);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            using var decompressedStream = new MemoryStream();
            await stream.CopyToAsync(decompressedStream);
            decompressedStream.Position = 0;
            await using var gzipStream = new GZipStream(decompressedStream, CompressionMode.Decompress);
            var resultStream = new MemoryStream();
            await gzipStream.CopyToAsync(resultStream);
            resultStream.Position = 0;
            return await SerializeStrategy.DeserializeAsync<T>(resultStream);
        }
    }
}
