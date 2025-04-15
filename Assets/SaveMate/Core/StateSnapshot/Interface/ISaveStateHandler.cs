using SaveMate.Core.StateSnapshot.SnapshotHandler;

namespace SaveMate.Core.StateSnapshot.Interface
{
    public interface ISaveStateHandler
    {
        void OnCaptureState(CreateSnapshotHandler createSnapshotHandler);
        void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler);
    }
}
