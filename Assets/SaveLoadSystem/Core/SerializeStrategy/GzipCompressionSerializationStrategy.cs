using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public class GzipCompressionSerializationStrategy : ICompressionStrategy
    {
        public async Task<byte[]> CompressAsync(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    await gzipStream.WriteAsync(data, 0, data.Length);
                }

                return memoryStream.ToArray();
            }
        }

        public async Task<byte[]> DecompressAsync(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    using (MemoryStream decompressedStream = new MemoryStream())
                    {
                        await gzipStream.CopyToAsync(decompressedStream);
                        return decompressedStream.ToArray();
                    }
                }
            }
        }
    }
    
    public class NoneCompressionSerializationStrategy : ICompressionStrategy
    {
        public Task<byte[]> CompressAsync(byte[] data)
        {
            return Task.FromResult(data);
        }

        public Task<byte[]> DecompressAsync(byte[] data)
        {
            return Task.FromResult(data);
        }
    }
}
