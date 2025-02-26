using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.UnityComponent;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using TypeUtility = SaveLoadSystem.Utility.TypeUtility;

namespace SaveLoadSystem.Core
{
    public class SaveFocus
    {
        public string FileName { get; }
        public bool HasPendingData { get; private set; }
        public bool IsPersistent { get; private set; }

        private readonly SaveLoadManager _saveLoadManager;
        private readonly AsyncOperationQueue _asyncQueue;
        
        private JObject _customMetaData;
        private SaveMetaData _metaData;
        private SaveData _saveData;

        public SaveFocus(SaveLoadManager saveLoadManager, string fileName)
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
        
        public void SnapshotActiveScenes()
        {
            SnapshotScenes(_saveLoadManager.TrackedSaveSceneManagers.ToArray());
        }

        public void SnapshotScenes(params SaveSceneManager[] scenesToSnapshot)
        {
            _asyncQueue.Enqueue(() =>
            {
                _saveData ??= new SaveData();
                HasPendingData = true;
                InternalSnapshotActiveScenes(_saveData, scenesToSnapshot);
                
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
                
                foreach (var trackedSaveSceneManager in _saveLoadManager.TrackedSaveSceneManagers)
                {
                    trackedSaveSceneManager.HandleBeforeWriteToDisk();
                }
                
                await SaveLoadUtility.WriteDataAsync(_saveLoadManager, _saveLoadManager, FileName, _metaData, _saveData);

                IsPersistent = true;
                HasPendingData = false;
                
                foreach (var trackedSaveSceneManager in _saveLoadManager.TrackedSaveSceneManagers)
                {
                    trackedSaveSceneManager.HandleAfterWriteToDisk();
                }
                
                Debug.LogWarning("Save Completed!");
            });
        }
        
        public void SaveActiveScenes()
        {
            SnapshotActiveScenes();
            WriteToDisk();
        }

        public void SaveScenes(params SaveSceneManager[] saveSceneManagersToSave)
        {
            SnapshotScenes(saveSceneManagersToSave);
            WriteToDisk();
        }

        public void ApplySnapshotToActiveScenes()
        {
            ApplySnapshotToScenes(_saveLoadManager.TrackedSaveSceneManagers.ToArray());
        }

        public void ApplySnapshotToScenes(params SaveSceneManager[] saveSceneManagersToApplySnapshot)
        {
            _asyncQueue.Enqueue(() =>
            {
                if (_saveData == null) return Task.CompletedTask;
                
                InternalLoadActiveScenes(_saveData, saveSceneManagersToApplySnapshot);
                return Task.CompletedTask;
            });
        }
        
        public void LoadActiveScenes()
        {
            ReadFromDisk();
            ApplySnapshotToActiveScenes();
        }
        
        public void LoadScenes(params SaveSceneManager[] saveSceneManagersToLoad)
        {
            ReadFromDisk();
            ApplySnapshotToScenes(saveSceneManagersToLoad);
        }

        public void ReadFromDisk()
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (!SaveLoadUtility.SaveDataExists(_saveLoadManager, FileName) 
                    || !SaveLoadUtility.MetaDataExists(_saveLoadManager, FileName)) return;
                
