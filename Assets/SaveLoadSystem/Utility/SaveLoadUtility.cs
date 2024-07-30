using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
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
    
            // Write save data to disk
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            using (var saveDataStream = new FileStream(saveDataPath, FileMode.Create))
            {
                saveMetaData.Checksum = HashingUtility.GenerateHash(saveDataStream);
                await saveConfig.SerializeStrategy.SerializeAsync(saveDataStream, saveData);
            }
    
            // Write meta data to disk
            var metaDataPath = MetaDataPath(saveConfig, fileName);
            using (var metaDataStream = new FileStream(metaDataPath, FileMode.Create))
            {
                await saveConfig.SerializeStrategy.SerializeAsync(metaDataStream, saveMetaData);
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
            return await ReadDataAsync<SaveMetaData>(saveConfig.SerializeStrategy, metaDataPath);
        }
        
        public static async Task<SaveData> ReadSaveDataSecureAsync(SaveVersion saveVersion, ISaveConfig saveConfig, string fileName)
        {
            var metaData = await ReadMetaDataAsync(saveConfig, fileName);
            if (metaData == null) return null;
            
            //check save version
            if (!IsValidVersion(metaData, saveVersion)) return null;
            
            var saveDataPath = SaveDataPath(saveConfig, fileName);
            return await ReadDataAsync<SaveData>(saveConfig.SerializeStrategy, saveDataPath, stream =>
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
        
        public static async Task<T> ReadDataAsync<T>(ISerializeStrategy serializationStrategy, string saveDataPath, Action<FileStream> onDeserializeSuccessful = null) where T : class
        {
            if (!File.Exists(saveDataPath))
            {
                Debug.LogError("Save file not found in " + saveDataPath);
                return null;
            }
    
            try
            {
                using (var saveDataStream = new FileStream(saveDataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    var saveData = await serializationStrategy.DeserializeAsync<T>(saveDataStream);
                    if (saveData != null)
                    {
                        onDeserializeSuccessful?.Invoke(saveDataStream);
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

        #endregion
    }
    
    public interface ISerializeStrategy
    {
        Task SerializeAsync<T>(Stream stream, T data);
        Task<T> DeserializeAsync<T>(Stream stream) where T : class;
    }
    
    public interface INestedSerializeStrategy : ISerializeStrategy
    {
        ISerializeStrategy SerializeStrategy { get; set; }
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
            using (var writer = new StreamWriter(stream))
            {
                var json = JsonConvert.SerializeObject(data);
                await writer.WriteAsync(json);
            }
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
    
    public class FallThroughSerializeStrategy : INestedSerializeStrategy
    {
        public ISerializeStrategy SerializeStrategy { get; set; }

        public FallThroughSerializeStrategy(ISerializeStrategy serializeStrategy)
        {
            SerializeStrategy = serializeStrategy;
        }

        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            await SerializeStrategy.SerializeAsync(stream, data);
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            return await SerializeStrategy.DeserializeAsync<T>(stream);
        }
    }
    
    public class AesEncryptSerializeStrategy : INestedSerializeStrategy
    {
        public ISerializeStrategy SerializeStrategy { get; set; }

        public AesEncryptSerializeStrategy(ISerializeStrategy serializeStrategy)
        {
            SerializeStrategy = serializeStrategy;
        }

        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            using (var ms = new MemoryStream())
            {
                await SerializeStrategy.SerializeAsync(ms, data);
                
                var encryptedData = AesEncryptionUtility.Encrypt(ms.ToArray());
                await stream.WriteAsync(encryptedData, 0, encryptedData.Length);
            }
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                
                var decryptedData = AesEncryptionUtility.Decrypt(ms.ToArray());
                using (var decryptedStream = new MemoryStream(decryptedData))
                {
                    return await SerializeStrategy.DeserializeAsync<T>(decryptedStream);
                }
            }
        }
    }
    
    public class GzipCompressionSerializationStrategy : INestedSerializeStrategy
    {
        public ISerializeStrategy SerializeStrategy { get; set; }

        public GzipCompressionSerializationStrategy(ISerializeStrategy serializeStrategy)
        {
            SerializeStrategy = serializeStrategy;
        }

        public async Task SerializeAsync<T>(Stream stream, T data)
        {
            using (var ms = new MemoryStream())
            {
                await SerializeStrategy.SerializeAsync(ms, data);
                
                var compressedData = GzipCompressionUtility.Compress(ms.ToArray());
                await stream.WriteAsync(compressedData, 0, compressedData.Length);
            }
        }

        public async Task<T> DeserializeAsync<T>(Stream stream) where T : class
        {
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                var decompressedData = GzipCompressionUtility.Decompress(ms.ToArray());
                
                using (var decompressedStream = new MemoryStream(decompressedData))
                {
                    return await SerializeStrategy.DeserializeAsync<T>(decompressedStream);
                }
            }
        }
    }
    
    public static class AesEncryptionUtility
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"); // Replace with your key
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("abcdef9876543210"); // Replace with your IV

        public static byte[] Encrypt(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(data))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var resultStream = new MemoryStream())
                {
                    cs.CopyTo(resultStream);
                    return resultStream.ToArray();
                }
            }
        }
    }
    
    public static class GzipCompressionUtility
    {
        public static byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
                gzipStream.Close();
                return compressedStream.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                gzipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}
