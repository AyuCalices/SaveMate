namespace SaveLoadSystem.Core.Components.Groups.Interface
{
    internal interface ILoadableGroupHandler : ILoadableGroup
    {
        string SceneName { get; }
        void OnBeforeRestoreSnapshot();
        void OnPrepareSnapshotObjects(SaveLoadManager saveLoadManager, SaveFileContext saveFileContext, LoadType loadType);
        void RestoreSnapshot(SaveLoadManager saveLoadManager, SaveFileContext saveFileContext, LoadType loadType);
        void OnAfterRestoreSnapshot();
    }
}
