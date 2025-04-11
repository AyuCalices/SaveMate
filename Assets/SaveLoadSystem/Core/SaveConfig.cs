using SaveLoadSystem.Core.Integrity;
using SaveLoadSystem.Core.SerializeStrategy;

namespace SaveLoadSystem.Core
{
    public interface ISaveConfig
    {
        string SavePath { get; }
        string SaveDataExtensionName { get; }
        string MetaDataExtensionName { get; }
        
    }

    internal interface ISaveStrategy
    {
        ISerializationStrategy GetSerializationStrategy();
        ICompressionStrategy GetCompressionStrategy();
        IEncryptionStrategy GetEncryptionStrategy();
        IIntegrityStrategy GetIntegrityStrategy();
    }
}
