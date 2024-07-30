using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Integrity;
using SaveLoadSystem.Core.Serializable;
using Unity.Plastic.Newtonsoft.Json;
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
                saveMetaData.Checksum = HashingUtility.GenerateHash(saveDataStream);
                await serializationStrategy.SerializeAsync(saveDataStream, saveData);
            }
    
            // Write meta data to disk
            var metaDataPath = MetaDataPath(saveConfig, fileName);
            await using (var metaDataStream = new FileStream(metaDataPath, FileMode.Create))
            {
                await serializationStrategy.SerializeAsync(metaDataStream, saveMetaData);
            }
    
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
    
    public interface ISerializeStrategy
    {
        Task SerializeAsync<T>(Stream stream, T data);
        Task<T> DeserializeAsync<T>(Stream stream) where T : class;
    }

    public class BinarySerializeStrategy : ISerializeStrategy
    {
        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            var formatter = new BinaryFormatter();
            await Task.Run(() => formatter.Serialize(stream, data));
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            var formatter = new BinaryFormatter();
            return await Task.Run(() => formatter.Deserialize(stream) as T);
        }
    }
    
    public class XmlSerializeStrategy : ISerializeStrategy
    {
        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            var serializer = new XmlSerializer(typeof(T));
            await Task.Run(() => serializer.Serialize(stream, data));
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            return await Task.Run(() => serializer.Deserialize(stream) as T);
        }
    }
    
    public class JsonSerializeStrategy : ISerializeStrategy
    {
        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            await using var writer = new StreamWriter(stream);
            var json = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(json);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
    
    public class AesEncryptSerializeStrategy : ISerializeStrategy
    {
        public ISerializeStrategy SerializeStrategy { get; set; }
        
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesEncryptSerializeStrategy(ISerializeStrategy serializeStrategy, byte[] key, byte[] iv)
        {
            SerializeStrategy = serializeStrategy;
            
            _key = key;
            _iv = iv;
        }

        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            using var ms = new MemoryStream();
            await SerializeStrategy.SerializeAsync(ms, data);
            var encryptedData = Encrypt(ms.ToArray());
            await stream.WriteAsync(encryptedData, 0, encryptedData.Length);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var decryptedData = Decrypt(ms.ToArray());
            using var decryptedStream = new MemoryStream(decryptedData);
            return await SerializeStrategy.DeserializeAsync<T>(decryptedStream);
        }

        private byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        private byte[] Decrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var resultStream = new MemoryStream();
            cs.CopyTo(resultStream);
            return resultStream.ToArray();
        }
    }
    
    public class GzipCompressionSerializationStrategy : ISerializeStrategy
    {
        public ISerializeStrategy SerializeStrategy { get; set; }

        public GzipCompressionSerializationStrategy(ISerializeStrategy serializeStrategy)
        {
            SerializeStrategy = serializeStrategy;
        }

        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            using var memoryStream = new MemoryStream();
            await SerializeStrategy.SerializeAsync(memoryStream, data);
            await using var gzipStream = new GZipStream(stream, CompressionMode.Compress);
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(gzipStream);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            using var decompressedStream = new MemoryStream();
            await stream.CopyToAsync(decompressedStream);
            decompressedStream.Position = 0;
            await using var gzipStream = new GZipStream(decompressedStream, CompressionMode.Decompress);
            var resultStream = new MemoryStream();
            await gzipStream.CopyToAsync(resultStream);
            resultStream.Position = 0;
            return await SerializeStrategy.DeserializeAsync<T>(resultStream);
        }
    }
}
