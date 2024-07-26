using System;
using System.Collections.Generic;
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
        
        //todo: use integrity check
        //storage type: json, binary, xml
        //max slot count
        //encription

        public string SavePath => savePath;
        public string ExtensionName => extensionName;
        public string MetaDataExtensionName => metaDataExtensionName;

        public SaveVersion GetSaveVersion() => new(major, minor, patch);
        public bool HasSaveFocus => SaveFocus != null;
        public SaveFocus SaveFocus { get; private set; }
        public HashSet<SaveSceneManager> TrackedSaveSceneManagers { get; } = new();
        
        public event Action<SaveFocus, SaveFocus> OnBeforeFocusChange;
        public event Action<SaveFocus, SaveFocus> OnAfterFocusChange;

        #region Simple Save

        public void SimpleSaveActiveScenes(string fileName = null)
        {
            SetFocus(fileName);

            var activeScenes = UnityUtility.GetActiveScenes();
            SaveFocus.SnapshotScenes(activeScenes);
            SaveFocus.WriteToDisk();
        }

        public void SimpleLoadActiveScenes(string fileName = null)
        {
            SetFocus(fileName);

            if (SaveFocus.IsPersistent)
            {
                var activeScenes = UnityUtility.GetActiveScenes();
                SaveFocus.LoadScenes(activeScenes);
            }
        }

        #endregion

        #region Focus Save

        public void SetFocus(string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = defaultFileName;
                Debug.LogWarning("Initialized the save system with the default file Name.");
            }
            
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

        #endregion

        #region Internal

        internal void RegisterSaveSceneManager(SaveSceneManager saveSceneManager)
        {
            TrackedSaveSceneManagers.Add(saveSceneManager);
        }
        
        internal void UnregisterSaveSceneManager(SaveSceneManager saveSceneManager)
        {
            TrackedSaveSceneManagers.Remove(saveSceneManager);
        }

        #endregion
    }
}
