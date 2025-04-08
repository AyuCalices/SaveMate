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
        private readonly AsyncOperationQueue _asyncQueue;
        
        private JObject _customMetaData;
        private SaveMetaData _metaData;
        
        internal RootSaveData RootSaveData;
        internal GuidToCreatedNonUnityObjectLookup GuidToCreatedNonUnityObjectLookup;
        internal ConditionalWeakTable<object, string> SavedNonUnityObjectToGuidLookup;
        internal HashSet<object> SoftLoadedObjects;
        
        
        public SaveFileContext(SaveLoadManager saveLoadManager, string fileName)
        {
            _asyncQueue = new AsyncOperationQueue();
            _saveLoadManager = saveLoadManager;
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

        public void Save(params ICaptureSnapshotGroupElement[] saveSceneManagersToSave)
        {
            CaptureSnapshot(saveSceneManagersToSave.ToArray());
            WriteToDisk();
        }
        
        public void CaptureSnapshotForActiveScenes()
        {
            CaptureSnapshot(_saveLoadManager.GetTrackedSaveSceneManagers().Cast<ICaptureSnapshotGroupElement>().ToArray());
        }

        public void CaptureSnapshot(params ICaptureSnapshotGroupElement[] captureSnapshotGroupElements)
        {
            _asyncQueue.Enqueue(() =>
            {
                RootSaveData ??= new RootSaveData();
                GuidToCreatedNonUnityObjectLookup ??= new GuidToCreatedNonUnityObjectLookup();
                SavedNonUnityObjectToGuidLookup ??= new ConditionalWeakTable<object, string>();
                SoftLoadedObjects ??= new HashSet<object>();
                
                var combinedCaptureGroupSnapshots = new List<ICaptureSnapshotGroupElement>();
                foreach (var captureSnapshotGroupElement in captureSnapshotGroupElements)
                {
                    if (captureSnapshotGroupElement.SceneName != "DontDestroyOnLoad" && !SceneManager.GetSceneByName(captureSnapshotGroupElement.SceneName).isLoaded)
                    {
                        Debug.LogWarning($"Tried to create a snapshot to the unloaded scene '{captureSnapshotGroupElement.SceneName}'");
                    }
                    
                    combinedCaptureGroupSnapshots.Add(captureSnapshotGroupElement);
                    if (captureSnapshotGroupElement is IGetCaptureSnapshotGroupElementHandler getRestoreSnapshotHandler)
                    {
                        combinedCaptureGroupSnapshots.AddRange(getRestoreSnapshotHandler.GetCaptureSnapshotGroupElements());
                    }
                }
                
                InternalCaptureSnapshot(combinedCaptureGroupSnapshots);
                
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
                
                foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
                {
                    trackedSaveSceneManager.OnBeforeWriteToDisk();
                }
                
                await SaveLoadUtility.WriteDataAsync(_saveLoadManager, _saveLoadManager, FileName, _metaData, RootSaveData);

                IsPersistent = true;
                HasPendingData = false;
                
                foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
                {
                    trackedSaveSceneManager.OnAfterWriteToDisk();
                }
                
                Debug.Log("Write to disk completed!");
            });
        }
        
        public void LoadActiveScenes(LoadType loadType)
        {
            ReadFromDisk();
            RestoreSnapshotForActiveScenes(loadType);
        }
        
        public void Load(LoadType loadType, params IRestoreSnapshotGroupElement[] saveSceneManagersToLoad)
        {
            ReadFromDisk();
            RestoreSnapshot(loadType, saveSceneManagersToLoad);
        }

        public void RestoreSnapshotForActiveScenes(LoadType loadType)
        {
            RestoreSnapshot(loadType, _saveLoadManager.GetTrackedSaveSceneManagers().Cast<IRestoreSnapshotGroupElement>().ToArray());
        }

        public void RestoreSnapshot(LoadType loadType, params IRestoreSnapshotGroupElement[] restoreSnapshotGroupElements)
        {
            _asyncQueue.Enqueue(() =>
            {
                if (RootSaveData == null) return Task.CompletedTask;

                var combinedRestoreGroupSnapshots = new List<IRestoreSnapshotGroupElement>();
                foreach (var restoreSnapshotGroupElement in restoreSnapshotGroupElements)
                {
                    if (restoreSnapshotGroupElement.SceneName != "DontDestroyOnLoad" && !SceneManager.GetSceneByName(restoreSnapshotGroupElement.SceneName).isLoaded)
                    {
                        Debug.LogWarning($"Tried to apply a snapshot to the unloaded scene '{restoreSnapshotGroupElement.SceneName}'");
                    }
                    
                    combinedRestoreGroupSnapshots.Add(restoreSnapshotGroupElement);
                    if (restoreSnapshotGroupElement is IGetRestoreSnapshotGroupElementHandler getRestoreSnapshotHandler)
                    {
                        combinedRestoreGroupSnapshots.AddRange(getRestoreSnapshotHandler.GetRestoreSnapshotGroupElements());
                    }
                }
                
                InternalRestoreSnapshot(loadType, combinedRestoreGroupSnapshots);
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
        
        public void DeleteActiveSceneSnapshotData()
        {
            DeleteSnapshotData(_saveLoadManager.GetTrackedSaveSceneManagers().ToArray());
        }

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
                foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
                {
                    trackedSaveSceneManager.OnBeforeDeleteDiskData();
                }

                await SaveLoadUtility.DeleteAsync(_saveLoadManager, FileName);

                IsPersistent = false;
                
                foreach (var trackedSaveSceneManager in _saveLoadManager.GetTrackedSaveSceneManagers())
                {
                    trackedSaveSceneManager.OnAfterDeleteDiskData();
                }
                
                Debug.Log("Delete Completed!");
            });
        }

        #region Private Methods

        private void InternalCaptureSnapshot(List<ICaptureSnapshotGroupElement> saveGroupElements)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            foreach (var saveGroupElement in saveGroupElements)
            {
                if (saveGroupElement is IBeforeCaptureSnapshotHandler eventHandler)
                {
                    eventHandler.OnBeforeCaptureSnapshot();
                }
            }

            foreach (var saveGroupElement in saveGroupElements)
            {
                saveGroupElement.CaptureSnapshot(_saveLoadManager);
            }
            
            HasPendingData = true;
            
            foreach (var saveSceneManager in saveGroupElements)
            {
                if (saveSceneManager is IAfterCaptureSnapshotHandler eventHandler)
                {
                    eventHandler.OnAfterCaptureSnapshot();
                }
            }
            
            stopwatch.Stop();
            if (saveGroupElements.Count == 0)
            {
                Debug.Log($"Performed Snapshotting for no scene!");
            }
            else if (saveGroupElements.Count == 1)
            {
                Debug.Log($"Snapshotting Completed for scene {saveGroupElements[0].SceneName}! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
            else
            {
                Debug.Log($"Snapshotting Completed for {saveGroupElements.Count} scenes! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
        }
        
        private void InternalRestoreSnapshot(LoadType loadType, List<IRestoreSnapshotGroupElement> loadGroupElements)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            //on before load event
            foreach (var loadGroupElement in loadGroupElements)
            {
                if (loadGroupElement is ISaveMateBeforeLoadHandler eventHandler)
                {
                    eventHandler.OnBeforeRestoreSnapshot();
                }
            }
            
            GuidToCreatedNonUnityObjectLookup.PrepareLoading();
            
            foreach (var loadGroupElement in loadGroupElements)
            {
                loadGroupElement.OnPrepareSnapshotObjects(_saveLoadManager, loadType);
            }
            
            foreach (var loadGroupElement in loadGroupElements)
            {
                loadGroupElement.RestoreSnapshot(_saveLoadManager, loadType);
            }
            
            GuidToCreatedNonUnityObjectLookup.CompleteLoading();
            
            //on load completed event
            foreach (var loadGroupElement in loadGroupElements)
            {
                if (loadGroupElement is ISaveMateAfterLoadHandler eventHandler)
                {
                    eventHandler.OnAfterRestoreSnapshot();
                }
            }
            
            stopwatch.Stop();
            if (loadGroupElements.Count == 0)
            {
                Debug.Log($"Performed Loading for no scene!");
            }
            else if (loadGroupElements.Count == 1)
            {
                Debug.Log($"Loading Completed for scene {loadGroupElements[0].SceneName}! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
            else
            {
                Debug.Log($"Loading Completed for {loadGroupElements.Count} scenes! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
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

    public class AsyncOperationQueue
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Queue<Func<Task>> _queue = new Queue<Func<Task>>();

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
}
