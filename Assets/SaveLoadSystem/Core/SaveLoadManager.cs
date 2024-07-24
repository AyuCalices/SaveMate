using System;
using SaveLoadSystem.Core.Serializable;
using SaveLoadSystem.Plugins.UnitySingleton.Scripts;
using SaveLoadSystem.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core
{
    public class SaveLoadManager : PersistentMonoSingleton<SaveLoadManager>, ISaveConfig
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
        [SerializeField] private bool autoSaveOnApplicationPause;
        [SerializeField] private bool autoSaveOnApplicationFocus;
        [SerializeField] private bool autoSaveOnApplicationQuit;
        
        //storage type: json, binary, xml
        //max slot count
        //encription

        public string SavePath => savePath;
        public string ExtensionName => extensionName;
        public string MetaDataExtensionName => metaDataExtensionName;
        
        public bool HasCurrentSave => !string.IsNullOrEmpty(CurrentSave.Name);
        public (string Name, SaveMetaData MetaData) CurrentSave { get; private set; }
        public float Playtime => CurrentSave.MetaData.playtime;
        
        public event Action OnBeforeSlotChange;
        public event Action OnAfterSlotChange;
        
        public event Action OnBeforeSnapshot;
        public event Action OnAfterSnapshot;
        
        public event Action OnBeforeWriteToDisk;
        public event Action OnAfterWriteToDisk;
        
        public event Action OnBeforeLoad;
        public event Action OnAfterLoad;
        
        
        protected override void OnInitializing()
        {
            base.OnInitializing();
        }

        private void Update()
        {
            if (HasCurrentSave)
            {
                CurrentSave.MetaData.playtime += Time.deltaTime;
            }
        }

        public TimeSpan GetPlaytime()
        {
            return TimeSpan.FromSeconds(Playtime);
        }

        public void Load(string fileName = null)
        {
            fileName ??= defaultFileName;

            if (!SaveLoadUtility.MetaDataExists(this, fileName) ||
                !SaveLoadUtility.SaveDataExists(this, fileName)) return;
            
            var metaData = SaveLoadUtility.ReadMetaData(this, fileName);
            CurrentSave = (fileName, metaData);
            
            var saveData = SaveLoadUtility.ReadSaveDataSecure<SaveDataBufferContainer>(this, fileName, metaData.checksum);
            //TODO: implement scene support
        }

        public void Save(string fileName)
        {
            fileName ??= defaultFileName;
            
            if (CurrentSave.Name == fileName)
            {
                CurrentSave.MetaData.modificationDate = DateTime.Now;
                CurrentSave.MetaData.SaveVersion = new SaveVersion(major, minor, patch);
            }
            else
            {
                SaveMetaData saveMetaData = new SaveMetaData()
                {
                    modificationDate = DateTime.Now,
                    playtime = Playtime,
                    SaveVersion = new SaveVersion(major, minor, patch)
                };
                CurrentSave = (fileName, saveMetaData);
            }
            
            //TODO: apply actual save data
            SaveLoadUtility.WriteDataAsync(CurrentSave.MetaData, CurrentSave.MetaData, this, fileName);
        }

        public void Delete(string fileName)
        {
            fileName ??= defaultFileName;
            
            if (CurrentSave.Name == fileName)
            {
                CurrentSave = default;
            }
            
            SaveLoadUtility.DeleteAsync(this, fileName);
        }

        public void ReloadActiveScenes()
        {
            ReloadActiveScenes(UnityUtility.GetActiveScenes());
        }
        
        public void ReloadActiveScenes(params Scene[] scenesToReload)
        {
            
        }

        public void SnapshotActiveScenes()
        {
            SnapshotActiveScenes(UnityUtility.GetActiveScenes());
        }

        public void SnapshotActiveScenes(params Scene[] scenesToSnapshot)
        {
            OnBeforeSnapshot?.Invoke();
            
            OnAfterSnapshot?.Invoke();
        }

        public void WriteToDisk()
        {
            OnBeforeWriteToDisk?.Invoke();
            
            OnAfterWriteToDisk?.Invoke();
        }

        public void SaveActiveScenes()
        {
            SnapshotActiveScenes();
            WriteToDisk();
        }

        public void SaveActiveScenes(params Scene[] scenesToSave)
        {
            SnapshotActiveScenes(scenesToSave);
            WriteToDisk();
        }

        public void LoadActiveScenes()
        {
            LoadActiveScenes(UnityUtility.GetActiveScenes());
        }
        
        public void LoadActiveScenes(params Scene[] scenesToLoad)
        {
            OnBeforeLoad?.Invoke();
            
            OnAfterLoad?.Invoke();
        }

        public void WipeActiveSceneData()
        {
            WipeActiveSceneData(UnityUtility.GetActiveScenes());
        }
        
        public void WipeActiveSceneData(params Scene[] scenesToWipe)
        {
            
        }

        public void AddSaveData(object obj)
        {
            
        }

        public void AddMetaData(object obj) // -> example for add to playerprefs is track time
        {
            
        }
    }
}
