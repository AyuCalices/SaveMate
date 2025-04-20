using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SaveMate.Runtime.Core.SaveStrategies.Compression
{
    internal class GzipCompressionSerializationStrategy : ICompressionStrategy
    {
        async Task<byte[]> ICompressionStrategy.CompressAsync(byte[] data)
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

        async Task<byte[]> ICompressionStrategy.DecompressAsync(byte[] data)
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
}
