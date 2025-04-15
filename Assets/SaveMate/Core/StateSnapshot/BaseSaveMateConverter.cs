using SaveMate.Core.StateSnapshot.Converter;

namespace SaveMate.Core.StateSnapshot
{
    public abstract class BaseSaveMateConverter<T> : ISaveMateConverter
    {
        void ISaveMateConverter.OnCaptureState(object input, CreateSnapshotHandler createSnapshotHandler)
        {
            OnCaptureState((T)input, createSnapshotHandler);
        }

        protected abstract void OnCaptureState(T input, CreateSnapshotHandler createSnapshotHandler);
        
        object ISaveMateConverter.CreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return OnCreateStateObject(restoreSnapshotHandler);
        }

        protected abstract T OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler);

        void ISaveMateConverter.OnRestoreState(object input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            OnRestoreState((T)input, restoreSnapshotHandler);
        }

        protected abstract void OnRestoreState(T input, RestoreSnapshotHandler restoreSnapshotHandler);
    }
}
