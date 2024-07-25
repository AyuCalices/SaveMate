using SaveLoadSystem.Core.Serializable;

namespace SaveLoadSystem.Core
{
    public interface ISaveConfig
    {
        SaveVersion GetSaveVersion();
        string SavePath { get; }
        string ExtensionName { get; }
        string MetaDataExtensionName { get; }
    }
}
