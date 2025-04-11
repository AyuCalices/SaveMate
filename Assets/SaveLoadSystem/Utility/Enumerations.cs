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
        BeforeSnapshot,
        AfterSnapshot,
        BeforeWriteToDisk,
        AfterWriteToDisk,
        BeforeReadFromDisk,
        AfterReadFromDisk,
        BeforeRestoreSnapshot,
        AfterRestoreSnpashot,
        BeforeDeleteSnapshotData,
        AfterDeleteSnapshotData,
        BeforeDeleteDiskData,
        AfterDeleteDiskData,
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
