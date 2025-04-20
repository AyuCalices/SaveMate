using SaveMate.Runtime.Core.StateSnapshot;
using UnityEngine;

namespace SaveMate.Runtime.Core.SaveComponents.GameObjectScope.StateSnapshot
{
    public class SavePosition : MonoBehaviour, ISaveStateHandler
    {
        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("position", transform.position);
        }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            if (restoreSnapshotHandler.TryLoad("position", out Vector3 position))
            {
                transform.position = position;
            }
        }
    }
}
