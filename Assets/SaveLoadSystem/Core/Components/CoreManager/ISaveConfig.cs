namespace SaveLoadSystem.Core
{
    public interface ISaveConfig
    {
        string SavePath { get; }
        string SaveDataExtensionName { get; }
        string MetaDataExtensionName { get; }
        
    }
}
