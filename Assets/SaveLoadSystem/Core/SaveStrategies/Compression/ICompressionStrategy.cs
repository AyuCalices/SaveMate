using System.Threading.Tasks;

namespace SaveLoadSystem.Core.SerializeStrategy.Compression
{
    public interface ICompressionStrategy
    {
        Task<byte[]> CompressAsync(byte[] data);
        Task<byte[]> DecompressAsync(byte[] data);
    }
}
