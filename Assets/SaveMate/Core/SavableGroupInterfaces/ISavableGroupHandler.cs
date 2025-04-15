using SaveMate.Core.SaveComponents.ManagingScope;

namespace SaveMate.Core.SavableGroupInterfaces
{
    internal interface ISavableGroupHandler : ISavableGroup
    {
        string SceneName { get; }
        void OnBeforeCaptureSnapshot();
        void CaptureSnapshot(SaveMateManager saveMateManager, SaveFileContext saveFileContext);
        void OnAfterCaptureSnapshot();
    }
}
