namespace SaveLoadSystem.Core.Converter
{
    public abstract class BaseConverter<T> : IConverter
    {
        void IConverter.OnCaptureState(object input, CreateSnapshotHandler createSnapshotHandler)
        {
            OnCaptureState((T)input, createSnapshotHandler);
        }

        protected abstract void OnCaptureState(T input, CreateSnapshotHandler createSnapshotHandler);
        
        object IConverter.CreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return OnCreateStateObject(restoreSnapshotHandler);
        }

        protected abstract T OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler);

        void IConverter.OnRestoreState(object input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            OnRestoreState((T)input, restoreSnapshotHandler);
        }

        protected abstract void OnRestoreState(T input, RestoreSnapshotHandler restoreSnapshotHandler);
    }
}
