using SaveMate.Core.StateSnapshot.Interface;
using SaveMate.Core.StateSnapshot.SnapshotHandler;
using UnityEngine;

namespace SaveMate.Core.SaveComponents.GameObjectScope.StateSnapshot
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
