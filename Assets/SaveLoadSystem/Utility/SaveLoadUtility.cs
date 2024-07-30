using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SaveLoadSystem.Core;
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

    public interface IIntegrityStrategy
    {
        string ComputeChecksum(Stream stream);
    }
    
    public class EmptyIntegrityStrategy : IIntegrityStrategy
    {
        public string ComputeChecksum(Stream stream)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Hashing (SHA-256)
    /// 
    /// Advantages:
    /// - Security: Cryptographic hashes like SHA-256 are designed to be secure against intentional data tampering, providing strong guarantees of data integrity and authenticity.
    /// - Collision Resistance: Cryptographic hashes are designed to minimize the likelihood of two different inputs producing the same hash output (collisions).
    /// - Versatility: They are widely used in various security applications, including digital signatures, data integrity checks, and password hashing.
    ///
    /// Disadvantages:
    /// - Performance: Cryptographic hashes are computationally more intensive than Adler-32 and CRC-32, which can be a disadvantage in performance-critical applications.
    /// - Complexity: Implementing cryptographic hash functions correctly can be more complex due to the need for understanding security properties and ensuring resistance against various attack vectors.
    /// </summary>
    public class HashingIntegrityStrategy : IIntegrityStrategy
    {
        public string ComputeChecksum(Stream stream)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);
            return Convert.ToBase64String(hashBytes);
        }
    }
    
    /// <summary>
    /// Adler-32
    /// 
    /// Advantages:
    /// - Speed: Adler-32 is faster to compute than CRC-32 and cryptographic hashes, making it suitable for applications where performance is critical.
    /// - Simplicity: The algorithm is simpler to implement compared to CRC-32 and cryptographic hashes.
    ///
    /// Disadvantages:
    /// - Error Detection: Adler-32 is less reliable in detecting errors compared to CRC-32 and cryptographic hashes, especially for small data sets or simple error patterns.
    /// - Security: It is not suitable for cryptographic purposes as it does not provide resistance against intentional data tampering.
    /// </summary>
    public class Adler32IntegrityStrategy : IIntegrityStrategy
    {
        private const uint ModAdler = 65521;
        
        public string ComputeChecksum(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            uint a = 1, b = 0;

            int bufferLength = 1024;
            byte[] buffer = new byte[bufferLength];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    a = (a + buffer[i]) % ModAdler;
                    b = (b + a) % ModAdler;
                }
            }

            uint checksum = (b << 16) | a;
            return checksum.ToString("X8");
        }
    }
    
    /// <summary>
    /// CRC-32
    /// 
    /// Advantages:
    /// - Error Detection: CRC-32 provides better error detection capabilities than Adler-32, making it more suitable for detecting accidental changes to raw data.
    /// - Widely Used: It is a well-established standard used in many networking protocols and file formats.
    ///
    /// Disadvantages:
    /// - Performance: CRC-32 is slower to compute than Adler-32.
    /// - Security: Like Adler-32, CRC-32 is not suitable for cryptographic purposes as it does not protect against intentional data tampering.
    /// </summary>
    public class CRC32IntegrityStrategy : IIntegrityStrategy
    {
        private readonly uint[] Table;

        public CRC32IntegrityStrategy()
        {
            uint polynomial = 0xedb88320;
            Table = new uint[256];

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (uint j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                    {
                        crc = (crc >> 1) ^ polynomial;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
                Table[i] = crc;
            }
        }
        
        public string ComputeChecksum(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            uint crc = 0xffffffff;

            int bufferLength = 1024;
            byte[] buffer = new byte[bufferLength];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    byte index = (byte)(((crc) & 0xff) ^ buffer[i]);
                    crc = (uint)((crc >> 8) ^ Table[index]);
                }
            }

            crc ^= 0xffffffff;
            return crc.ToString("X8");
        }
    }
}
