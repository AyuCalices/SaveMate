namespace SaveMate.Runtime.Core.StateSnapshot
{
    public interface ISaveStateHandler
    {
        void OnCaptureState(CreateSnapshotHandler createSnapshotHandler);
        void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler);
    }
}
