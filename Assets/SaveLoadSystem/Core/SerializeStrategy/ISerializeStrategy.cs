using System.IO;
using System.Threading.Tasks;

namespace SaveLoadSystem.Core.SerializeStrategy
{
    public interface ISerializeStrategy
    {
        Task SerializeAsync<T>(Stream stream, T data);
        Task<T> DeserializeAsync<T>(Stream stream) where T : class;
    }
}
