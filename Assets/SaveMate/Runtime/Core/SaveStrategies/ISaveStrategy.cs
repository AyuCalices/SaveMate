using SaveMate.Core.SaveStrategies.Compression;
using SaveMate.Core.SaveStrategies.Encryption;
using SaveMate.Core.SaveStrategies.Integrity;
using SaveMate.Core.SaveStrategies.Serialization;

namespace SaveMate.Core.SaveStrategies
{
    internal interface ISaveStrategy
    {
        ISerializationStrategy GetSerializationStrategy();
        ICompressionStrategy GetCompressionStrategy();
        IEncryptionStrategy GetEncryptionStrategy();
        IIntegrityStrategy GetIntegrityStrategy();
    }
}
