using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] public int major;
        [SerializeField] public int minor;
        [SerializeField] public int patch;
        
        [Header("File Name")]
        [SerializeField] public string defaultFileName;
        [SerializeField] public string savePath;
        [SerializeField] public string extensionName;
        [SerializeField] public string metaDataExtensionName;
        
        [Header("Storage")]
        [SerializeField] public SaveIntegrityType integrityCheckType;
        [SerializeField] public SaveCompressionType compressionType;
        [SerializeField] public SaveEncryptionType encryptionType;
        [SerializeField] public string defaultEncryptionKey = "0123456789abcdef0123456789abcdef";
        [SerializeField] public string defaultEncryptionIv = "abcdef9876543210";

        [Header("Other")] 
        [SerializeField] private AssetRegistry assetRegistry;
        [SerializeField] public bool autoSaveOnSaveFocusSwap;
        
        public event Action<SaveFileContext, SaveFileContext> OnBeforeFocusChange;
        public event Action<SaveFileContext, SaveFileContext> OnAfterFocusChange;
        
        public string SavePath => savePath;
        public string ExtensionName => extensionName;
        public string MetaDataExtensionName => metaDataExtensionName;
        public SaveVersion SaveVersion => new(major, minor, patch);
        public bool HasSaveFileContext => _saveFileContext != null;
        public SaveFileContext CurrentSaveFileContext
        {
            get
            {
                if (!HasSaveFileContext)
                {
                    SetFocus();
                }

                return _saveFileContext;
            }
        }

        private readonly List<SimpleSceneSaveManager> _trackedSaveSceneManagers = new();
        private static SimpleSceneSaveManager _dontDestroyOnLoadManager;
        
        private SaveFileContext _saveFileContext;
        private HashSet<SceneSaveManager> _scenesToReload;
        private byte[] _aesKey = Array.Empty<byte>();
        private byte[] _aesIv = Array.Empty<byte>();
        
        
        internal readonly Dictionary<GuidPath, ScriptableObject> GuidToScriptableObjectLookup = new();
        internal readonly Dictionary<ScriptableObject, GuidPath> ScriptableObjectToGuidLookup = new();
        
        internal readonly Dictionary<string, Savable> GuidToSavablePrefabsLookup = new();
        

        private void OnEnable()
        {
            foreach (var scriptableObjectSavable in assetRegistry.ScriptableObjectSavables)
            {
                var guidPath = new GuidPath(RootSaveData.GlobalSaveDataName, scriptableObjectSavable.guid);
                ScriptableObjectToGuidLookup.Add((ScriptableObject)scriptableObjectSavable.unityObject, guidPath);
                GuidToScriptableObjectLookup.Add(guidPath, (ScriptableObject)scriptableObjectSavable.unityObject);
            }

            foreach (var prefabSavable in assetRegistry.PrefabSavables)
            {
                GuidToSavablePrefabsLookup.Add(prefabSavable.PrefabGuid, prefabSavable);
            }
        }

        private void OnDisable()
        {
            GuidToScriptableObjectLookup.Clear();
            GuidToSavablePrefabsLookup.Clear();
            ScriptableObjectToGuidLookup.Clear();
        }

        #region Simple Save

        public string[] GetAllSaveFiles()
        {
            return SaveLoadUtility.FindAllSaveFiles(this);
        }
        
        public void SimpleSaveActiveScenes(string fileName = null)
        {
            SetFocus(fileName);

            CurrentSaveFileContext.SaveActiveScenes();
        }

        public void SimpleLoadActiveScenes(LoadType loadType = LoadType.Hard, string fileName = null)
        {
            SetFocus(fileName);
            
            CurrentSaveFileContext.Load(loadType, GetTrackedSaveSceneManagers().Cast<IRestoreSnapshotGroupElement>().ToArray());
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
            
            if (HasSaveFileContext)
            {
                //same to current save
                if (_saveFileContext.FileName == fileName) return;
                
                //other save, but still has pending data that can be saved
                if (autoSaveOnSaveFocusSwap)
                {
                    _saveFileContext.Save();
                }
            }
            
            SwapFocus(new SaveFileContext(this, fileName));
        }

        public void ReleaseFocus()
        {
            //save pending data if allowed
            if (HasSaveFileContext && autoSaveOnSaveFocusSwap)
            {
                _saveFileContext.Save();
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

        public List<SimpleSceneSaveManager> GetTrackedSaveSceneManagers()
        {
            if (_dontDestroyOnLoadManager != null && !_trackedSaveSceneManagers.Contains(_dontDestroyOnLoadManager))
            {
                _trackedSaveSceneManagers.Add(_dontDestroyOnLoadManager);
            }

            if (_dontDestroyOnLoadManager == null && _trackedSaveSceneManagers.Contains(_dontDestroyOnLoadManager))
            {
                _trackedSaveSceneManagers.Remove(_dontDestroyOnLoadManager);
            }

            return _trackedSaveSceneManagers;
        }

        internal void RegisterSaveSceneManager(SceneSaveManager sceneSaveManager)
        {
            _trackedSaveSceneManagers.Add(sceneSaveManager);
        }
        
        internal bool UnregisterSaveSceneManager(SceneSaveManager sceneSaveManager)
        {
            return _trackedSaveSceneManagers.Remove(sceneSaveManager);
        }

        internal static SimpleSceneSaveManager GetDontDestroyOnLoadSceneManager()
        {
            if (!_dontDestroyOnLoadManager)
            {
                GameObject newObject = new GameObject("DontDestroyOnLoadObject");
                DontDestroyOnLoad(newObject);
                _dontDestroyOnLoadManager = newObject.AddComponent<SimpleSceneSaveManager>();
            }
            
            return _dontDestroyOnLoadManager;
        }

        internal static void DestroyDontDestroyOnLoadSceneManager()
        {
            Destroy(_dontDestroyOnLoadManager.gameObject);
            _dontDestroyOnLoadManager = null;
        }
        
        #endregion

        #region Private

        private void SwapFocus(SaveFileContext newSaveFileContext)
        {
            SaveFileContext oldSaveFileContext = _saveFileContext;
            
            OnBeforeFocusChange?.Invoke(oldSaveFileContext, newSaveFileContext);

            _saveFileContext = newSaveFileContext;
            
            OnAfterFocusChange?.Invoke(oldSaveFileContext, newSaveFileContext);
        }

        #endregion
    }
}
