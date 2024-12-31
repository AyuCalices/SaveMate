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
            if (transform.parent == null && transform.parent.GetComponent<Savable>() == null)
            {
                Debug.LogWarning($"The {nameof(Savable)} object {name} needs a parent with a {typeof(Savable)} component to support Save Parenting!");
                return;
            }

            saveDataHandler.Save("parent", transform.parent);
                
            // Save the sibling index of the current transform. This helps maintain the order of the object in the hierarchy.
            saveDataHandler.Save("siblingIndex", transform.GetSiblingIndex());
        }

        // Method called during the load process to restore data and references.
        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            // Retrieve the sibling index stored during the save process.
            if (!loadDataHandler.TryLoad("siblingIndex", out int siblingIndex)) return;
            
            if (loadDataHandler.TryLoad("parent", out Transform parent))
            {
                // Cast the found object to Transform and set it as the parent of the current object.
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