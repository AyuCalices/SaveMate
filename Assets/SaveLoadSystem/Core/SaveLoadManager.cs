using System;
using System.Collections.Generic;
using System.Text;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.Integrity;
using SaveLoadSystem.Core.SerializeStrategy;
using SaveLoadSystem.Core.UnityComponent;
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

        [Header("QOL")] 
        [SerializeField] private bool autoSaveOnSaveFocusSwap;
        
        public event Action<SaveLink, SaveLink> OnBeforeFocusChange;
        public event Action<SaveLink, SaveLink> OnAfterFocusChange;
        
        public string SavePath => savePath;
        public string ExtensionName => extensionName;
        public string MetaDataExtensionName => metaDataExtensionName;
        public SaveVersion SaveVersion => new(major, minor, patch);
        public List<SaveSceneManager> TrackedSaveSceneManagers { get; } = new();
        public bool HasSaveFocus => _saveLink != null;
        public SaveLink CurrentSaveLink
        {
            get
            {
                if (!HasSaveFocus)
                {
                    SetFocus();
                }

                return _saveLink;
            }
        }

        private SaveLink _saveLink;
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

            CurrentSaveLink.SaveActiveScenes();
        }

        public void SimpleLoadActiveScenes(LoadType loadType = LoadType.Hard, string fileName = null)
        {
            SetFocus(fileName);
            
            CurrentSaveLink.LoadScenes(loadType, TrackedSaveSceneManagers.ToArray());
        }

        #endregion

        #region Focus Save

        public void SetFocus(string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = defaultFileName;
                Debug.Log("Initialized the save system with the default file Name.");
            }
            
            if (HasSaveFocus)
            {
                //same to current save
                if (_saveLink.FileName == fileName) return;
                
                //other save, but still has pending data that can be saved
                if (autoSaveOnSaveFocusSwap)
                {
                    _saveLink.SaveScenes();
                }
            }
            
            SwapFocus(new SaveLink(this, fileName));
        }

        public void ReleaseFocus()
        {
            //save pending data if allowed
            if (HasSaveFocus && autoSaveOnSaveFocusSwap)
            {
                _saveLink.SaveScenes();
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
        
        internal bool UnregisterSaveSceneManager(SaveSceneManager saveSceneManager)
        {
            return TrackedSaveSceneManagers.Remove(saveSceneManager);
        }
        
        #endregion

        #region Private

        private void SwapFocus(SaveLink newSaveLink)
        {
            SaveLink oldSaveLink = _saveLink;
            
            OnBeforeFocusChange?.Invoke(oldSaveLink, newSaveLink);

            _saveLink = newSaveLink;
            
            OnAfterFocusChange?.Invoke(oldSaveLink, newSaveLink);
        }

        #endregion
    }
}
