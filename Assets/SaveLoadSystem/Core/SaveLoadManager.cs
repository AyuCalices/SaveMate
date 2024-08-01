using System;
using System.Collections.Generic;
using System.Text;
using SaveLoadSystem.Core.Component;
using SaveLoadSystem.Core.Integrity;
using SaveLoadSystem.Core.SerializableTypes;
using SaveLoadSystem.Core.SerializeStrategy;
using SaveLoadSystem.Utility;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class SaveLoadManager : ScriptableObject, ISaveConfig, ISaveStrategy
    {
        [Header("Version")] 
        [SerializeField] private int major;
        [SerializeField] private int minor;
        [SerializeField] private int patch;
        
        [Header("File Name")]
        [SerializeField] private string defaultFileName;
        [SerializeField] private string savePath;
        [SerializeField] private string extensionName;
        [SerializeField] private string metaDataExtensionName;
        
        [Header("Storage")]
        [SerializeField] private SaveIntegrityType integrityCheckType;
        [SerializeField] private SaveCompressionType compressionType;
        [SerializeField] private SaveEncryptionType encryptionType;
        [SerializeField] private string defaultEncryptionKey = "0123456789abcdef0123456789abcdef";
        [SerializeField] private string defaultEncryptionIv = "abcdef9876543210";
        
        [Header("Slot Settings")] 
        [SerializeField] private bool autoSaveOnSaveFocusSwap;
        
        public event Action<SaveFocus, SaveFocus> OnBeforeFocusChange;
        public event Action<SaveFocus, SaveFocus> OnAfterFocusChange;
        
        public string SavePath => savePath;
        public string ExtensionName => extensionName;
        public string MetaDataExtensionName => metaDataExtensionName;
        public SaveVersion SaveVersion => new(major, minor, patch);
        public HashSet<SaveSceneManager> TrackedSaveSceneManagers { get; } = new();
        public bool HasSaveFocus => _saveFocus != null;
        public SaveFocus SaveFocus
        {
            get
            {
                if (!HasSaveFocus)
                {
                    SetFocus();
                }

                return _saveFocus;
            }
        }

        private SaveFocus _saveFocus;
        private HashSet<SaveSceneManager> _scenesToReload;
        private byte[] _aesKey = Array.Empty<byte>();
        private byte[] _aesIv = Array.Empty<byte>();

        #region Simple Save

        public string[] GetAllSaveFiles()
        {
            return SaveLoadUtility.FindAllSaveFiles(this);
        }
        
        public void SimpleSaveActiveScenes(string fileName = null)
        {
            SetFocus(fileName);

            var activeScenes = UnityUtility.GetActiveScenes();
            SaveFocus.SnapshotScenes(activeScenes);
            SaveFocus.WriteToDisk();
        }

        public void SimpleLoadActiveScenes(string fileName = null)
        {
            SetFocus(fileName);

            if (SaveFocus.IsPersistent)
            {
                var activeScenes = UnityUtility.GetActiveScenes();
                SaveFocus.LoadScenes(activeScenes);
            }
            else
            {
                Debug.LogWarning($"Couldn't load, because there is no save file with name '{fileName}.{ExtensionName}' at path '{SavePath}'");
            }
        }

        #endregion

        #region Focus Save

        public void SetFocus(string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = defaultFileName;
                Debug.LogWarning("Initialized the save system with the default file Name.");
            }
            
            if (HasSaveFocus)
            {
                //same to current save
                if (_saveFocus.FileName == fileName) return;
                
                //other save, but still has pending data that can be saved
                if (autoSaveOnSaveFocusSwap)
                {
                    _saveFocus.SaveScenes();
                }
            }
            
            SwapFocus(new SaveFocus(this, fileName));
        }

        public void ReleaseFocus()
        {
            //save pending data if allowed
            if (HasSaveFocus && autoSaveOnSaveFocusSwap)
            {
                _saveFocus.SaveScenes();
            }
            
            SwapFocus(null);
        }

        #endregion

        public ICompressionStrategy GetCompressionStrategy()
        {
            return compressionType switch
            {
                SaveCompressionType.None => new NoneCompressionSerializationStrategy(),
                SaveCompressionType.Gzip => new GzipCompressionSerializationStrategy(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public ISerializationStrategy GetSerializationStrategy()
        {
            return new JsonSerializeStrategy();
        }

        public IEncryptionStrategy GetEncryptionStrategy()
        {
            switch (encryptionType)
            {
                case SaveEncryptionType.None:
                    return new NoneEncryptSerializeStrategy();
                
                case SaveEncryptionType.Aes:
                    if (_aesKey.Length == 0 || _aesIv.Length == 0)
                    {
                        return new AesEncryptSerializeStrategy(Encoding.UTF8.GetBytes(defaultEncryptionKey), Encoding.UTF8.GetBytes(defaultEncryptionIv));
                    }
                    return new AesEncryptSerializeStrategy(_aesKey, _aesIv);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public IIntegrityStrategy GetIntegrityStrategy()
        {
            return integrityCheckType switch
            {
                SaveIntegrityType.None => new EmptyIntegrityStrategy(),
                SaveIntegrityType.Sha256Hashing => new Sha256HashingIntegrityStrategy(),
                SaveIntegrityType.CRC32 => new CRC32IntegrityStrategy(),
                SaveIntegrityType.Adler32 => new Adler32IntegrityStrategy(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void SetAesEncryption(byte[] aesKey, byte[] aesIv)
        {
            _aesKey = aesKey;
            _aesIv = aesIv;
        }

        public void ClearAesEncryption()
        {
            _aesKey = Array.Empty<byte>();
            _aesIv = Array.Empty<byte>();
        }

        #region Internal

        internal void RegisterSaveSceneManager(SaveSceneManager saveSceneManager)
        {
            TrackedSaveSceneManagers.Add(saveSceneManager);
        }
        
        internal void UnregisterSaveSceneManager(SaveSceneManager saveSceneManager)
        {
            TrackedSaveSceneManagers.Remove(saveSceneManager);
        }

        #endregion

        #region Private

        private void SwapFocus(SaveFocus newSaveFocus)
        {
            SaveFocus oldSaveFocus = _saveFocus;
            
            OnBeforeFocusChange?.Invoke(oldSaveFocus, newSaveFocus);

            _saveFocus = newSaveFocus;
            
            OnAfterFocusChange?.Invoke(oldSaveFocus, newSaveFocus);
        }

        #endregion
    }
}
