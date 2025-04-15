namespace SaveLoadSystem.Core.Components.Groups.Interface
{
    internal interface ISavableGroupHandler : ISavableGroup
    {
        string SceneName { get; }
        void OnBeforeCaptureSnapshot();
        void CaptureSnapshot(SaveLoadManager saveLoadManager, SaveFileContext saveFileContext);
        void OnAfterCaptureSnapshot();
    }
}