                //only load saveData, if it is persistent and not initialized
                if (IsPersistent && _saveData == null)
                {
                    _saveData = await SaveLoadUtility.ReadSaveDataSecureAsync(_saveLoadManager, _saveLoadManager.SaveVersion, _saveLoadManager, FileName);
                }
            });
        }
        
        public void ReloadThenLoadActiveScenes()
        {
            ReloadThenLoadScenes(_saveLoadManager.TrackedSaveSceneManagers.ToArray());
        }
        
        /// <summary>
        /// data will be applied after awake
        /// </summary>
        /// <param name="scenesToLoad"></param>
        public void ReloadThenLoadScenes(params SaveSceneManager[] scenesToLoad)
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (!SaveLoadUtility.SaveDataExists(_saveLoadManager, FileName) 
                    || !SaveLoadUtility.MetaDataExists(_saveLoadManager, FileName)) return;
                
                //only load saveData, if it is persistent and not initialized
                if (IsPersistent && _saveData == null)
                {
                    _saveData = await SaveLoadUtility.ReadSaveDataSecureAsync(_saveLoadManager, _saveLoadManager.SaveVersion, _saveLoadManager, FileName);
                }

                //buffer save paths, because they will be null later on the scene array
                var savePaths = new string[scenesToLoad.Length];
                for (var index = 0; index < scenesToLoad.Length; index++)
                {
                    savePaths[index] = scenesToLoad[index].Scene.path;
                }

                //async reload scene and continue, when all are loaded
                var loadTasks = scenesToLoad.Select(saveSceneManager => LoadSceneAsync(saveSceneManager.Scene.path)).ToList();
                await Task.WhenAll(loadTasks);

                //remap save paths to the reloaded scenes
                List<SaveSceneManager> matchingGuids = new();
                foreach (var saveSceneManager in _saveLoadManager.TrackedSaveSceneManagers)
                {
                    if (savePaths.Contains(saveSceneManager.Scene.path))
                    {
                        matchingGuids.Add(saveSceneManager);
                    }
                }
                
                InternalLoadActiveScenes(_saveData, matchingGuids.ToArray());
            });
        }
        
        public void DeleteActiveSceneData()
        {
            DeleteSceneData(_saveLoadManager.TrackedSaveSceneManagers.ToArray());
        }

        public void DeleteSceneData(params SaveSceneManager[] saveSceneManagersToWipe)
        {
            _asyncQueue.Enqueue(() =>
            {
                if (_saveData == null) return Task.CompletedTask;

                foreach (var saveSceneManager in saveSceneManagersToWipe)
                {
                    _saveData.RemoveSceneData(saveSceneManager.Scene);
                }
                HasPendingData = true;

                return Task.CompletedTask;
            });
        }
        
        public void DeleteAll(params SaveSceneManager[] saveSceneManagersToWipe)
        {
            foreach (var saveSceneManager in saveSceneManagersToWipe)
            {
                DeleteSceneData(saveSceneManager);
            }

            DeleteDiskData();
        }
        
        public void DeleteDiskData()
        {
            _asyncQueue.Enqueue(async () =>
            {
                foreach (var trackedSaveSceneManager in _saveLoadManager.TrackedSaveSceneManagers)
                {
                    trackedSaveSceneManager.HandleBeforeDeleteDiskData();
                }

                await SaveLoadUtility.DeleteAsync(_saveLoadManager, FileName);

                IsPersistent = false;
                
                foreach (var trackedSaveSceneManager in _saveLoadManager.TrackedSaveSceneManagers)
                {
                    trackedSaveSceneManager.HandleAfterDeleteDiskData();
                }
                
                Debug.LogWarning("Delete Completed!");
            });
        }

        #region Private Methods

        private void InternalSnapshotActiveScenes(SaveData saveData, params SaveSceneManager[] saveSceneManagers)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            //get relevant scene data
            foreach (var saveSceneManager in saveSceneManagers)
            {
                if (!saveSceneManager.Scene.isLoaded)
                {
                    Debug.LogWarning($"Tried to snapshot the unloaded scene {saveSceneManager.name}!");
                }
            }
            
            //before event
            foreach (var saveSceneManager in saveSceneManagers)
            {
                if (saveSceneManager.Scene.isLoaded)
                {
                    saveSceneManager.HandleBeforeSnapshot();
                }
            }

            
            
            HashSet<AssetRegistry> processedAssetRegistries = new();
            Dictionary<GameObject, GuidPath> uniqueGameObjects = new();
            Dictionary<ScriptableObject, GuidPath> uniqueScriptableObjects = new();
            Dictionary<Component, GuidPath> uniqueComponents = new();
            foreach (var saveSceneManager in saveSceneManagers)
            {
                //prevent processing of the same asset registry
                if (!processedAssetRegistries.Add(saveSceneManager.AssetRegistry)) continue;
                
                foreach (var (gameObject, guidPath) in saveSceneManager.SavableGameObjectToGuidLookup)
                {
                    uniqueGameObjects.TryAdd(gameObject, new GuidPath(saveSceneManager.Scene.name, guidPath.TargetGuid));
                }
                
                foreach (var (scriptableObject, guidPath) in saveSceneManager.ScriptableObjectToGuidLookup)
                {
                    uniqueScriptableObjects.TryAdd(scriptableObject, new GuidPath(guidPath.TargetGuid));
                }
                
                foreach (var (component, guidPath) in saveSceneManager.ComponentToGuidLookup)
                {
                    uniqueComponents.TryAdd(component, new GuidPath(saveSceneManager.Scene.name, guidPath.TargetGuid));
                }
            }
            
            
            Dictionary<GuidPath, InstanceSaveData> instanceSaveDataLookup = new();
            Dictionary<object, GuidPath> processedInstancesLookup = new ();
            foreach (var (scriptableObject, guidPath) in uniqueScriptableObjects)
            {
                var instanceSaveData = new InstanceSaveData();

                instanceSaveDataLookup.Add(guidPath, instanceSaveData);

                if (!TypeUtility.TryConvertTo(scriptableObject, out ISavable targetSavable)) return;

                targetSavable.OnSave(new SaveDataHandler(guidPath, instanceSaveData, instanceSaveDataLookup,
                    processedInstancesLookup, uniqueGameObjects, uniqueScriptableObjects, uniqueComponents));
            }
            
            
            
            //perform snapshot
            foreach (var saveSceneManager in saveSceneManagers)
            {
                if (saveSceneManager.Scene.isLoaded)
                {
                    saveData.SetSceneData(saveSceneManager.Scene, saveSceneManager.CreateSnapshot());
                }
            }
            
            //after event
            foreach (var saveSceneManager in saveSceneManagers)
            {
                if (saveSceneManager.Scene.isLoaded)
                {
                    saveSceneManager.HandleAfterSnapshot();
                }
            }
            
            stopwatch.Stop();
            Debug.LogWarning("Time taken: " + stopwatch.ElapsedMilliseconds + " ms");
            
            Debug.LogWarning("Snapshot Completed!");
        }

        private void InternalLoadActiveScenes(SaveData saveData, params SaveSceneManager[] saveSceneManagers)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //get relevant scene data
            Dictionary<SaveSceneManager, SceneSaveData> sceneDataLookup = new();
            foreach (var saveSceneManager in saveSceneManagers)
            {
                if (!saveSceneManager.Scene.isLoaded)
                {
                    Debug.LogWarning($"Tried to load a save into the unloaded scene {saveSceneManager.name}!");
                }
                
                if (!saveData.TryGetSceneData(saveSceneManager.Scene, out SceneSaveData sceneDataContainer)) continue;
                
                sceneDataLookup.Add(saveSceneManager, sceneDataContainer);
            }

            //before event
            foreach (var saveSceneManager in sceneDataLookup.Keys)
            {
                saveSceneManager.HandleBeforeLoad();
            }
            
            
            
            HashSet<AssetRegistry> processedAssetRegistries = new();
            Dictionary<GuidPath, GameObject> uniqueGameObjects = new();
            Dictionary<GuidPath, ScriptableObject> uniqueScriptableObjects = new();
            Dictionary<GuidPath, Component> uniqueComponents = new();
            foreach (var saveSceneManager in saveSceneManagers)
            {
                //prevent processing of the same asset registry
                if (!processedAssetRegistries.Add(saveSceneManager.AssetRegistry)) continue;
                
                foreach (var (guidPath, gameObject) in saveSceneManager.GuidToSavableGameObjectLookup)
                {
                    uniqueGameObjects.TryAdd(new GuidPath(saveSceneManager.Scene.name, guidPath.TargetGuid), gameObject);
                }
                
                foreach (var (guidPath, scriptableObject) in saveSceneManager.GuidToScriptableObjectLookup)
                {
                    uniqueScriptableObjects.TryAdd(new GuidPath(guidPath.TargetGuid), scriptableObject);
                }
                
                foreach (var (guidPath, component) in saveSceneManager.GuidToComponentLookup)
                {
                    uniqueComponents.TryAdd(new GuidPath(saveSceneManager.Scene.name, guidPath.TargetGuid), component);
                }
            }
            
            /*
            //perform scriptable object snapshot
            foreach (var (scriptableObject, guidPath) in uniqueScriptableObjects)
            {
                if (sceneSaveData.InstanceSaveDataLookup.TryGetValue(guidPath, out var instanceSaveData))
                {
                    var loadDataHandler = new LoadDataHandler(sceneSaveData, instanceSaveData, 
                        createdObjectsLookup, this);
                        
                    if (!TypeUtility.TryConvertTo(scriptableObject, out ISavable targetSavable)) return;
                    
                    targetSavable.OnLoad(loadDataHandler);
                }
            }
             */
            
            //perform load
            foreach (var (saveSceneManager, sceneSaveData) in sceneDataLookup)
            {
                saveSceneManager.LoadSnapshot(sceneSaveData);
            }
            
            //after event
            foreach (var saveSceneManager in sceneDataLookup.Keys)
            {
                saveSceneManager.HandleAfterLoad();
            }
            
            stopwatch.Stop();
            Debug.LogWarning("Time taken: " + stopwatch.ElapsedMilliseconds + " ms");
            
            Debug.LogWarning("Loading Completed!");
        }
        
        private Task<AsyncOperation> LoadSceneAsync(string scenePath)
        {
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(scenePath);
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
            ProcessQueue();
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
