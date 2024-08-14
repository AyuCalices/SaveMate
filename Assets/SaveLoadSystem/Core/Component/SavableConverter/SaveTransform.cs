using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

namespace SaveLoadSystem.Core.Component.SavableConverter
{
    public class SaveTransform : MonoBehaviour, ISavable
    {
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.TrySaveAsReferencable("position", transform.position);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            if (loadDataHandler.TryLoadReferencable("position", out GuidPath path))
            {
                loadDataHandler.EnqueueReferenceBuilding(path, foundObject =>
                {
                    transform.position = (Vector3)foundObject;
                });
            }
        }
    }
}
