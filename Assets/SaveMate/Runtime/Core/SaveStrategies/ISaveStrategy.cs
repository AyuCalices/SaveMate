using SaveMate.Runtime.Core.SaveStrategies.Compression;
using SaveMate.Runtime.Core.SaveStrategies.Encryption;
using SaveMate.Runtime.Core.SaveStrategies.Integrity;
using SaveMate.Runtime.Core.SaveStrategies.Serialization;

namespace SaveMate.Runtime.Core.SaveStrategies
{
    internal interface ISaveStrategy
    {
        ISerializationStrategy GetSerializationStrategy();
        ICompressionStrategy GetCompressionStrategy();
        IEncryptionStrategy GetEncryptionStrategy();
        IIntegrityStrategy GetIntegrityStrategy();
    }
}
