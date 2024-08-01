using SaveLoadSystem.Core.SerializableTypes;
using UnityEngine;

namespace SaveLoadSystem.Core.Component.SavableConverter
{
    public class SaveParent : MonoBehaviour, ISavable
    {
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            if (transform.parent == null || !saveDataHandler.TryAddReferencable("parent", transform.parent))
            {
                Debug.LogWarning($"The {nameof(Savable)} object {name} needs a parent with a {typeof(Savable)} component to support Save Parenting!");
                return;
            }
            
            saveDataHandler.AddSerializable("siblingIndex", transform.GetSiblingIndex());
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            if (!loadDataHandler.TryGetReferencable("parent", out GuidPath parent)) return;
                
            var siblingIndex = loadDataHandler.GetSerializable<int>("siblingIndex");
            
            loadDataHandler.EnqueueReferenceBuilding(parent, foundObject =>
            {
                transform.parent = (Transform)foundObject;
                transform.SetSiblingIndex(siblingIndex);
            });
        }
    }
}