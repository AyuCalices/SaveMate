using UnityEngine;

namespace SaveLoadSystem.Core.Component.SavableConverter
{
    public class SaveEnabled : MonoBehaviour, ISavable
    {
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("activeSelf", gameObject.activeSelf);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            gameObject.SetActive(loadDataHandler.GetSerializable<bool>("activeSelf"));
        }
    }
}
