using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

namespace SaveLoadSystem.Core.Component.SavableConverter
{
    public class SaveTransform : MonoBehaviour, ISavable
    {
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.TryAddReferencable("position", transform.position);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            if (loadDataHandler.TryGetReferencable("position", out GuidPath path))
            {
                loadDataHandler.EnqueueReferenceBuilding(path, foundObject =>
                {
                    transform.position = (Vector3)foundObject;
                });
            }
        }
    }
}
