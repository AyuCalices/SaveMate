using System.Threading.Tasks;

namespace SaveMate.Core.SaveStrategies.Compression
{
    internal interface ICompressionStrategy
    {
        Task<byte[]> CompressAsync(byte[] data);
        Task<byte[]> DecompressAsync(byte[] data);
    }
}
