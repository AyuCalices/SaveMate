using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.UnityComponent;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

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
        private RootSaveData _rootSaveData;

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
                _rootSaveData ??= new RootSaveData();
                
                HasPendingData = true;
                InternalSnapshotScenes(_rootSaveData, scenesToSnapshot);
                
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
                
                await SaveLoadUtility.WriteDataAsync(_saveLoadManager, _saveLoadManager, FileName, _metaData, _rootSaveData);

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
                if (_rootSaveData == null) return Task.CompletedTask;
                
                InternalLoadScenes(_rootSaveData, saveSceneManagersToApplySnapshot);
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
                if (IsPersistent && _rootSaveData == null)
                {
                    _rootSaveData = await SaveLoadUtility.ReadSaveDataSecureAsync(_saveLoadManager, _saveLoadManager.SaveVersion, _saveLoadManager, FileName);
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
                if (IsPersistent && _rootSaveData == null)
                {
                    _rootSaveData = await SaveLoadUtility.ReadSaveDataSecureAsync(_saveLoadManager, _saveLoadManager.SaveVersion, _saveLoadManager, FileName);
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
                
                InternalLoadScenes(_rootSaveData, matchingGuids.ToArray());
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
                if (_rootSaveData == null) return Task.CompletedTask;

                foreach (var saveSceneManager in saveSceneManagersToWipe)
                {
                    _rootSaveData.RemoveSceneData(saveSceneManager.Scene);
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

        private void InternalSnapshotScenes(RootSaveData currentRootSaveData, params SaveSceneManager[] saveSceneManagers)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            //before event
            foreach (var saveSceneManager in saveSceneManagers)
            {
                saveSceneManager.HandleBeforeSnapshot();
            }

            //TODO: Prefab stuff
            //TODO: cleanup
            
            List<string> requestedScenes = new (){ RootSaveData.GlobalSaveDataName };
            HashSet<AssetRegistry> processedAssetRegistries = new();
            Dictionary<GameObject, GuidPath> uniqueGameObjects = new();
            Dictionary<ScriptableObject, GuidPath> uniqueScriptableObjectsToGuidPath = new();   //TODO: instead of GuidPath there is a List<GuidPath> required
            Dictionary<Component, GuidPath> uniqueComponents = new();
            foreach (var saveSceneManager in saveSceneManagers)
            {
                //prevent processing of the same asset registry
                if (!processedAssetRegistries.Add(saveSceneManager.AssetRegistry)) continue;
                
                requestedScenes.Add(saveSceneManager.Scene.name);
                
                foreach (var (gameObject, guidPath) in saveSceneManager.SavableGameObjectToGuidLookup)
                {
                    uniqueGameObjects.TryAdd(gameObject, guidPath);
                }
                
                foreach (var (scriptableObject, guidPath) in saveSceneManager.ScriptableObjectToGuidLookup)
                {
                    uniqueScriptableObjectsToGuidPath.TryAdd(scriptableObject, guidPath);
                }
                
                foreach (var (component, guidPath) in saveSceneManager.ComponentToGuidLookup)
                {
                    uniqueComponents.TryAdd(component, guidPath);
                }
            }

            var scriptableObjectsToSave = GetScriptableObjectToSave(requestedScenes, uniqueScriptableObjectsToGuidPath, currentRootSaveData);
            
            Dictionary<object, GuidPath> processedObjectLookup = new ();
            
            var globalSaveData = new BranchSaveData();
            foreach (var (scriptableObject, guidPath) in scriptableObjectsToSave)
            {
                var leafSaveData = new LeafSaveData();

                globalSaveData.AddLeafSaveData(guidPath, leafSaveData);

                if (!TypeUtility.TryConvertTo(scriptableObject, out ISavable targetSavable)) return;

                targetSavable.OnSave(new SaveDataHandler(globalSaveData, guidPath, leafSaveData, processedObjectLookup, 
                    uniqueGameObjects, uniqueScriptableObjectsToGuidPath, uniqueComponents));
            }
            
            currentRootSaveData.SetGlobalSceneData(globalSaveData);
            
            //perform snapshot
            foreach (var saveSceneManager in saveSceneManagers)
            {
                currentRootSaveData.SetSceneData(saveSceneManager.Scene, saveSceneManager.CreateSnapshot(processedObjectLookup));
            }
            
            //after event
            foreach (var saveSceneManager in saveSceneManagers)
            {
                saveSceneManager.HandleAfterSnapshot();
            }
            
            stopwatch.Stop();
            Debug.LogWarning("Time taken: " + stopwatch.ElapsedMilliseconds + " ms");
            
            Debug.LogWarning("Snapshot Completed!");
        }
        
        private Dictionary<ScriptableObject, GuidPath> GetScriptableObjectToSave(List<string> requestedScenes, 
            Dictionary<ScriptableObject, GuidPath> uniqueScriptableObjects, RootSaveData rootSaveData)
        {
            Dictionary<ScriptableObject, GuidPath> scriptableObjectsToLoad = new();
            
            foreach (var (scriptableObject, branchGuidPath) in uniqueScriptableObjects)
            {
                if (scriptableObject == null)
                {
                    Debug.LogWarning($"Skipped ScriptableObject because it is null. GUID: {branchGuidPath}");
                    continue;
                }
                
                // Check if this object already exists in stored data
                if (rootSaveData.GlobalSaveData.Elements.TryGetValue(branchGuidPath, out LeafSaveData leafSaveData))
                {
                    if (ScenesForGlobalLeafSaveDataAreActive(requestedScenes, leafSaveData))
                    {
                        scriptableObjectsToLoad.Add(scriptableObject, branchGuidPath);
                    }
                    else
                    {
                        Debug.LogWarning($"Skipped ScriptableObject '{scriptableObject.name}' for saving, because of a scene requirement. ScriptableObject GUID: '{branchGuidPath.ToString()}'");
                    }
                }
                else
                {  
                    scriptableObjectsToLoad.Add(scriptableObject, branchGuidPath);
                }
            }

            return scriptableObjectsToLoad;
        }
        
        private void InternalLoadScenes(RootSaveData rootSaveData, params SaveSceneManager[] saveSceneManagers)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //before event
            foreach (var saveSceneManager in saveSceneManagers)
            {
                saveSceneManager.HandleBeforeLoad();
            }
            
            //TODO: Prefab stuff
            //TODO: cleanup
            
            HashSet<AssetRegistry> processedAssetRegistries = new();
            List<string> requestedScenes = new (){ RootSaveData.GlobalSaveDataName };
            Dictionary<GuidPath, GameObject> uniqueGameObjects = new();
            Dictionary<GuidPath, ScriptableObject> uniqueUnloadedScriptableObjects = new();
            Dictionary<GuidPath, Component> uniqueComponents = new();
            foreach (var saveSceneManager in saveSceneManagers)
            {
                //prevent processing of the same asset registry
                if (!processedAssetRegistries.Add(saveSceneManager.AssetRegistry)) continue;
                
                requestedScenes.Add(saveSceneManager.Scene.name);
                
                foreach (var (guidPath, gameObject) in saveSceneManager.GuidToSavableGameObjectLookup)
                {
                    uniqueGameObjects.TryAdd(guidPath, gameObject);
                }
                
                foreach (var (guidPath, scriptableObject) in saveSceneManager.GuidToScriptableObjectLookup)
                {
                    uniqueUnloadedScriptableObjects.TryAdd(guidPath, scriptableObject);
                }
                
                foreach (var (guidPath, component) in saveSceneManager.GuidToComponentLookup)
                {
                    uniqueComponents.TryAdd(guidPath, component);
                }
            }

            var scriptableObjectsToLoad = GetScriptableObjectToLoad(requestedScenes, uniqueUnloadedScriptableObjects, rootSaveData);
            
            var createdObjectsLookup = new Dictionary<GuidPath, object>();
            
            //perform scriptable object snapshot
            //TODO: create snapshot comparison and throw out scriptable object branches, that have changed -> this also includes objects created by the scriptable object
            foreach (var (guidPath, scriptableObject) in uniqueUnloadedScriptableObjects) 
            {
                if (rootSaveData.GlobalSaveData.TryGetLeafSaveData(guidPath, out var instanceSaveData))
                {
                    var loadDataHandler = new LoadDataHandler(rootSaveData.GlobalSaveData, instanceSaveData, 
                        createdObjectsLookup, uniqueGameObjects, scriptableObjectsToLoad, uniqueComponents);
                        
                    if (!TypeUtility.TryConvertTo(scriptableObject, out ISavable targetSavable)) return;
                    
                    targetSavable.OnLoad(loadDataHandler);
                }
            }
            
            
            //perform load
            foreach (var saveSceneManager in saveSceneManagers)
            {
                if (rootSaveData.TryGetSceneData(saveSceneManager.Scene, out var sceneData))
                {
                    saveSceneManager.LoadSnapshot(sceneData, createdObjectsLookup);
                }
            }
            
            //after event
            foreach (var saveSceneManager in saveSceneManagers)
            {
                saveSceneManager.HandleAfterLoad();
            }
            
            stopwatch.Stop();
            Debug.LogWarning("Time taken: " + stopwatch.ElapsedMilliseconds + " ms");
            
            Debug.LogWarning("Loading Completed!");
        }

        private Dictionary<GuidPath, ScriptableObject> GetScriptableObjectToLoad(List<string> requestedScenes, 
            Dictionary<GuidPath, ScriptableObject> uniqueUnloadedScriptableObjects, RootSaveData rootSaveData)
        {
            Dictionary<GuidPath, ScriptableObject> scriptableObjectsToLoad = new();
            
            foreach (var (branchGuidPath, scriptableObject) in uniqueUnloadedScriptableObjects)
            {
                if (scriptableObject == null)
                {
                    Debug.LogWarning($"Skipped ScriptableObject because it is null. GUID: {branchGuidPath}");
                    continue;
                }
                
                // Check if this object already exists in stored data
                if (rootSaveData.GlobalSaveData.Elements.TryGetValue(branchGuidPath, out LeafSaveData leafSaveData))
                {
                    if (ScenesForGlobalLeafSaveDataAreActive(requestedScenes, leafSaveData))
                    {
                        scriptableObjectsToLoad.Add(branchGuidPath, scriptableObject);
                    }
                    else
                    {
                        Debug.LogWarning($"Skipped ScriptableObject '{scriptableObject.name}' for loading, because of a scene requirement. ScriptableObject GUID: '{branchGuidPath.ToString()}'");
                    }
                }
            }

            return scriptableObjectsToLoad;
        }
        
        bool ScenesForGlobalLeafSaveDataAreActive(List<string> requiredScenes, LeafSaveData leafSaveData)
        {
            foreach (var referenceGuidPath in leafSaveData.References.Values)
            {
                if (!requiredScenes.Contains(referenceGuidPath.Scene)) return false;
            }

            return true;
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
