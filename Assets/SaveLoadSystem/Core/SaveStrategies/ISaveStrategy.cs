using SaveLoadSystem.Core.Integrity;
using SaveLoadSystem.Core.SerializeStrategy;
using SaveLoadSystem.Core.SerializeStrategy.Compression;
using SaveLoadSystem.Core.SerializeStrategy.Encryption;

namespace SaveLoadSystem.Core.SaveStrategies
{
    internal interface ISaveStrategy
    {
        ISerializationStrategy GetSerializationStrategy();
        ICompressionStrategy GetCompressionStrategy();
        IEncryptionStrategy GetEncryptionStrategy();
        IIntegrityStrategy GetIntegrityStrategy();
    }
}
