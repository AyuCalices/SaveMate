namespace SaveMate.Core.SaveComponents.SceneScope
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
