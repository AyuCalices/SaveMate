using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Integrity;
using SaveLoadSystem.Core.Serializable;
using UnityEngine;

namespace SaveLoadSystem.Utility
{
    public static class SaveLoadUtility
    {
        #region Public

        public static string[] FindAllSaveFiles(ISaveConfig saveConfig)
        {
            string directoryPath = DirectoryPath(saveConfig);
            
            if (Directory.Exists(directoryPath))
            {
                return Directory.GetFiles(directoryPath, $"*.{saveConfig.MetaDataExtensionName}");
            }

            return Array.Empty<string>();
        }
        
        public static bool SaveDataExists(ISaveConfig saveConfig, string fileName)
        {
            string path = SaveDataPath(saveConfig, fileName);
            return File.Exists(path);
        }
        
        public static bool MetaDataExists(ISaveConfig saveConfig, string fileName)
        {
            string path = MetaDataPath(saveConfig, fileName);
            return File.Exists(path);
        }
        
        public static async void WriteDataAsync<T>(SaveMetaData saveMetaData, T saveData, 
            ISaveConfig saveConfig, string fileName, Action onComplete = null) where T : class
        {
            if (Directory.Exists(DirectoryPath(saveConfig)))
            {
                Directory.CreateDirectory(saveConfig.SavePath);
            }
            
            var formatter = new BinaryFormatter();
            
            //write save Data to disk
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            var saveDataStream = new FileStream(saveDataPath, FileMode.Create);
            await Task.Run(() => formatter.Serialize(saveDataStream, saveData));
            
            saveMetaData.SetChecksum(HashingUtility.GenerateHash(saveDataStream));
            
            //write meta Data to disk
            var metaDataPath = MetaDataPath(saveConfig, fileName);
            var metaDataStream = new FileStream(metaDataPath, FileMode.Create);
            await Task.Run(() => formatter.Serialize(metaDataStream, saveMetaData));
        
            saveDataStream.Close();
            metaDataStream.Close();
            
            onComplete?.Invoke();
            Debug.LogWarning("Save Successful");
        }
        
        public static async void DeleteAsync(ISaveConfig saveConfig, string fileName, Action onComplete = null)
        {
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            var metaDataPath = MetaDataPath(saveConfig, fileName);

            if (!MetaDataExists(saveConfig, fileName) ||
                !SaveDataExists(saveConfig, fileName))
            {
                Debug.LogWarning("Save data or meta data file not found.");
                return;
            }
            
            await Task.Run(() => File.Delete(saveDataPath));
            await Task.Run(() => File.Delete(metaDataPath));
            
            onComplete?.Invoke();
        }

        public static SaveMetaData ReadMetaData(ISaveConfig saveConfig, string fileName)
        {
            var metaDataPath = MetaDataPath(saveConfig, fileName);
            return ReadData<SaveMetaData>(metaDataPath);
        }

        public static bool IsValidVersion(SaveMetaData metaData, SaveVersion currentVersion)
        {
            if (metaData.SaveVersion < currentVersion)
            {
                Debug.LogWarning($"The version of the loaded save '{metaData.SaveVersion}' is older than the local version '{currentVersion}'");
                return false;
            }
            if (metaData.SaveVersion > currentVersion)
            {
                Debug.LogWarning($"The version of the loaded save '{metaData.SaveVersion}' is newer than the local version '{currentVersion}'");
                return false;
            }

            return true;
        }
        
        public static SaveData ReadSaveDataSecure(ISaveConfig saveConfig, string fileName)
        {
            var metaData = ReadMetaData(saveConfig, fileName);
            if (metaData == null) return null;
            
            //check save version
            if (!IsValidVersion(metaData, saveConfig.GetSaveVersion())) return null;
            
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            return ReadData<SaveData>(saveDataPath, stream =>
            {
                if (metaData.GetChecksum() == HashingUtility.GenerateHash(stream))
                {
                    Debug.LogWarning("Integrity Check Successful!");
                }
                
                Debug.LogError("The save data didn't pass the data integrity check!");
            });
        }

        #endregion
        

        #region Private

        private static string DirectoryPath(ISaveConfig saveConfig) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath);

        private static string MetaDataPath(ISaveConfig saveConfig, string fileName) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath, $"{fileName}.{saveConfig.MetaDataExtensionName}");

        private static string SaveDataPath(ISaveConfig saveConfig, string fileName) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath, $"{fileName}.{saveConfig.ExtensionName}");

        //TODO: async
        private static T ReadData<T>(string saveDataPath, Action<FileStream> onDeserializeSuccessful = null) where T : class
        {
            if (File.Exists(saveDataPath))
            {
                var formatter = new BinaryFormatter();
                var metaStream = new FileStream(saveDataPath, FileMode.Open);

                if (formatter.Deserialize(metaStream) is T saveData)
                {
                    onDeserializeSuccessful?.Invoke(metaStream);
                    metaStream.Close();
                    return saveData;
                }
                
                Debug.LogError("An error occured while deserialization of the save data!");
                return null;
            }
            
            Debug.LogError("Save file not found in " + saveDataPath);
            return null;
        }

        #endregion
    }
}
