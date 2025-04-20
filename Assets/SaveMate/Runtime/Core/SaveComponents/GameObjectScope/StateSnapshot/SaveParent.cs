using SaveMate.Core.StateSnapshot;
using UnityEngine;

namespace SaveMate.Core.SaveComponents.GameObjectScope.StateSnapshot
{
    public class SaveParent : MonoBehaviour, ISaveStateHandler
    {
        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler)
        {
            // Check if the current object has a savable parent.
            if (transform.parent == null && transform.parent.GetComponent<Savable>() == null)
            {
                Debug.LogWarning($"The {nameof(Savable)} object {name} needs a parent with a {typeof(Savable)} component to support OnCaptureState Parenting!");
                return;
            }

            createSnapshotHandler.Save("parent", transform.parent);
            createSnapshotHandler.Save("siblingIndex", transform.GetSiblingIndex());
        }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            if (!restoreSnapshotHandler.TryLoad("siblingIndex", out int siblingIndex)) return;
            
            if (restoreSnapshotHandler.TryLoad("parent", out Transform parent))
            {
                transform.SetParent(parent, false);
                
                // Set the sibling index to maintain the order in the hierarchy.
                if (siblingIndex != -1)
                {
                    transform.SetSiblingIndex(siblingIndex);
                }
            }
        }
    }
}