using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.EventHandler;
using SaveLoadSystem.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace SaveLoadSystem.Core
{
    public enum LoadType { Hard, Soft }
    
    public class SaveFileContext
    {
        public string FileName { get; }
        public bool HasPendingData { get; private set; }
        public bool IsPersistent { get; private set; }

        private readonly SaveLoadManager _saveLoadManager;
        private readonly AssetRegistry _assetRegistry;
        private readonly AsyncOperationQueue _asyncQueue;
        
        private JObject _customMetaData;
        private SaveMetaData _metaData;
        
        internal RootSaveData RootSaveData;
        internal GuidToCreatedNonUnityObjectLookup GuidToCreatedNonUnityObjectLookup;
        internal ConditionalWeakTable<object, string> SavedNonUnityObjectToGuidLookup;
        internal HashSet<object> SoftLoadedObjects;
        
        
        public SaveFileContext(SaveLoadManager saveLoadManager, AssetRegistry assetRegistry, string fileName)
        {
            _saveLoadManager = saveLoadManager;
            _assetRegistry = assetRegistry;
            _asyncQueue = new AsyncOperationQueue();
            FileName = fileName;

            Initialize();
        }

        private void Initialize()
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (SaveLoadUtility.MetaDataExists(_saveLoadManager, FileName) && SaveLoadUtility.SaveDataExists(_saveLoadManager, FileName))
                {
                    _metaData = await SaveLoadUtility.ReadMetaDataAsync(_saveLoadManager, _saveLoadManager, FileName);
                    IsPersistent = true;
                    _customMetaData = _metaData.CustomData;
                }
                else
                {
                    _customMetaData = new();
                }
            });
        }


        #region Public Methods
        
        
        internal void SaveCustomMetaData(string identifier, object data)
        {
            _asyncQueue.Enqueue(() =>
            {
                _customMetaData.Add(identifier, JToken.FromObject(data));
                return Task.CompletedTask;
            });
        }
        
        internal bool TryLoadCustomMetaData<T>(string identifier, out T obj)
        {
            obj = default;

            if (_customMetaData == null)
            {
                Debug.LogError($"{nameof(SaveFileContext)} not Initialized!");
            }
            
            if (_customMetaData[identifier] == null)
            {
                Debug.LogError($"Wasn't able to find the object of type '{typeof(T).FullName}' for identifier '{identifier}' inside the meta data!");
                return false;
            }

            try
            {
                obj = _customMetaData[identifier].ToObject<T>();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error serializing object of type '{typeof(T).FullName}' using '{nameof(Newtonsoft.Json)}'. " +
                               $"Exception: {e.GetType().Name} - {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        internal void CaptureSnapshot(params ISavableGroup[] savableGroups)
        {
            _asyncQueue.Enqueue(() =>
            {
                RootSaveData ??= new RootSaveData();
                GuidToCreatedNonUnityObjectLookup ??= new GuidToCreatedNonUnityObjectLookup();
                SavedNonUnityObjectToGuidLookup ??= new ConditionalWeakTable<object, string>();
                SoftLoadedObjects ??= new HashSet<object>();
                
                //convert into required handlers
                var combinedSavableGroupHandlers = new List<ISavableGroupHandler>();
                foreach (var savableGroup in savableGroups)
                {
                    if (savableGroup is not ISavableGroupHandler saveGroupHandler) continue;
                    
                    if (savableGroup is INestableSaveGroupHandler nestableSaveGroupHandler)
                    {
                        combinedSavableGroupHandlers.AddRange(nestableSaveGroupHandler.GetSavableGroupHandlers());
                    }
                    combinedSavableGroupHandlers.Add(saveGroupHandler);
                }
                
                InternalCaptureSnapshot(combinedSavableGroupHandlers);
                
                return Task.CompletedTask;
            });
        }
        
        internal void WriteToDisk()
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (!HasPendingData)
                {
                    Debug.LogWarning($"[Save Mate] {nameof(WriteToDisk)} aborted: No pending data to save!");
                    return;
                }
                
                _metaData = new SaveMetaData()
                {
                    SaveVersion = _saveLoadManager.SaveVersion,
                    ModificationDate = DateTime.Now,
                    CustomData = _customMetaData
                };

                OnBeforeWriteToDisk();
                
                await SaveLoadUtility.WriteDataAsync(_saveLoadManager, _saveLoadManager, FileName, _metaData, RootSaveData);

                IsPersistent = true;
                HasPendingData = false;

                OnAfterWriteToDisk();
                
                Debug.Log($"[Save Mate] {nameof(WriteToDisk)} successful!");
            });
        }
        
        internal void ReadFromDisk()
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (!SaveLoadUtility.SaveDataExists(_saveLoadManager, FileName) 
                    || !SaveLoadUtility.MetaDataExists(_saveLoadManager, FileName)) return;
                
                //only load saveData, if it is persistent and not initialized
                if (IsPersistent && RootSaveData == null)
                {
                    OnBeforeReadFromDisk();
                    
                    RootSaveData = await SaveLoadUtility.ReadSaveDataSecureAsync(_saveLoadManager, _saveLoadManager.SaveVersion, _saveLoadManager, FileName);
                    GuidToCreatedNonUnityObjectLookup = new GuidToCreatedNonUnityObjectLookup();
                    SavedNonUnityObjectToGuidLookup = new ConditionalWeakTable<object, string>();
                    SoftLoadedObjects = new HashSet<object>();
                    
                    OnAfterReadFromDisk();
                    
                    Debug.Log($"[Save Mate] {nameof(ReadFromDisk)} successful!");
                }
                else
                {
                    Debug.LogWarning($"[Save Mate] '{nameof(ReadFromDisk)}' aborted: No persistent save data found on disk.");
                }
            });
        }

        internal void RestoreSnapshot(LoadType loadType, params ILoadableGroup[] loadableGroups)
        {
            _asyncQueue.Enqueue(() =>
            {
                if (RootSaveData == null)
                {
                    Debug.LogWarning($"[Save Mate] {nameof(RestoreSnapshot)} aborted: No save data found in memory. Ensure '{nameof(ReadFromDisk)}' was called before attempting to load.");
                    return Task.CompletedTask;
                }

                //convert into required handlers
                var combinedLoadGroupHandlers = new List<ILoadableGroupHandler>();
                foreach (var loadableGroup in loadableGroups)
                {
                    if (loadableGroup is not ILoadableGroupHandler loadableGroupHandler) continue;
                    
                    if (loadableGroupHandler.SceneName != "DontDestroyOnLoad" && !SceneManager.GetSceneByName(loadableGroupHandler.SceneName).isLoaded)
                    {
                        Debug.LogWarning($"[Save Mate] Skipped '{nameof(RestoreSnapshot)}' for scene '{loadableGroupHandler.SceneName}': scene is not currently loaded.");
                        continue;
                    }
                    
                    if (loadableGroup is INestableLoadGroupHandler getRestoreSnapshotHandler)
                    {
                        combinedLoadGroupHandlers.AddRange(getRestoreSnapshotHandler.GetLoadableGroupHandlers());
                    }
                    combinedLoadGroupHandlers.Add(loadableGroupHandler);
                }
                
                InternalRestoreSnapshot(loadType, combinedLoadGroupHandlers);
                return Task.CompletedTask;
            });
        }

        internal void DeleteSnapshotData()
        {
            _asyncQueue.Enqueue(() =>
            {
                if (RootSaveData == null) return Task.CompletedTask;
                
                OnBeforeDeleteSnapshotData();

                RootSaveData.Clear();
                GuidToCreatedNonUnityObjectLookup.CLear();
                SavedNonUnityObjectToGuidLookup.Clear();
                SoftLoadedObjects.Clear();
                HasPendingData = true;
                
                OnAfterDeleteSnapshotData();

                return Task.CompletedTask;
            });
        }
        
        internal void DeleteDiskData()
        {
            _asyncQueue.Enqueue(async () =>
            {
                OnBeforeDeleteDiskData();

                await SaveLoadUtility.DeleteAsync(_saveLoadManager, FileName);

                IsPersistent = false;

                OnAfterDeleteDiskData();
                
                Debug.Log("Delete Completed!");
            });
        }
        
        
        #endregion

        #region Private Events

        private void OnBeforeCaptureSnapshot(List<ISavableGroupHandler> savableGroupHandlers)
        {
            foreach (var savableGroupHandler in savableGroupHandlers)
            {
                savableGroupHandler.OnBeforeCaptureSnapshot();
            }
        }
        
        private void OnAfterCaptureSnapshot(List<ISavableGroupHandler> savableGroupHandlers)
        {
            foreach (var savableGroupHandler in savableGroupHandlers)
            {
                savableGroupHandler.OnAfterCaptureSnapshot();
            }
        }
        
        private void OnBeforeWriteToDisk()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is IBeforeWriteToDiskHandler eventHandler)
                {
                    eventHandler.OnBeforeWriteToDisk();
                }
            }

            foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                trackedSaveSceneManager.OnBeforeWriteToDisk();
            }
        }

        private void OnAfterWriteToDisk()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is IAfterWriteToDiskHandler eventHandler)
                {
                    eventHandler.OnAfterWriteToDisk();
                }
            }
            
            foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                trackedSaveSceneManager.OnAfterWriteToDisk();
            }
        }
        
        private void OnBeforeReadFromDisk()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is IBeforeReadFromDiskHandler eventHandler)
                {
                    eventHandler.OnBeforeReadFromDisk();
                }
            }

            foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                trackedSaveSceneManager.OnBeforeReadFromDisk();
            }
        }

        private void OnAfterReadFromDisk()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is IAfterReadFromDiskHandler eventHandler)
                {
                    eventHandler.OnAfterReadFromDisk();
                }
            }
            
            foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                trackedSaveSceneManager.OnAfterReadFromDisk();
            }
        }
        
        private void OnBeforeRestoreSnapshot(List<ILoadableGroupHandler> loadableGroupHandlers)
        {
            foreach (var loadableGroupHandler in loadableGroupHandlers)
            {
                loadableGroupHandler.OnBeforeRestoreSnapshot();
            }
        }
        
        private void OnAfterRestoreSnapshot(List<ILoadableGroupHandler> loadableGroupHandlers)
        {
            foreach (var loadableGroupHandler in loadableGroupHandlers)
            {
                loadableGroupHandler.OnAfterRestoreSnapshot();
            }
        }

        private void OnBeforeDeleteSnapshotData()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is IBeforeDeleteSnapshotData eventHandler)
                {
                    eventHandler.OnBeforeDeleteSnapshotData();
                }
            }
            
            foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                trackedSaveSceneManager.OnBeforeDeleteSnapshotData();
            }
        }

        private void OnAfterDeleteSnapshotData()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is IAfterDeleteSnapshotData eventHandler)
                {
                    eventHandler.OnAfterDeleteSnapshotData();
                }
            }
            
            foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                trackedSaveSceneManager.OnAfterDeleteSnapshotData();
            }
        }

        private void OnBeforeDeleteDiskData()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is IBeforeDeleteDiskHandler eventHandler)
                {
                    eventHandler.OnBeforeDeleteDiskData();
                }
            }
            
            foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                trackedSaveSceneManager.OnBeforeDeleteDiskData();
            }
        }
        
        private void OnAfterDeleteDiskData()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is IAfterDeleteDiskHandler eventHandler)
                {
                    eventHandler.OnAfterDeleteDiskData();
                }
            }
            
            foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                trackedSaveSceneManager.OnAfterDeleteDiskData();
            }
        }

        
        #endregion

        #region Private Snapshots

        
        private void InternalCaptureSnapshot(List<ISavableGroupHandler> savableGroupHandlers)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            OnBeforeCaptureSnapshot(savableGroupHandlers);

            foreach (var savableGroupHandler in savableGroupHandlers)
            {
                savableGroupHandler.CaptureSnapshot(_saveLoadManager, this);
            }
            HasPendingData = true;
            
            OnAfterCaptureSnapshot(savableGroupHandlers);
            
            stopwatch.Stop();
            if (savableGroupHandlers.Count == 0)
            {
                Debug.Log($"Performed Snapshotting for no scene!");
            }
            else if (savableGroupHandlers.Count == 1)
            {
                Debug.Log($"Snapshotting Completed for scene {savableGroupHandlers[0].SceneName}! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
            else
            {
                Debug.Log($"Snapshotting Completed for {savableGroupHandlers.Count} scenes! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
        }
        
        private void InternalRestoreSnapshot(LoadType loadType, List<ILoadableGroupHandler> loadableGroupHandlers)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            OnBeforeRestoreSnapshot(loadableGroupHandlers);
            
            GuidToCreatedNonUnityObjectLookup.PrepareLoading();
            
            foreach (var loadableGroupHandler in loadableGroupHandlers)
            {
                loadableGroupHandler.OnPrepareSnapshotObjects(_saveLoadManager, this, loadType);
            }
            
            foreach (var loadableGroupHandler in loadableGroupHandlers)
            {
                loadableGroupHandler.RestoreSnapshot(_saveLoadManager, this, loadType);
            }
            
            GuidToCreatedNonUnityObjectLookup.CompleteLoading();
            
            OnAfterRestoreSnapshot(loadableGroupHandlers);
            
            stopwatch.Stop();
            if (loadableGroupHandlers.Count == 0)
            {
                Debug.Log($"Performed Loading for no scene!");
            }
            else if (loadableGroupHandlers.Count == 1)
            {
                Debug.Log($"Loading Completed for scene {loadableGroupHandlers[0].SceneName}! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
            else
            {
                Debug.Log($"Loading Completed for {loadableGroupHandlers.Count} scenes! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        
        #endregion

        #region Private Scene Loading

        
        //TODO: test
        internal async void ReloadScenes(params SimpleSceneSaveManager[] scenesToLoad)
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

        #region Private Classes

        
        private class AsyncOperationQueue
        {
            private readonly SemaphoreSlim _semaphore = new(1, 1);
            private readonly Queue<Func<Task>> _queue = new();

            // Enqueue a task to be executed
            public void Enqueue(Func<Task> task)
            {
                _queue.Enqueue(task);

                if (_queue.Count == 1)
                {
                    ProcessQueue();
                }
            }

            // Process the queue
            private async void ProcessQueue()
            {
                await _semaphore.WaitAsync();

                try
                {
                    while (_queue.Count > 0)
                    {
                        var task = _queue.Dequeue();
                        await task();
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        
        #endregion
    }
}
