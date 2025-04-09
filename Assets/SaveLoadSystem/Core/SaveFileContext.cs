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
    
    //TODO: implement a system to add things similar to the PlayerPrefs, but with reference support
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
        
        
        public void SetCustomMetaData(string identifier, object data)
        {
            _asyncQueue.Enqueue(() =>
            {
                _customMetaData.Add(identifier, JToken.FromObject(data));
                return Task.CompletedTask;
            });
        }

        public T GetCustomMetaData<T>(string identifier)
        {
            if (_customMetaData?[identifier] == null)
            {
                return default;
            }
            
            return _customMetaData[identifier].ToObject<T>();
        }
        
        public void SaveActiveScenes()
        {
            CaptureSnapshotForActiveScenes();
            WriteToDisk();
        }

        public void Save(params ISavableGroup[] savableGroups)
        {
            CaptureSnapshot(savableGroups.ToArray());
            WriteToDisk();
        }
        
        public void CaptureSnapshotForActiveScenes()
        {
            CaptureSnapshot(_saveLoadManager.GetTrackedSaveSceneManagers().Cast<ISavableGroup>().ToArray());
        }

        public void CaptureSnapshot(params ISavableGroup[] savableGroups)
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
        
        public void WriteToDisk()
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (!HasPendingData)
                {
                    Debug.LogWarning("Aborted attempt to save without pending data!");
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
                
                Debug.Log("Write to disk completed!");
            });
        }
        
        public void LoadActiveScenes(LoadType loadType)
        {
            ReadFromDisk();
            RestoreSnapshotForActiveScenes(loadType);
        }
        
        public void Load(LoadType loadType, params ILoadableGroup[] loadableGroups)
        {
            ReadFromDisk();
            RestoreSnapshot(loadType, loadableGroups);
        }

        public void RestoreSnapshotForActiveScenes(LoadType loadType)
        {
            RestoreSnapshot(loadType, _saveLoadManager.GetTrackedSaveSceneManagers().Cast<ILoadableGroup>().ToArray());
        }

        public void RestoreSnapshot(LoadType loadType, params ILoadableGroup[] loadableGroups)
        {
            _asyncQueue.Enqueue(() =>
            {
                if (RootSaveData == null) return Task.CompletedTask;

                //convert into required handlers
                var combinedLoadGroupHandlers = new List<ILoadableGroupHandler>();
                foreach (var loadableGroup in loadableGroups)
                {
                    if (loadableGroup is not ILoadableGroupHandler loadableGroupHandler) continue;
                    
                    if (loadableGroupHandler.SceneName != "DontDestroyOnLoad" && !SceneManager.GetSceneByName(loadableGroupHandler.SceneName).isLoaded)
                    {
                        Debug.LogWarning($"Tried to apply a snapshot to the unloaded scene '{loadableGroupHandler.SceneName}'");
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

        public void ReadFromDisk()
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (!SaveLoadUtility.SaveDataExists(_saveLoadManager, FileName) 
                    || !SaveLoadUtility.MetaDataExists(_saveLoadManager, FileName)) return;
                
                //only load saveData, if it is persistent and not initialized
                if (IsPersistent && RootSaveData == null)
                {
                    RootSaveData = await SaveLoadUtility.ReadSaveDataSecureAsync(_saveLoadManager, _saveLoadManager.SaveVersion, _saveLoadManager, FileName);
                    GuidToCreatedNonUnityObjectLookup = new GuidToCreatedNonUnityObjectLookup();
                    SavedNonUnityObjectToGuidLookup = new ConditionalWeakTable<object, string>();
                    SoftLoadedObjects = new HashSet<object>();
                    
                    Debug.Log("Read from disk completed");
                }
            });
        }
        
        public void ReloadScenes()
        {
            ReloadScenes(_saveLoadManager.GetTrackedSaveSceneManagers().ToArray());
        }

        /// <summary>
        /// data will be applied after awake
        /// </summary>
        /// <param name="loadType"></param>
        /// <param name="scenesToLoad"></param>
        public void ReloadScenes(params SimpleSceneSaveManager[] scenesToLoad)
        {
            _asyncQueue.Enqueue(async () =>
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
            });
        }
        
        //TODO: rework deletion (savable groups)
        public void DeleteActiveSceneSnapshotData()
        {
            DeleteSnapshotData(_saveLoadManager.GetTrackedSaveSceneManagers().ToArray());
        }

        //TODO: rework deletion (savable groups)
        public void DeleteSnapshotData(params SimpleSceneSaveManager[] saveSceneManagersToWipe)
        {
            _asyncQueue.Enqueue(() =>
            {
                if (RootSaveData == null) return Task.CompletedTask;

                foreach (var saveSceneManager in saveSceneManagersToWipe)
                {
                    RootSaveData.RemoveSceneData(saveSceneManager.SceneName);
                }
                HasPendingData = true;

                return Task.CompletedTask;
            });
        }
        
        //TODO: rework deletion (savable groups)
        public void Delete(params SimpleSceneSaveManager[] saveSceneManagersToWipe)
        {
            foreach (var saveSceneManager in saveSceneManagersToWipe)
            {
                DeleteSnapshotData(saveSceneManager);
            }

            DeleteDiskData();
        }
        
        public void DeleteDiskData()
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
        
        
        private void OnBeforeWriteToDisk()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is ISaveMateBeforeWriteDiskHandler eventHandler)
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
                if (scriptableObjectSavable.unityObject is ISaveMateAfterWriteDiskHandler eventHandler)
                {
                    eventHandler.OnAfterWriteToDisk();
                }
            }
            
            foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
            {
                trackedSaveSceneManager.OnAfterWriteToDisk();
            }
        }

        private void OnBeforeDeleteDiskData()
        {
            foreach (var scriptableObjectSavable in _assetRegistry.ScriptableObjectSavables)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (scriptableObjectSavable.unityObject is ISaveMateBeforeDeleteDiskHandler eventHandler)
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
                if (scriptableObjectSavable.unityObject is ISaveMateAfterDeleteDiskHandler eventHandler)
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
            
            //on before capture snapshot event
            foreach (var savableGroupHandler in savableGroupHandlers)
            {
                savableGroupHandler.OnBeforeCaptureSnapshot();
            }

            //perform capture snapshot
            foreach (var savableGroupHandler in savableGroupHandlers)
            {
                savableGroupHandler.CaptureSnapshot(_saveLoadManager);
            }
            HasPendingData = true;
            
            //on after capture snapshot event
            foreach (var savableGroupHandler in savableGroupHandlers)
            {
                savableGroupHandler.OnAfterCaptureSnapshot();
            }
            
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
            
            //on before restore snapshot event
            foreach (var loadableGroupHandler in loadableGroupHandlers)
            {
                loadableGroupHandler.OnBeforeRestoreSnapshot();
            }
            
            //perform restore snapshot
            GuidToCreatedNonUnityObjectLookup.PrepareLoading();
            foreach (var loadableGroupHandler in loadableGroupHandlers)
            {
                loadableGroupHandler.OnPrepareSnapshotObjects(_saveLoadManager, loadType);
            }
            
            foreach (var loadableGroupHandler in loadableGroupHandlers)
            {
                loadableGroupHandler.RestoreSnapshot(_saveLoadManager, loadType);
            }
            GuidToCreatedNonUnityObjectLookup.CompleteLoading();
            
            //on after restore snapshot event
            foreach (var loadableGroupHandler in loadableGroupHandlers)
            {
                loadableGroupHandler.OnAfterRestoreSnapshot();
            }
            
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
