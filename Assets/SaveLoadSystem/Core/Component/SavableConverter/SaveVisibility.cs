using UnityEngine;

namespace SaveLoadSystem.Core.Component.SavableConverter
{
    public class SaveVisibility : MonoBehaviour, ISavable
    {
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("activeSelf", gameObject.activeSelf);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            gameObject.SetActive(loadDataHandler.LoadValue<bool>("activeSelf"));
        }
    }
}
