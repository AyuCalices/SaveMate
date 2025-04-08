namespace SaveLoadSystem.Utility
{
    public enum SaveSceneManagerDestroyType
    {
        None,
        SnapshotSingleScene,
        SnapshotActiveScenes,
        SaveSingleScene,
        SaveActiveScenes,
    }
    
    public enum SceneManagerEventType
    {
        OnBeforeSnapshot,
        OnAfterSnapshot,
        OnBeforeLoad,
        OnAfterLoad,
        OnBeforeDeleteDiskData,
        OnAfterDeleteDiskData,
        OnBeforeWriteToDisk,
        OnAfterWriteToDisk
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
