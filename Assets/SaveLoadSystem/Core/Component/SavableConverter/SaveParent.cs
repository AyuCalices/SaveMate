using UnityEngine;

namespace SaveLoadSystem.Core.Component.SavableConverter
{
    public class SaveParent : MonoBehaviour, ISavable
    {
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddReferencable("parent", transform.parent);
            saveDataHandler.AddSerializable("siblingIndex", transform.GetSiblingIndex());
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            Debug.Log(name);
            var parent = loadDataHandler.GetSaveElement("parent");
            var siblingIndex = loadDataHandler.GetSaveElement<int>("siblingIndex");
            
            loadDataHandler.EnqueueReferenceBuilding(parent, foundObject =>
            {
                transform.parent = (Transform)foundObject;
                transform.SetSiblingIndex(siblingIndex);
            });
        }
    }
}