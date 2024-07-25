using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.Serializable;
using SaveLoadSystem.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core
{
    public class SaveFocus
    {
        public string FileName { get; }
        public bool HasPendingData { get; private set; }
        public bool IsPersistent { get; private set; }

        private readonly SaveLoadManager _saveLoadManager;
        
        private readonly Dictionary<string, object> _customMetaData;
        private SaveMetaData _metaData;
        private SaveData _saveData;

        public SaveFocus(SaveLoadManager saveLoadManager, string fileName)
        {
            _saveLoadManager = saveLoadManager;
            FileName = fileName;
            
            if (SaveLoadUtility.MetaDataExists(_saveLoadManager, fileName))
            {
                _metaData = SaveLoadUtility.ReadMetaData(_saveLoadManager, fileName);
                IsPersistent = true;
                _customMetaData = _metaData.CustomData;
            }
            else
            {
                _customMetaData = new();
            }
        }

        public void SetSerializableMetaData(string identifier, object data)
        {
            if (!data.GetType().IsSerializable())
            {
                Debug.LogWarning("You attempted to save data, that is not serializable!");
                return;
            }
            
            _customMetaData[identifier] = data;
        }
        
        public void SnapshotActiveScenes()
        {
            SnapshotActiveScenes(UnityUtility.GetActiveScenes());
        }

        public void SnapshotActiveScenes(params Scene[] scenesToSnapshot)
        {
            _saveData ??= new SaveData();
            HasPendingData = true;
            _saveLoadManager.SnapshotActiveScenes(_saveData, scenesToSnapshot);
        }
        
        public void WriteToDisk()
        {
            if (!HasPendingData)
            {
                Debug.LogWarning("Aborted attempt to save without pending data!");
                return;
            }

            _metaData = new SaveMetaData()
            {
                SaveVersion = _saveLoadManager.GetSaveVersion(),
                ModificationDate = DateTime.Now,
                CustomData = _customMetaData
            };
            
            _saveLoadManager.WriteToDisk(FileName, _metaData, _saveData);
            IsPersistent = true;
            HasPendingData = false;
        }

        public void DeleteFromDisk()
        {
            _saveLoadManager.DeleteFromDisk(FileName);
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
            if (!SaveLoadUtility.SaveDataExists(_saveLoadManager, FileName) 
                || !SaveLoadUtility.MetaDataExists(_saveLoadManager, FileName)) return;
            
            _saveData = SaveLoadUtility.ReadSaveDataSecure(_saveLoadManager, FileName);
            _saveLoadManager.LoadActiveScenes(_saveData, scenesToLoad);
        }
        
        public void WipeActiveSceneData()
        {
            WipeActiveSceneData(UnityUtility.GetActiveScenes());
        }
        
        public void WipeActiveSceneData(params Scene[] scenesToWipe)
        {
            if (_saveData == null) return;
            
            _saveLoadManager.WipeActiveSceneData(_saveData, scenesToWipe);
        }
    }
}
