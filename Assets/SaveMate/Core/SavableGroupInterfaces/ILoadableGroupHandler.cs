using SaveMate.Core.SaveComponents.ManagingScope;

namespace SaveMate.Core.SavableGroupInterfaces
{
    internal interface ILoadableGroupHandler : ILoadableGroup
    {
        string SceneName { get; }
        void OnBeforeRestoreSnapshot();
        void OnPrepareSnapshotObjects(SaveMateManager saveMateManager, SaveFileContext saveFileContext, LoadType loadType);
        void RestoreSnapshot(SaveMateManager saveMateManager, SaveFileContext saveFileContext, LoadType loadType);
        void OnAfterRestoreSnapshot();
    }
}
