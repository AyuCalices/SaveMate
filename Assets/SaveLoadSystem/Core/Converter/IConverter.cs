namespace SaveLoadSystem.Core.Converter
{
    internal interface IConverter
    {
        void OnCaptureState(object input, CreateSnapshotHandler createSnapshotHandler);
        object CreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler);
        void OnRestoreState(object input, RestoreSnapshotHandler restoreSnapshotHandler);
    }
}
