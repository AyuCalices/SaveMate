using System;
using SaveLoadSystem.Core.Component;
using SaveLoadSystem.Core.Serializable;
using SaveLoadSystem.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class SaveLoadManager : ScriptableObject, ISaveConfig
    {
        [Header("Version")] 
        [SerializeField] private int major;
        [SerializeField] private int minor;
        [SerializeField] private int patch;
        
        [Header("File Settings")]
        [SerializeField] private string defaultFileName;
        [SerializeField] private string savePath;
        [SerializeField] private string extensionName;
        [SerializeField] private string metaDataExtensionName;

        [Header("Slot Settings")] 
        [SerializeField] private bool autoSaveOnSaveFocusSwap;
        [SerializeField] private bool autoSaveOnApplicationPause;
        [SerializeField] private bool autoSaveOnApplicationFocus;
        [SerializeField] private bool autoSaveOnApplicationQuit;
        
        //storage type: json, binary, xml
        //max slot count
        //encription

        public string SavePath => savePath;
        public string ExtensionName => extensionName;
        public string MetaDataExtensionName => metaDataExtensionName;

        public SaveVersion GetSaveVersion() => new(major, minor, patch);
        public bool HasSaveFocus => SaveFocus != null;
        public SaveFocus SaveFocus { get; private set; }
        
        public event Action<SaveFocus, SaveFocus> OnBeforeFocusChange;
        public event Action<SaveFocus, SaveFocus> OnAfterFocusChange;
        
        public event Action OnBeforeSnapshot;
        public event Action OnAfterSnapshot;
        
        public event Action OnBeforeDeleteFromDisk;
        public event Action OnAfterDeleteFromDisk;
        
        public event Action OnBeforeWriteToDisk;
        public event Action OnAfterWriteToDisk;
        
        public event Action OnBeforeLoad;
        public event Action OnAfterLoad;

        #region Simple Save

        public void SaveActiveScenes(string fileName)
        {
            var saveData = new SaveData();
            SnapshotActiveScenes(saveData, UnityUtility.GetActiveScenes());

            var saveMetaData = new SaveMetaData
            {
                SaveVersion = GetSaveVersion(),
                ModificationDate = DateTime.Now
            };
            
            WriteToDisk(fileName, saveMetaData, saveData);
        }

        public void LoadActiveScenes(string fileName)
        {
            var saveData = SaveLoadUtility.ReadSaveDataSecure(this, fileName);

            LoadActiveScenes(saveData, UnityUtility.GetActiveScenes());
        }

        #endregion

        #region Focus Save

        public void SetFocus(string fileName)
        {
            if (SaveFocus != null)
            {
                //same to current save
                if (SaveFocus.FileName == fileName) return;
                
                //other save, but still has pending data that can be saved
                if (SaveFocus.HasPendingData && autoSaveOnSaveFocusSwap)
                {
                    SaveFocus.WriteToDisk();
                }
            }
            
            SwapFocus(new SaveFocus(this, fileName));
        }

        public void ReleaseFocus()
        {
            //save pending data if allowed
            if (SaveFocus.HasPendingData && autoSaveOnSaveFocusSwap)
            {
                SaveFocus.WriteToDisk();
            }
            
            SwapFocus(null);
        }

        private void SwapFocus(SaveFocus newSaveFocus)
        {
            SaveFocus oldSaveFocus = SaveFocus;
            
            OnBeforeFocusChange?.Invoke(oldSaveFocus, newSaveFocus);

            SaveFocus = newSaveFocus;
            
            OnAfterFocusChange?.Invoke(oldSaveFocus, newSaveFocus);
        }

        #endregion
        
        #region Utility Methods
        
        public void ReloadActiveScenes()
        {
            ReloadActiveScenes(UnityUtility.GetActiveScenes());
        }
        
        public void ReloadActiveScenes(params Scene[] scenesToReload)
        {
            foreach (var scene in scenesToReload)
            {
                if (!scene.isLoaded) continue;

                SceneManager.LoadSceneAsync(scene.path);
            }
        }
        
        public void SnapshotActiveScenes(SaveData saveData, params Scene[] scenesToSnapshot)
        {
            OnBeforeSnapshot?.Invoke();

            foreach (var scene in scenesToSnapshot)
            {
                var saveSceneManager = UnityUtility.FindObjectOfTypeInScene<SaveSceneManager>(scene, false);

                if (saveData.ContainsSceneData(scene))
                {
                    saveData.SetSceneData(scene, saveSceneManager.CreateSnapshot());
                }
            }
            
            OnAfterSnapshot?.Invoke();
        }

        public void WriteToDisk(string fileName, SaveMetaData metaData, SaveData saveData)
        {
            //TODO: what if currently something is doing this?
            OnBeforeWriteToDisk?.Invoke();
            
            SaveLoadUtility.WriteDataAsync(metaData, saveData, this, fileName, () => OnAfterWriteToDisk?.Invoke());
        }
        
        public void DeleteFromDisk(string fileName)
        {
            //TODO: what if currently something is doing this?
            
            OnBeforeWriteToDisk?.Invoke();
            
            SaveLoadUtility.DeleteAsync(this, fileName, () => OnAfterWriteToDisk?.Invoke());
        }
        
        public void LoadActiveScenes(SaveData saveData, params Scene[] scenesToLoad)
        {
            OnBeforeLoad?.Invoke();
            
            foreach (var scene in scenesToLoad)
            {
                if (!saveData.TryGetSceneData(scene, out SceneDataContainer sceneDataContainer)) continue;
                
                var saveSceneManager = UnityUtility.FindObjectOfTypeInScene<SaveSceneManager>(scene, true);
                saveSceneManager.LoadSnapshot(sceneDataContainer);
            }
            
            OnAfterLoad?.Invoke();
        }
        
        public void WipeActiveSceneData(SaveData saveData, params Scene[] scenesToWipe)
        {
            foreach (var scene in scenesToWipe)
            {
                saveData.RemoveSceneData(scene);
            }
        }

        #endregion
    }
}
