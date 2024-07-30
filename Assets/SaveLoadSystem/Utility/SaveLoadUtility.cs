using System;
using System.IO;
using System.Threading.Tasks;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.SerializableTypes;
using SaveLoadSystem.Core.SerializeStrategy;
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
        
        public static async Task WriteDataAsync<T>(ISaveConfig saveConfig, string fileName, 
            SaveMetaData saveMetaData, T saveData) where T : class
        {
            if (!Directory.Exists(DirectoryPath(saveConfig)))
            {
                Directory.CreateDirectory(DirectoryPath(saveConfig));
            }

            var serializationStrategy = saveConfig.GetSerializeStrategy();
            
            // Write save data to disk
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            await using (var saveDataStream = new FileStream(saveDataPath, FileMode.Create))
            {
                saveMetaData.Checksum = saveConfig.GetIntegrityStrategy().ComputeChecksum(saveDataStream);
                await serializationStrategy.SerializeAsync(saveDataStream, saveData);
            }
    
            // Write meta data to disk
            var metaDataPath = MetaDataPath(saveConfig, fileName);
            await using (var metaDataStream = new FileStream(metaDataPath, FileMode.Create))
            {
                await serializationStrategy.SerializeAsync(metaDataStream, saveMetaData);
            }
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
            return await ReadDataAsync<SaveMetaData>(saveConfig.GetSerializeStrategy(), metaDataPath);
        }
        
        public static async Task<SaveData> ReadSaveDataSecureAsync(SaveVersion saveVersion, ISaveConfig saveConfig, string fileName)
        {
            var metaData = await ReadMetaDataAsync(saveConfig, fileName);
            if (metaData == null) return null;
            
            //check save version
            if (!IsValidVersion(metaData, saveVersion)) return null;
            
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            return await ReadDataAsync<SaveData>(saveConfig.GetSerializeStrategy(), saveDataPath, stream =>
            {
                if (string.IsNullOrEmpty(metaData.Checksum)) return;
                
                if (metaData.Checksum == saveConfig.GetIntegrityStrategy().ComputeChecksum(stream))
                {
                    Debug.LogWarning("Integrity Check Successful!");
                    return;
                }
                
                Debug.LogError("The save data didn't pass the data integrity check!");
            });
        }

        #endregion
        

        #region Private

        private static string DirectoryPath(ISaveConfig saveConfig) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath) + Path.AltDirectorySeparatorChar;

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

        private static async Task<T> ReadDataAsync<T>(ISerializeStrategy serializationStrategy, string saveDataPath, Action<FileStream> onDeserializeSuccessful = null) where T : class
        {
            if (!File.Exists(saveDataPath))
            {
                Debug.LogError("Save file not found in " + saveDataPath);
                return null;
            }
    
            try
            {
                await using var saveDataStream = new FileStream(saveDataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var saveData = await serializationStrategy.DeserializeAsync<T>(saveDataStream);
                if (saveData != null)
                {
                    onDeserializeSuccessful?.Invoke(saveDataStream);
                    return saveData;
                }
                Debug.LogError("An error occurred while deserialization of the save data!");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError("An error occurred: " + ex.Message);
                return null;
            }
        }

        #endregion
    }
}
