using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

namespace SaveLoadSystem.Utility
{
    internal static class SaveLoadUtility
    {
        #region Public

        internal static string[] FindAllSaveFiles(ISaveConfig saveConfig)
        {
            string directoryPath = DirectoryPath(saveConfig);
            
            if (Directory.Exists(directoryPath))
            {
                return Directory.GetFiles(directoryPath, $"*.{saveConfig.MetaDataExtensionName}");
            }

            return Array.Empty<string>();
        }
        
        internal static bool SaveDataExists(ISaveConfig saveConfig, string fileName)
        {
            string path = SaveDataPath(saveConfig, fileName);
            return File.Exists(path);
        }
        
        internal static bool MetaDataExists(ISaveConfig saveConfig, string fileName)
        {
            string path = MetaDataPath(saveConfig, fileName);
            return File.Exists(path);
        }
        
        internal static async Task WriteDataAsync<T>(ISaveConfig saveConfig, ISaveStrategy saveStrategy, string fileName, 
            SaveMetaData saveMetaData, T saveData) where T : class
        {
            if (!Directory.Exists(DirectoryPath(saveConfig)))
            {
                Directory.CreateDirectory(DirectoryPath(saveConfig));
            }
            
            try
            {
                // Prepare save data
                var serializedSaveData = await saveStrategy.GetSerializationStrategy().SerializeAsync(saveData);
                var compressedSaveData = await saveStrategy.GetCompressionStrategy().CompressAsync(serializedSaveData);
                var encryptedSaveData = await saveStrategy.GetEncryptionStrategy().EncryptAsync(compressedSaveData);
                saveMetaData.Checksum = saveStrategy.GetIntegrityStrategy().ComputeChecksum(encryptedSaveData);
                
                // Prepare meta data
                var serializedData = await saveStrategy.GetSerializationStrategy().SerializeAsync(saveMetaData);
                var compressedData = await saveStrategy.GetCompressionStrategy().CompressAsync(serializedData);
                var encryptedData = await saveStrategy.GetEncryptionStrategy().EncryptAsync(compressedData);
                
                // Write to disk
                var metaDataPath = MetaDataPath(saveConfig, fileName);
                await using var metaDataStream = new FileStream(metaDataPath, FileMode.Create);
                await metaDataStream.WriteAsync(encryptedData, 0, encryptedData.Length);
                
                var saveDataPath = SaveDataPath(saveConfig, fileName);
                await using var saveDataStream = new FileStream(saveDataPath, FileMode.Create);
                await saveDataStream.WriteAsync(encryptedSaveData, 0, encryptedSaveData.Length);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        internal static async Task DeleteAsync(ISaveConfig saveConfig, string fileName)
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

        internal static async Task<SaveMetaData> ReadMetaDataAsync(ISaveStrategy saveStrategy, ISaveConfig saveConfig, string fileName)
        {
            var metaDataPath = MetaDataPath(saveConfig, fileName);
            return await ReadDataAsync<SaveMetaData>(saveStrategy, metaDataPath);
        }
        
        internal static async Task<RootSaveData> ReadSaveDataSecureAsync(ISaveStrategy saveStrategy, SaveVersion saveVersion, ISaveConfig saveConfig, string fileName)
        {
            var metaData = await ReadMetaDataAsync(saveStrategy, saveConfig, fileName);
            if (metaData == null) return null;
            
            //check save version
            if (!IsValidVersion(metaData, saveVersion)) return null;
            
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            return await ReadDataAsync<RootSaveData>(saveStrategy, saveDataPath, metaData.Checksum);
        }

        #endregion
        

        #region Private

        private static string DirectoryPath(ISaveConfig saveConfig) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath) + Path.AltDirectorySeparatorChar;

        private static string MetaDataPath(ISaveConfig saveConfig, string fileName) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath, $"{fileName}.{saveConfig.MetaDataExtensionName}");

        private static string SaveDataPath(ISaveConfig saveConfig, string fileName) => Path.Combine(Application.persistentDataPath, saveConfig.SavePath, $"{fileName}.{saveConfig.SaveDataExtensionName}");

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

        private static async Task<T> ReadDataAsync<T>(ISaveStrategy saveStrategy, string saveDataPath, string checksum = null) where T : class
        {
            if (!File.Exists(saveDataPath))
            {
                Debug.LogError("Save file not found in " + saveDataPath);
                return null;
            }
    
            try
            {
                byte[] encryptedData;
                
                await using (FileStream fileStream = new FileStream(saveDataPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
                {
                    encryptedData = new byte[fileStream.Length];
                    var readAsync = await fileStream.ReadAsync(encryptedData, 0, encryptedData.Length);

                    if (checksum != null)
                    {
                        if (checksum != saveStrategy.GetIntegrityStrategy().ComputeChecksum(encryptedData))
                        {
                            Debug.LogError("The save data didn't pass the data integrity check!");
                            return null;
                        }
                    }
                }
                
                var decryptedData = await saveStrategy.GetEncryptionStrategy().DecryptAsync(encryptedData);
                var serializedData = await saveStrategy.GetCompressionStrategy().DecompressAsync(decryptedData);
                return (T)await saveStrategy.GetSerializationStrategy().DeserializeAsync(serializedData, typeof(T));
            }
            catch (Exception ex)
            {
                Debug.LogError("An error occurred: " + ex.Message);
                return null;
            }
        }

        #endregion
        
        /// <summary>
        /// Generates a random ID of the specified length.
        /// </summary>
        /// <param name="length">The length of the ID to generate. Default is 5.</param>
        /// <returns>A random string ID.</returns>
        internal static string GenerateId(int length = 5)
        {
            const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789";
            
            var id = new char[length];
            for (var i = 0; i < length; i++)
            {
                // Random.Range with int parameters is inclusive on the lower bound and exclusive on the upper bound.
                var index = UnityEngine.Random.Range(0, allowedChars.Length);
                id[i] = allowedChars[index];
            }
            return new string(id);
        }
        
        internal static void CheckUniqueGuidOnInspectorInput<T>(
            IEnumerable<T> items,
            Func<T, UnityEngine.Object> getUnityObject,
            Func<T, string> getGuid,
            string errorMessagePrefix)
        {
            var guidLookup = new Dictionary<string, (bool Duplicated, HashSet<string> HashSet)>();

            foreach (var item in items)
            {
                var unityObject = getUnityObject(item);
                if (unityObject.IsUnityNull()) continue;
                
                var guid = getGuid(item);
                if (string.IsNullOrEmpty(guid)) continue;

                var stringToAdd = $"'{unityObject.name}'";
                if (!guidLookup.TryGetValue(guid, out var uniqueWithCount))
                {
                    guidLookup.Add(guid, (false, new HashSet<string> { stringToAdd }));
                }
                else
                {
                    uniqueWithCount.HashSet.Add(stringToAdd);
                    guidLookup[guid] = (true, uniqueWithCount.HashSet);
                }
            }

            foreach (var (guid, uniqueWithCount) in guidLookup)
            {
                if (uniqueWithCount.Duplicated)
                {
                    Debug.LogError($"{errorMessagePrefix}. Please ensure all GUIDs are unique. Duplicated guid: '{guid}'. Duplications detected on: {string.Join(" | ", uniqueWithCount.HashSet)}.");
                }
            }
        }
    }
}
