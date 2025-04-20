using SaveMate.Runtime.Core.SaveComponents.ManagingScope;

namespace SaveMate.Runtime.Core.SavableGroupInterfaces
{
    internal interface ISavableGroupHandler : ISavableGroup
    {
        string SceneName { get; }
        void OnBeforeCaptureSnapshot();
        void CaptureSnapshot(SaveMateManager saveMateManager, SaveFileContext saveFileContext);
        void OnAfterCaptureSnapshot();
    }
}
