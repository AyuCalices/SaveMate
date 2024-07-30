using SaveLoadSystem.Utility;

namespace SaveLoadSystem.Core
{
    public interface ISaveConfig
    {
        string SavePath { get; }
        string ExtensionName { get; }
        string MetaDataExtensionName { get; }
        ISerializeStrategy SerializeStrategy { get; }
    }
}
