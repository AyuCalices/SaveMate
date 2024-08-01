namespace SaveLoadSystem.Utility
{
    public enum SaveSceneManagerDestroyType
    {
        None,
        SnapshotScene,
        SaveScene
    }
    
    public enum SceneManagerEventType
    {
        OnBeforeSnapshot,
        OnAfterSnapshot,
        OnBeforeLoad,
        OnAfterLoad
    }
    
    public enum SaveIntegrityType
    {
        None,
        Sha256Hashing,
        CRC32,
        Adler32
    }

    public enum SaveEncryptionType
    {
        None,
        Aes
    }
    
    public enum SaveCompressionType
    {
        None,
        Gzip
    }
}
