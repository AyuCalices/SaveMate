using UnityEngine;

namespace SaveLoadSystem.Core.UnityComponent.SavableConverter
{
    
    public class SavePosition : MonoBehaviour, ISavable
    {
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("position", transform.position);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            if (loadDataHandler.TryLoad("position", out Vector3 position))
            {
                transform.position = position;
            }
        }
    }
}
