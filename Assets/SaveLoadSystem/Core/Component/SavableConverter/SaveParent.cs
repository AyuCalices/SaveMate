using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

namespace SaveLoadSystem.Core.Component.SavableConverter
{
    
    public class SaveParent : MonoBehaviour, ISavable
    {
        // Method called during the save process to store relevant data.
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            // Check if the current object has a parent and if the parent was successfully added to the saveDataHandler as a referencable object.
            if (transform.parent == null || !saveDataHandler.TrySaveAsReferencable("parent", transform.parent))
            {
                Debug.LogWarning($"The {nameof(Savable)} object {name} needs a parent with a {typeof(Savable)} component to support Save Parenting!");
                return;
            }
            
            // Save the sibling index of the current transform. This helps maintain the order of the object in the hierarchy.
            saveDataHandler.SaveAsValue("siblingIndex", transform.GetSiblingIndex());
        }

        // Method called during the load process to restore data and references.
        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            // Attempt to retrieve the parent reference from the loadDataHandler.
            if (!loadDataHandler.TryLoadReferencable("parent", out GuidPath parent)) return;
            
            // Retrieve the sibling index stored during the save process.
            var siblingIndex = loadDataHandler.LoadValue<int>("siblingIndex");
            
            // Enqueue a reference-building action to set the parent and sibling index once the parent reference is resolved.
            loadDataHandler.EnqueueReferenceBuilding(parent, foundObject =>
            {
                // Cast the found object to Transform and set it as the parent of the current object.
                transform.parent = (Transform)foundObject;
                
                // Set the sibling index to maintain the order in the hierarchy.
                transform.SetSiblingIndex(siblingIndex);
            });
        }
    }
    
}