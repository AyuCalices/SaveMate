using System.IO;

namespace SaveLoadSystem.Core.Integrity
{
    public interface IIntegrityStrategy
    {
        string ComputeChecksum(Stream stream);
    }
}
