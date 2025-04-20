using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SaveMate.Runtime.Core.DataTransferObject;
using SaveMate.Runtime.Core.SavableGroupInterfaces;
using SaveMate.Runtime.Core.SaveComponents.AssetScope;
using SaveMate.Runtime.Core.SaveComponents.GameObjectScope;
using SaveMate.Runtime.Core.SaveComponents.SceneScope;
using SaveMate.Runtime.Core.SaveStrategies;
using SaveMate.Runtime.Core.SaveStrategies.Compression;
using SaveMate.Runtime.Core.SaveStrategies.Encryption;
using SaveMate.Runtime.Core.SaveStrategies.Integrity;
using SaveMate.Runtime.Core.SaveStrategies.Serialization;
using SaveMate.Runtime.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveMate.Runtime.Core.SaveComponents.ManagingScope
{
    /// <summary>
    /// SaveMateManager is the core manager for handling save and load operations within a Unity project.
    /// It manages file names, paths, encryption, serialization, compression, metadata handling, and more.
    /// It also tracks save scene managers and executes various save strategies such as snapshot, load, and restore.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveMateManager", menuName = "Save Mate/Manager")]
    public class SaveMateManager : ScriptableObject, ISaveConfig, ISaveStrategy
    {
        [Header("Version")] 
        [SerializeField] private int major;
        [SerializeField] private int minor;
        [SerializeField] private int patch;
        
        [Header("File Name")]
        [SerializeField] private string defaultFileName = "defaultFileName";
        [SerializeField] private string savePath;
        [SerializeField] private string saveDataExtensionName = "savedata";
        [SerializeField] private string metaDataExtensionName = "metadata";
        [SerializeField] private Formatting jsonFormatting = Formatting.None;
        
        [Header("Storage")]
        [SerializeField] private SaveIntegrityType integrityCheckType;
        [SerializeField] private SaveCompressionType compressionType;
        [SerializeField] private SaveEncryptionType encryptionType;
        [SerializeField] private string defaultEncryptionKey = "0123456789abcdef0123456789abcdef";
        [SerializeField] private string defaultEncryptionIv = "abcdef9876543210";

        [Header("Other")] 
        [SerializeField] private AssetRegistry assetRegistry;
        
        public event Action<SaveFileContext, SaveFileContext> OnBeforeSaveFileContextChange;
        public event Action<SaveFileContext, SaveFileContext> OnAfterSaveFileContextChange;
        
        /// <summary>
        /// Gets the current version of the save data format.
        /// </summary>
        public SaveVersion SaveVersion => new(major, minor, patch);
        
        /// <summary>
        /// Gets or sets the directory path where save files are stored.
        /// </summary>
        public string SavePath
        {
            get => savePath;
            set => savePath = value;
        }

        /// <summary>
        /// Gets or sets the file extension used for save data files.
        /// </summary>
        public string SaveDataExtensionName
        {
            get => saveDataExtensionName;
            set => saveDataExtensionName = value;
        }

        /// <summary>
        /// Gets or sets the file extension used for save metadata files.
        /// </summary>
        public string MetaDataExtensionName
        {
            get => metaDataExtensionName;
            set => metaDataExtensionName = value;
        }
        
        private string _activeSaveFile; //TODO: may be need to be changed on file swap
        private SaveFileContext _currentSaveFileContext;
        private JsonSerializerSettings _jsonSerializerSettings;
        private byte[] _aesKey = Array.Empty<byte>();
        private byte[] _aesIv = Array.Empty<byte>();
        
        //scene save manager
        private static SimpleSceneSaveManager _dontDestroyOnLoadManager;
        private readonly List<SimpleSceneSaveManager> _trackedSaveSceneManagers = new();
        
        //reference lookup
        internal readonly Dictionary<GuidPath, ScriptableObject> GuidToScriptableObjectLookup = new();
        internal readonly Dictionary<ScriptableObject, GuidPath> ScriptableObjectToGuidLookup = new();
        internal readonly Dictionary<string, Savable> GuidToSavablePrefabsLookup = new();
        
        //meta data
        internal readonly JObject CustomMetaData = new();
        
        #region Unity Lifecycle

        
        private void OnEnable()
        {
            _activeSaveFile = defaultFileName;
            
            foreach (var scriptableObjectSavable in assetRegistry.ScriptableObjectSavables)
            {
                var guidPath = new GuidPath(SaveLoadUtility.ScriptableObjectDataName, scriptableObjectSavable.guid);

                if (scriptableObjectSavable.unityObject is not ScriptableObject scriptableObject) continue;
                
                if (ScriptableObjectToGuidLookup.TryAdd(scriptableObject, guidPath))
                {
                    GuidToScriptableObjectLookup.Add(guidPath, scriptableObject);
                }
            }

            foreach (var prefabSavable in assetRegistry.PrefabSavables)
            {
                if (prefabSavable == null) continue;
                
                GuidToSavablePrefabsLookup.TryAdd(prefabSavable.PrefabGuid, prefabSavable);
            }
        }

        private void OnDisable()
        {
            ReleaseSaveFileContext();
            
            GuidToScriptableObjectLookup.Clear();
            GuidToSavablePrefabsLookup.Clear();
            ScriptableObjectToGuidLookup.Clear();
        }

        
        #endregion
        
        #region File I/O
        
        /// <summary>
        /// Sets the name of the active save file. This affects future saves and loads.
        /// </summary>
        /// <param name="fileName">The name of the save file to activate.</param>
        public void SetActiveSaveFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                _activeSaveFile = defaultFileName;
                Debug.Log($"[SaveMate] {nameof(fileName)} missing: Swapped to the default file name: {fileName}.");
            }
            
            _activeSaveFile = fileName;
            UpdateSaveFileContext(fileName);
        }
        
        /// <summary>
        /// Retrieves all save files located in the save directory.
        /// </summary>
        public string[] GetAllSaveFiles()
        {
            return SaveFileUtility.FindAllSaveFiles(this);
        }

        /// <summary>
        /// Sets custom Newtonsoft Json serializer settings for serialization.
        /// </summary>
        /// <param name="settings">The JsonSerializerSettings to use.</param>
        public void SetJsonSerializerSettings(JsonSerializerSettings settings)
        {
            _jsonSerializerSettings = settings;
        }
        
        
        #endregion

        #region Encryption
        
        
        /// <summary>
        /// Sets AES encryption keys used during saving and loading.
        /// </summary>
        public void SetAesEncryption(byte[] aesKey, byte[] aesIv)
        {
            _aesKey = aesKey;
            _aesIv = aesIv;
        }

        /// <summary>
        /// Clears any AES encryption keys in use.
        /// </summary>
        public void ClearAesEncryption()
        {
            _aesKey = Array.Empty<byte>();
            _aesIv = Array.Empty<byte>();
        }

        
        #endregion

        #region MetaData
        
        
        /// <summary>
        /// Asynchronously fetches all available save metadata and invokes a callback when done.
        /// </summary>
        public async void FetchAllMetaData(Action<SaveMetaData[]> onAllMetaDataFound)
        {
            var saveMetaData = await FetchAllMetaData();
            
            onAllMetaDataFound.Invoke(saveMetaData);
        }
        
        /// <summary>
        /// Asynchronously fetches all available save metadata.
        /// </summary>
        public async Task<SaveMetaData[]> FetchAllMetaData()
        {
            var saveFileNames = GetAllSaveFiles();
            var saveMetaData = new SaveMetaData[saveFileNames.Length];

            for (var index = 0; index < saveFileNames.Length; index++)
            {
                var saveFileName = saveFileNames[index];

                saveMetaData[index] = await FetchMetaData(saveFileName);
            }

            return saveMetaData;
        }
        
        /// <summary>
        /// Asynchronously fetches metadata for a specific save file and invokes a callback when done.
        /// </summary>
        public async void FetchMetaData(string saveName, Action<SaveMetaData> onMetaDataFound)
        {
            var metaData = await FetchMetaData(saveName);
            
            onMetaDataFound.Invoke(metaData);
        }
        
        /// <summary>
        /// Asynchronously fetches metadata for a specific save file.
        /// </summary>
        public async Task<SaveMetaData> FetchMetaData(string saveName)
        {
            return await SaveFileUtility.ReadMetaDataAsync(this, this, saveName);
        }
        
        /// <summary>
        /// Adds or updates a metadata entry for the current save session.
        /// </summary>
        public void AddOrUpdateSaveInfo(string identifier, object obj)
        {
            CustomMetaData[identifier] = JToken.FromObject(obj);
        }

        /// <summary>
        /// Removes a specific metadata entry from the current session.
        /// </summary>
        public bool RemoveSaveInfo(string identifier)
        {
            return CustomMetaData.Remove(identifier);
        }
        
        /// <summary>
        /// Tries to extract and deserialize metadata of a given type from a SaveMetaData object.
        /// </summary>
        public bool TryGetSaveInfo<T>(SaveMetaData saveMetaData, string identifier, out T obj)
        {
            obj = default;

            var customMetaData = saveMetaData.CustomData;
            if (customMetaData?[identifier] == null)
            {
                Debug.LogWarning($"[SaveMate] Wasn't able to find the object of type '{typeof(T).Name}' for identifier '{identifier}' inside the meta data!");
                return false;
            }
            
            obj = customMetaData[identifier].ToObject<T>();
            return true;
        }

        
        #endregion
        
        #region OnCaptureState
        
        
        /// <summary>
        /// Captures and writes the current scene states to the active save file.
        /// </summary>
        public void SaveActiveScenes()
        {
            Save(_activeSaveFile, _trackedSaveSceneManagers.Cast<ISavableGroup>().ToArray());
        }

        /// <summary>
        /// Captures and writes the current scene states to the specified save file.
        /// </summary>
        public void SaveActiveScenes(string fileName)
        {
            Save(fileName, _trackedSaveSceneManagers.Cast<ISavableGroup>().ToArray());
        }
        
        /// <summary>
        /// Saves the given savable groups to the active save file.
        /// </summary>
        public void Save(params ISavableGroup[] savableGroups)
        {
            Save(_activeSaveFile, savableGroups);
        }
        
        /// <summary>
        /// Saves the given savable groups to the specified file.
        /// </summary>
        public void Save(string fileName, params ISavableGroup[] savableGroups)
        {
            UpdateSaveFileContext(fileName);
            
            _currentSaveFileContext.CaptureSnapshot(savableGroups.ToArray());
            _currentSaveFileContext.WriteToDisk();
        }
        

        #endregion

        #region CaptureSnapshot
        
        
        /// <summary>
        /// Captures a snapshot of the currently tracked scenes and stores it in memory.
        /// </summary>
        public void CaptureSnapshotActiveScenes()
        {
            CaptureSnapshot(_activeSaveFile, _trackedSaveSceneManagers.Cast<ISavableGroup>().ToArray());
        }

        /// <summary>
        /// Captures a snapshot of the specified scenes and stores it in memory.
        /// </summary>
        public void CaptureSnapshotActiveScenes(string fileName)
        {
            CaptureSnapshot(fileName, _trackedSaveSceneManagers.Cast<ISavableGroup>().ToArray());
        }

        /// <summary>
        /// Captures a snapshot of the provided savable groups into memory.
        /// </summary>
        public void CaptureSnapshot(params ISavableGroup[] savableGroups)
        {
            CaptureSnapshot(_activeSaveFile, savableGroups);
        }

        /// <summary>
        /// Captures a snapshot of the savable groups into memory for the specified file.
        /// </summary>
        public void CaptureSnapshot(string fileName, params ISavableGroup[] savableGroups)
        {
            UpdateSaveFileContext(fileName);
            
            _currentSaveFileContext.CaptureSnapshot(savableGroups);
        }

        
        #endregion

        #region WriteToDisk
        
        
        /// <summary>
        /// Writes the in-memory snapshot to disk using the specified file name.
        /// </summary>
        public void WriteToDisk()
        {
            WriteToDisk(_activeSaveFile);
        }

        /// <summary>
        /// Writes the in-memory snapshot to disk using the specified file name.
        /// </summary>
        public void WriteToDisk(string fileName)
        {
            UpdateSaveFileContext(fileName);
            
            _currentSaveFileContext.WriteToDisk();
        }

        
        #endregion

        #region OnRestoreState
        
        
        /// <summary>
        /// Loads and restores scene states from the active save file.
        /// </summary>
        public void LoadActiveScenes(LoadType loadType = LoadType.Hard)
        {
            Load(_activeSaveFile, loadType, _trackedSaveSceneManagers.Cast<ILoadableGroup>().ToArray());
        }

        /// <summary>
        /// Loads and restores scene states from the specified file.
        /// </summary>
        public void LoadActiveScenes(string fileName, LoadType loadType = LoadType.Hard)
        {
            Load(fileName, loadType, _trackedSaveSceneManagers.Cast<ILoadableGroup>().ToArray());
        }

        /// <summary>
        /// Loads from the active save file and restores the state of the provided loadable groups.
        /// </summary>
        public void Load(LoadType loadType = LoadType.Hard, params ILoadableGroup[] loadableGroups)
        {
            Load(_activeSaveFile, loadType, loadableGroups);
        }

        /// <summary>
        /// Loads from the specified file and restores the state of the provided loadable groups.
        /// </summary>
        public void Load(string fileName, LoadType loadType = LoadType.Hard, params ILoadableGroup[] loadableGroups)
        {
            UpdateSaveFileContext(fileName);
            
            _currentSaveFileContext.ReadFromDisk();
            _currentSaveFileContext.RestoreSnapshot(loadType, loadableGroups);
        }

        
        #endregion

        #region RestoreSnapshot
        
        
        /// <summary>
        /// Restores the in-memory snapshot for active scenes using the current load type.
        /// </summary>
        public void RestoreSnapshotActiveScenes(LoadType loadType = LoadType.Hard)
        {
            RestoreSnapshot(_activeSaveFile, loadType, _trackedSaveSceneManagers.Cast<ILoadableGroup>().ToArray());
        }

        /// <summary>
        /// Restores the in-memory snapshot from the specified file for active scenes.
        /// </summary>
        public void RestoreSnapshotActiveScenes(string fileName, LoadType loadType = LoadType.Hard)
        {
            RestoreSnapshot(fileName, loadType, _trackedSaveSceneManagers.Cast<ILoadableGroup>().ToArray());
        }

        /// <summary>
        /// Restores the in-memory snapshot for the given loadable groups using the active save file.
        /// </summary>
        public void RestoreSnapshot(LoadType loadType = LoadType.Hard, params ILoadableGroup[] loadableGroups)
        {
            RestoreSnapshot(_activeSaveFile, loadType, loadableGroups);
        }

        /// <summary>
        /// Restores the in-memory snapshot from the specified file for the given loadable groups.
        /// </summary>
        public void RestoreSnapshot(string fileName, LoadType loadType = LoadType.Hard, params ILoadableGroup[] loadableGroups)
        {
            UpdateSaveFileContext(fileName);
            
            _currentSaveFileContext.RestoreSnapshot(loadType, loadableGroups);
        }
        

        #endregion

        #region ReadFromDisk
        
        
        /// <summary>
        /// Reads snapshot data from the active save file and applies it to the in-memory snapshot.
        /// </summary>
        public void ReadFromDisk()
        {
            ReadFromDisk(_activeSaveFile);
        }

        /// <summary>
        /// Reads snapshot data from the specified file and applies it to the in-memory snapshot.
        /// </summary>
        public void ReadFromDisk(string fileName)
        {
            UpdateSaveFileContext(fileName);
            
            _currentSaveFileContext.ReadFromDisk();
        }

        
        #endregion

        #region DeleteSnapshotData

        
        /// <summary>
        /// Deletes in-memory snapshot data from the active fileName. Metadata is preserved.
        /// </summary>
        public void DeleteSnapshotData()
        {
            DeleteSnapshotData(_activeSaveFile);
        }

        /// <summary>
        /// Deletes in-memory snapshot data from the specified fileName. Metadata is preserved.
        /// </summary>
        public void DeleteSnapshotData(string fileName)
        {
            UpdateSaveFileContext(fileName);
            
            _currentSaveFileContext.DeleteSnapshotData();
        }

        
        #endregion

        #region DeleteDiskData

        
        /// <summary>
        /// Deletes all disk data from the active save file, including metadata.
        /// </summary>
        public void DeleteDiskData()
        {
            DeleteDiskData(_activeSaveFile);
        }

        /// <summary>
        /// Deletes all disk data from the specified file, including metadata.
        /// </summary>
        public void DeleteDiskData(string fileName)
        {
            UpdateSaveFileContext(fileName);
            
            _currentSaveFileContext.DeleteDiskData();
        }

        
        #endregion

        #region WipeAll

        
        /// <summary>
        /// Deletes all data (in-memory data and disk data) for the active save file.
        /// </summary>
        public void WipeAll()
        {
            UpdateSaveFileContext(_activeSaveFile);
        }

        /// <summary>
        /// Deletes all data (in-memory data and disk data) for the specified save file.
        /// </summary>
        public void WipeAll(string fileName)
        {
            UpdateSaveFileContext(fileName);
            
            _currentSaveFileContext.DeleteSnapshotData();
            _currentSaveFileContext.DeleteDiskData();
        }

        
        #endregion

        #region ReloadScenes

        
        /// <summary>
        /// Asynchronously reloads all currently tracked save scenes.
        /// </summary>
        public async Task ReloadActiveScenes()
        {
            await ReloadScenes(_trackedSaveSceneManagers.ToArray());
        }

        /// <summary>
        /// Asynchronously reloads a given set of scenes.
        /// </summary>
        public async Task ReloadScenes(params SimpleSceneSaveManager[] scenesToLoad)
        {
            //buffer save paths, because they will be null later on the scene array
            var sceneNamesToLoad = new string[scenesToLoad.Length];
            for (var index = 0; index < scenesToLoad.Length; index++)
            {
                sceneNamesToLoad[index] = scenesToLoad[index].SceneName;
            }

            //async reload scene and continue, when all are loaded
            var loadMode = SceneManager.sceneCount == 1 ? LoadSceneMode.Single : LoadSceneMode.Additive;
            if (SceneManager.sceneCount > 1)
            {
                var unloadTasks = sceneNamesToLoad.Select(UnloadSceneAsync).ToList();
                await Task.WhenAll(unloadTasks);
            }
                
            var loadTasks = sceneNamesToLoad.Select(name => LoadSceneAsync(name, loadMode)).ToList();
            await Task.WhenAll(loadTasks);
        }
        

        #endregion
        

        #region Internal

        
        internal void RegisterSaveSceneManager(SceneSaveManager sceneSaveManager)
        {
            _trackedSaveSceneManagers.Add(sceneSaveManager);
        }
        
        internal bool UnregisterSaveSceneManager(SceneSaveManager sceneSaveManager)
        {
            return _trackedSaveSceneManagers.Remove(sceneSaveManager);
        }
        
        internal List<SimpleSceneSaveManager> GetTrackedSaveSceneManagers()
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

        internal static SimpleSceneSaveManager GetDontDestroyOnLoadSceneManager()
        {
            if (!_dontDestroyOnLoadManager)
            {
                var newObject = new GameObject("DontDestroyOnLoadObject");
                DontDestroyOnLoad(newObject);
                _dontDestroyOnLoadManager = newObject.AddComponent<SimpleSceneSaveManager>();
            }
            
            return _dontDestroyOnLoadManager;
        }
        
        ICompressionStrategy ISaveStrategy.GetCompressionStrategy()
        {
            return compressionType switch
            {
                SaveCompressionType.None => new NoneCompressionSerializationStrategy(),
                SaveCompressionType.Gzip => new GzipCompressionSerializationStrategy(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        ISerializationStrategy ISaveStrategy.GetSerializationStrategy()
        {
            return new JsonSerializeStrategy(jsonFormatting, _jsonSerializerSettings);
        }

        IEncryptionStrategy ISaveStrategy.GetEncryptionStrategy()
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
        
        IIntegrityStrategy ISaveStrategy.GetIntegrityStrategy()
        {
            return integrityCheckType switch
            {
                SaveIntegrityType.None => null,
                SaveIntegrityType.Sha256Hashing => new Sha256HashingIntegrityStrategy(),
                SaveIntegrityType.CRC32 => new CRC32IntegrityStrategy(),
                SaveIntegrityType.Adler32 => new Adler32IntegrityStrategy(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        
        #endregion

        #region Private
        
        
        private void UpdateSaveFileContext(string fileName = null)
        {
            if (_currentSaveFileContext != null && _currentSaveFileContext.FileName == fileName) return;
            
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = _activeSaveFile;
                Debug.Log($"[SaveMate] {nameof(fileName)} missing: Swapped to the file with the name: {fileName}.");
            }
            
            ChangeSaveFileContext(new SaveFileContext(this, assetRegistry, fileName));
        }

        private void ReleaseSaveFileContext()
        {
            if (_currentSaveFileContext != null)
            {
                ChangeSaveFileContext(null);
            }
        }
        
        private void ChangeSaveFileContext(SaveFileContext newSaveFileContext)
        {
            SaveFileContext oldSaveFileContext = _currentSaveFileContext;
            
            OnBeforeSaveFileContextChange?.Invoke(oldSaveFileContext, newSaveFileContext);

            _currentSaveFileContext = newSaveFileContext;
            
            OnAfterSaveFileContextChange?.Invoke(oldSaveFileContext, newSaveFileContext);
        }
        
        private Task<AsyncOperation> UnloadSceneAsync(string sceneName)
        {
            AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
            TaskCompletionSource<AsyncOperation> tcs = new TaskCompletionSource<AsyncOperation>();

            if (asyncOperation != null)
            {
                asyncOperation.allowSceneActivation = true;
                asyncOperation.completed += operation =>
                {
                    tcs.SetResult(operation);
                };
            }
            else
            {
                tcs.SetResult(null);
            }
            
            // Return the task
            return tcs.Task;
        }
        
        private Task<AsyncOperation> LoadSceneAsync(string scenePath, LoadSceneMode loadSceneMode)
        {
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(scenePath, loadSceneMode);
            TaskCompletionSource<AsyncOperation> tcs = new TaskCompletionSource<AsyncOperation>();

            if (asyncOperation != null)
            {
                asyncOperation.allowSceneActivation = true;
                asyncOperation.completed += operation =>
                {
                    tcs.SetResult(operation);
                };
            }
            else
            {
                tcs.SetResult(null);
            }
            
            // Return the task
            return tcs.Task;
        }
        

        #endregion
    }
}
