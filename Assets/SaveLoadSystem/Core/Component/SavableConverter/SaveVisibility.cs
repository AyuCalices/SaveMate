using UnityEngine;

namespace SaveLoadSystem.Core.Component.SavableConverter
{
    public class SaveVisibility : MonoBehaviour, ISavable
    {
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("activeSelf", gameObject.activeSelf);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            if (loadDataHandler.TryLoad("activeSelf", out bool activeSelf))
            {
                gameObject.SetActive(activeSelf);
            }
        }
    }
}
