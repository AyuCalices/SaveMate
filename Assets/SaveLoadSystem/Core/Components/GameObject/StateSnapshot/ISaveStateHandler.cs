namespace SaveLoadSystem.Core.UnityComponent.SavableConverter
{
    public interface ISaveStateHandler
    {
        void OnCaptureState(CreateSnapshotHandler createSnapshotHandler);
        void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler);
    }
}
