using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

namespace SaveLoadSystem.Core.Component.SavableConverter
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
