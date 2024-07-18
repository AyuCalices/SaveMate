using UnityEngine;

namespace SaveLoadCore.Core.SavableConverter
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