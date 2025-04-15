using SaveMate.Core.StateSnapshot.Interface;
using SaveMate.Core.StateSnapshot.SnapshotHandler;
using UnityEngine;

namespace SaveMate.Core.SaveComponents.GameObjectScope.StateSnapshot
{
    public class SaveVisibility : MonoBehaviour, ISaveStateHandler
    {
        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("activeSelf", gameObject.activeSelf);
        }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            if (restoreSnapshotHandler.TryLoad("activeSelf", out bool activeSelf))
            {
                gameObject.SetActive(activeSelf);
            }
        }
    }
}
