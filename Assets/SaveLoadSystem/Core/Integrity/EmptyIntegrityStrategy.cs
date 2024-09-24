using System.IO;

namespace SaveLoadSystem.Core.Integrity
{
    public class EmptyIntegrityStrategy : IIntegrityStrategy
    {
        public string ComputeChecksum(byte[] data)
        {
            return string.Empty;
        }
    }
}
