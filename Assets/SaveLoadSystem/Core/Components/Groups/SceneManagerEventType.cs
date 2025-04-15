namespace SaveLoadSystem.Core.Components.Groups
{
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
}
