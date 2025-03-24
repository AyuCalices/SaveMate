using UnityEngine;

namespace SaveLoadSystem.Core.UnityComponent.SavableConverter
{
    
    public class SaveParent : MonoBehaviour, ISavable
    {
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            // Check if the current object has a savable parent.
            if (transform.parent == null && transform.parent.GetComponent<Savable>() == null)
            {
                Debug.LogWarning($"The {nameof(Savable)} object {name} needs a parent with a {typeof(Savable)} component to support Save Parenting!");
                return;
            }

            saveDataHandler.Save("parent", transform.parent);
            saveDataHandler.Save("siblingIndex", transform.GetSiblingIndex());
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            if (!loadDataHandler.TryLoad("siblingIndex", out int siblingIndex)) return;
            
            if (loadDataHandler.TryLoad("parent", out Transform parent))
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