using System.Threading.Tasks;

namespace SaveMate.Runtime.Core.SaveStrategies.Compression
{
    internal interface ICompressionStrategy
    {
        Task<byte[]> CompressAsync(byte[] data);
        Task<byte[]> DecompressAsync(byte[] data);
    }
}
