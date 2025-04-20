using System.Threading.Tasks;

namespace SaveMate.Runtime.Core.SaveStrategies.Compression
{
    internal class NoneCompressionSerializationStrategy : ICompressionStrategy
    {
        Task<byte[]> ICompressionStrategy.CompressAsync(byte[] data)
        {
            return Task.FromResult(data);
        }

        Task<byte[]> ICompressionStrategy.DecompressAsync(byte[] data)
        {
            return Task.FromResult(data);
        }
    }
}
