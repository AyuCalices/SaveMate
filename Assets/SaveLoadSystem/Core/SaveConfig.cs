using SaveLoadSystem.Core.Integrity;
using SaveLoadSystem.Core.SerializeStrategy;

namespace SaveLoadSystem.Core
{
    public interface ISaveConfig
    {
        string SavePath { get; }
        string ExtensionName { get; }
        string MetaDataExtensionName { get; }
        
    }

    public interface ISaveStrategy
    {
        ISerializationStrategy GetSerializationStrategy();
        ICompressionStrategy GetCompressionStrategy();
        IEncryptionStrategy GetEncryptionStrategy();
        IIntegrityStrategy GetIntegrityStrategy();
    }
}
