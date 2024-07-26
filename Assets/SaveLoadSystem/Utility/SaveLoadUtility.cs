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
        
        public static async Task WriteDataAsync<T>(SaveMetaData saveMetaData, T saveData, 
            ISaveConfig saveConfig, string fileName) where T : class
        {
            if (Directory.Exists(DirectoryPath(saveConfig)))
            {
                Directory.CreateDirectory(DirectoryPath(saveConfig));
            }
            
            var formatter = new BinaryFormatter();
            
            //write save Data to disk
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            var saveDataStream = new FileStream(saveDataPath, FileMode.Create);
            await Task.Run(() => formatter.Serialize(saveDataStream, saveData));
            
            saveMetaData.Checksum = HashingUtility.GenerateHash(saveDataStream);
            
            //write meta Data to disk
            var metaDataPath = MetaDataPath(saveConfig, fileName);
            var metaDataStream = new FileStream(metaDataPath, FileMode.Create);
            await Task.Run(() => formatter.Serialize(metaDataStream, saveMetaData));
        
            saveDataStream.Close();
            metaDataStream.Close();
            
            Debug.LogWarning("Save Successful");
        }
        
        public static async Task DeleteAsync(ISaveConfig saveConfig, string fileName)
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
        }

        public static async Task<SaveMetaData> ReadMetaDataAsync(ISaveConfig saveConfig, string fileName)
        {
            var metaDataPath = MetaDataPath(saveConfig, fileName);
            return await ReadDataAsync<SaveMetaData>(metaDataPath);
        }
        
        public static async Task<SaveData> ReadSaveDataSecureAsync(ISaveConfig saveConfig, string fileName)
        {
            var metaData = await ReadMetaDataAsync(saveConfig, fileName);
            if (metaData == null) return null;
            
            //check save version
            if (!IsValidVersion(metaData, saveConfig.GetSaveVersion())) return null;
            
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            return await ReadDataAsync<SaveData>(saveDataPath, stream =>
            {
                if (string.IsNullOrEmpty(metaData.Checksum)) return;
                
                if (metaData.Checksum == HashingUtility.GenerateHash(stream))
                {
                    Debug.LogWarning("Integrity Check Successful!");
                    return;
                }
                
                Debug.LogError("The save data didn't pass the data integrity check!");
            });
        }

        #endregion
        

        #region Private

        private static string DirectoryPath(ISaveConfig saveConfig) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath);

        private static string MetaDataPath(ISaveConfig saveConfig, string fileName) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath, $"{fileName}.{saveConfig.MetaDataExtensionName}");

        private static string SaveDataPath(ISaveConfig saveConfig, string fileName) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath, $"{fileName}.{saveConfig.ExtensionName}");

        private static bool IsValidVersion(SaveMetaData metaData, SaveVersion currentVersion)
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
        
        public static async Task<T> ReadDataAsync<T>(string saveDataPath, Action<FileStream> onDeserializeSuccessful = null) where T : class
        {
            if (File.Exists(saveDataPath))
            {
                var formatter = new BinaryFormatter();

                try
                {
                    using (var metaStream = new FileStream(saveDataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    {
                        var saveData = await Task.Run(() => formatter.Deserialize(metaStream) as T);
                        if (saveData != null)
                        {
                            onDeserializeSuccessful?.Invoke(metaStream);
                            return saveData;
                        }

                        Debug.LogError("An error occurred while deserialization of the save data!");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("An error occurred: " + ex.Message);
                    return null;
                }
            }

            Debug.LogError("Save file not found in " + saveDataPath);
            return null;
        }

        #endregion
    }
}
