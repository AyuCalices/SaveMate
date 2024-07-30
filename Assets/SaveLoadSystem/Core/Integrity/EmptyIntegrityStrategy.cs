using System.IO;

namespace SaveLoadSystem.Core.Integrity
{
    public class EmptyIntegrityStrategy : IIntegrityStrategy
    {
        public string ComputeChecksum(Stream stream)
        {
            return string.Empty;
        }
    }
}
