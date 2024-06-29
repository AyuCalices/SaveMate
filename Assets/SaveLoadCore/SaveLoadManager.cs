using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SaveLoadCore.Integrity;
using UnityEngine;

namespace SaveLoadCore
{
    public static class SaveLoadManager
    {
        public static void Save<T>(T saveData, string savePath = "", string saveName = "player") where T : class
        {
            var formatter = new BinaryFormatter();
            
            var saveDataPath = $"{Application.persistentDataPath}{savePath}/{saveName}.data";
            var dataStream = new FileStream(saveDataPath, FileMode.Create);
            formatter.Serialize(dataStream, saveData);
            
            var metaDataPath = $"{Application.persistentDataPath}{savePath}/{saveName}.meta";
            var metaStream = new FileStream(metaDataPath, FileMode.Create);
            SaveMetaData saveMetaData = new SaveMetaData()
            {
                checksum = HashingUtility.GenerateHash(dataStream)
            };
            formatter.Serialize(metaStream, saveMetaData);
            
            dataStream.Close();
            metaStream.Close();
        }
        
        private static bool TryLoadData<T>(out T data, string savePath = "", string saveName = "player", string saveType = "data", Func<FileStream, T, bool> onDeserializeSuccessful = null) where T : class
        {
            data = default;
            var saveDataPath = $"{Application.persistentDataPath}{savePath}/{saveName}.{saveType}";
            if (File.Exists(saveDataPath))
            {
                var formatter = new BinaryFormatter();
                var metaStream = new FileStream(saveDataPath, FileMode.Open);

                if (formatter.Deserialize(metaStream) is T saveData)
                {
                    onDeserializeSuccessful?.Invoke(metaStream, saveData);
                    metaStream.Close();
                    data = saveData;
                    return true;
                }
                
                Debug.LogError("An error occured while deserialization of the save data!");
                return false;
            }
            
            Debug.LogError("Save file not found in " + saveDataPath);
            return false;
        }
    
        public static T Load<T>(string savePath = "", string saveName = "player") where T : class
        {
            if (!TryLoadData(out SaveMetaData metaData, savePath, saveName, "meta")) return null;
            
            var isLoadSuccessful = TryLoadData(out T saveData, savePath, saveName, "data", (stream, data) =>
            {
                if (metaData.checksum == HashingUtility.GenerateHash(stream))
                {
                    Debug.LogWarning("Integrity Check Successful!");
                    return true;
                }
                
                Debug.LogError("The save data didn't pass the data integrity check!");
                return false;
            });

            return isLoadSuccessful ? saveData : null;
        }

        public static bool SaveExists(string saveName = "/player.data", string savePath = "")
        {
            return File.Exists(Application.persistentDataPath + savePath + saveName);
        }
    }
}
