using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public enum LoadType { Hard, Soft }
    
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
        private Dictionary<GuidPath, WeakReference<object>> _createdObjectLookup;
        private HashSet<ScriptableObject> _loadedScriptableObjects;
        private HashSet<SaveSceneManager> _loadedSaveSceneManagers;
        
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
                _createdObjectLookup ??= new Dictionary<GuidPath, WeakReference<object>>();
                _loadedScriptableObjects ??= new HashSet<ScriptableObject>();
                _loadedSaveSceneManagers ??= new HashSet<SaveSceneManager>();
                
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

        public void ApplySnapshotToActiveScenes(LoadType loadType)
        {
            ApplySnapshotToScenes(loadType, _saveLoadManager.TrackedSaveSceneManagers.ToArray());
        }

        public void ApplySnapshotToScenes(LoadType loadType, params SaveSceneManager[] saveSceneManagersToApplySnapshot)
        {
            _asyncQueue.Enqueue(() =>
            {
                if (_rootSaveData == null) return Task.CompletedTask;
                
                InternalLoadScenes(_rootSaveData, loadType, saveSceneManagersToApplySnapshot);
                return Task.CompletedTask;
            });
        }
        
        public void LoadActiveScenes(LoadType loadType)
        {
            ReadFromDisk();
            ApplySnapshotToActiveScenes(loadType);
        }
        
        public void LoadScenes(LoadType loadType, params SaveSceneManager[] saveSceneManagersToLoad)
        {
            ReadFromDisk();
            ApplySnapshotToScenes(loadType, saveSceneManagersToLoad);
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
                    _createdObjectLookup = new Dictionary<GuidPath, WeakReference<object>>();
                    _loadedScriptableObjects = new HashSet<ScriptableObject>();
                    _loadedSaveSceneManagers = new HashSet<SaveSceneManager>();
                }
            });
        }
        
        public void ReloadThenLoadActiveScenes(LoadType loadType)
        {
            ReloadThenLoadScenes(loadType, _saveLoadManager.TrackedSaveSceneManagers.ToArray());
        }

        /// <summary>
        /// data will be applied after awake
        /// </summary>
        /// <param name="loadType"></param>
        /// <param name="scenesToLoad"></param>
        public void ReloadThenLoadScenes(LoadType loadType, params SaveSceneManager[] scenesToLoad)
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (!SaveLoadUtility.SaveDataExists(_saveLoadManager, FileName) 
                    || !SaveLoadUtility.MetaDataExists(_saveLoadManager, FileName)) return;
                
                //only load saveData, if it is persistent and not initialized
                if (IsPersistent && _rootSaveData == null)
                {
                    _rootSaveData = await SaveLoadUtility.ReadSaveDataSecureAsync(_saveLoadManager, _saveLoadManager.SaveVersion, _saveLoadManager, FileName);
                    _createdObjectLookup = new Dictionary<GuidPath, WeakReference<object>>();
                    _loadedScriptableObjects = new HashSet<ScriptableObject>();
                    _loadedSaveSceneManagers = new HashSet<SaveSceneManager>();
                }

                //buffer save paths, because they will be null later on the scene array
                var sceneNamesToLoad = new string[scenesToLoad.Length];
                for (var index = 0; index < scenesToLoad.Length; index++)
                {
                    sceneNamesToLoad[index] = scenesToLoad[index].Scene.path;
                }
                
                InternalSnapshotScenes(_rootSaveData, scenesToLoad);

                //async reload scene and continue, when all are loaded
                var loadMode = SceneManager.sceneCount == 1 ? LoadSceneMode.Single : LoadSceneMode.Additive;
                if (SceneManager.sceneCount > 1)
                {
                    var unloadTasks = sceneNamesToLoad.Select(UnloadSceneAsync).ToList();
                    await Task.WhenAll(unloadTasks);
                }
                
                var loadTasks = sceneNamesToLoad.Select(name => LoadSceneAsync(name, loadMode)).ToList();
                await Task.WhenAll(loadTasks);
                

                //remap save paths to the reloaded scenes
                List<SaveSceneManager> matchingGuids = new();
                foreach (var saveSceneManager in _saveLoadManager.TrackedSaveSceneManagers)
                {
                    if (sceneNamesToLoad.Contains(saveSceneManager.Scene.path))
                    {
                        matchingGuids.Add(saveSceneManager);
                    }
                }
                
                InternalLoadScenes(_rootSaveData, loadType, matchingGuids.ToArray());
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
            
            List<string> activeSceneNames = new (){ RootSaveData.GlobalSaveDataName };
            Dictionary<GameObject, GuidPath> gameObjects = new();
            Dictionary<ScriptableObject, GuidPath> uniqueScriptableObjects = new();
            Dictionary<Component, GuidPath> components = new();
            foreach (var saveSceneManager in _saveLoadManager.TrackedSaveSceneManagers)
            {
                activeSceneNames.Add(saveSceneManager.Scene.name);
                
                foreach (var (gameObject, guidPath) in saveSceneManager.SavableGameObjectToGuidLookup)
                {
                    gameObjects.Add(gameObject, guidPath);
                }
                
                foreach (var (component, guidPath) in saveSceneManager.ComponentToGuidLookup)
                {
                    components.Add(component, guidPath);
                }
                
                foreach (var (scriptableObject, guidPath) in saveSceneManager.ScriptableObjectToGuidLookup)
                {
                    uniqueScriptableObjects.TryAdd(scriptableObject, guidPath);
                }
            }

            var scriptableObjectsToSave = ParseScriptableObjectWithLoadedScenes(currentRootSaveData, activeSceneNames, uniqueScriptableObjects);
            
            Dictionary<object, GuidPath> processedObjectLookup = new ();
            
            var globalSaveData = new BranchSaveData();
            currentRootSaveData.SetGlobalSceneData(globalSaveData);     //TODO: change: must be assigned before the scriptable object OnSave method -> otherwise it will be overwritten
            foreach (var (scriptableObject, guidPath) in scriptableObjectsToSave)
            {
                if (!TypeUtility.TryConvertTo(scriptableObject, out ISavable targetSavable)) return;
                
                var leafSaveData = new LeafSaveData();
                globalSaveData.AddLeafSaveData(guidPath, leafSaveData);

                targetSavable.OnSave(new SaveDataHandler(currentRootSaveData, leafSaveData, guidPath, _createdObjectLookup,
                    processedObjectLookup, gameObjects, uniqueScriptableObjects, components));
                
                _loadedScriptableObjects.Remove(scriptableObject);
            }
            
            //perform snapshot
            foreach (var saveSceneManager in saveSceneManagers)
            {
                var prefabGuidGroup = saveSceneManager.CreatePrefabGuidGroup();
                var branchSaveData = saveSceneManager.CreateBranchSaveData(currentRootSaveData, _createdObjectLookup, 
                    processedObjectLookup, gameObjects, uniqueScriptableObjects, components);
                
                var sceneData = new SceneData 
                {
                    ActivePrefabs = prefabGuidGroup, 
                    ActiveSaveData = branchSaveData
                };
                
                currentRootSaveData.SetSceneData(saveSceneManager.Scene, sceneData);
                _loadedSaveSceneManagers.Remove(saveSceneManager);
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
        
        private Dictionary<ScriptableObject, GuidPath> ParseScriptableObjectWithLoadedScenes(RootSaveData rootSaveData, 
            List<string> activeSceneNames, Dictionary<ScriptableObject, GuidPath> uniqueScriptableObjects)
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
                    if (ScenesForGlobalLeafSaveDataAreLoaded(activeSceneNames, leafSaveData))
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
        
        //TODO: implement a list that tracks already loaded scriptable objects -> additive scriptable object loading will only process unloaded ones
        //TODO: setup of references of everything that is active -> loading of everything that is related to the parameter
        private void InternalLoadScenes(RootSaveData rootSaveData, LoadType loadType, params SaveSceneManager[] saveSceneManagers)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            //cleanup
            _loadedSaveSceneManagers.RemoveWhere(x => x.IsUnityNull());
            
            //select scenes based on hard and Soft loading
            var scenesToLoad = new List<SaveSceneManager>();
            foreach (var saveSceneManager in saveSceneManagers)
            {
                if (loadType == LoadType.Hard || !_loadedSaveSceneManagers.Contains(saveSceneManager))
                {
                    scenesToLoad.Add(saveSceneManager);
                }
            }
            
            //on before load event
            foreach (var saveSceneManager in scenesToLoad)
            {
                saveSceneManager.HandleBeforeLoad();
            }

            //handle prefabs
            foreach (var saveSceneManager in scenesToLoad)
            {
                var currentSavePrefabGuidGroup = saveSceneManager.CreatePrefabGuidGroup();
                
                if (rootSaveData.TryGetSceneData(saveSceneManager.Scene, out var sceneData))
                {
                    saveSceneManager.InstantiatePrefabsOnLoad(sceneData.ActivePrefabs, currentSavePrefabGuidGroup);
                    saveSceneManager.DestroyPrefabsOnLoad(sceneData.ActivePrefabs, currentSavePrefabGuidGroup);
                }
            }
            
            //cleanup unused weak references
            //CleanupWeakReferences();
            
            //prepare references
            List<string> activeSceneNames = new (){ RootSaveData.GlobalSaveDataName };
            Dictionary<GuidPath, ScriptableObject> uniqueScriptableObjects = new();
            Dictionary<GuidPath, GameObject> uniqueGameObjects = new();
            Dictionary<GuidPath, Component> uniqueComponents = new();
            foreach (var saveSceneManager in _saveLoadManager.TrackedSaveSceneManagers)
            {
                activeSceneNames.Add(saveSceneManager.Scene.name);
                
                foreach (var (guidPath, gameObject) in saveSceneManager.GuidToSavableGameObjectLookup)
                {
                    uniqueGameObjects.TryAdd(guidPath, gameObject);
                }
                
                foreach (var (guidPath, component) in saveSceneManager.GuidToComponentLookup)
                {
                    uniqueComponents.TryAdd(guidPath, component);
                }
                
                foreach (var (guidPath, scriptableObject) in saveSceneManager.GuidToScriptableObjectLookup)
                {
                    if (loadType == LoadType.Hard || !_loadedScriptableObjects.Contains(scriptableObject))
                    {
                        uniqueScriptableObjects.TryAdd(guidPath, scriptableObject);
                    }
                }
            }

            
            //throw out scriptable objects, that have references to unloaded scenes in the save files
            var scriptableObjectsToLoad = ParseScriptableObjectWithLoadedScenes(rootSaveData, activeSceneNames, uniqueScriptableObjects);

            
            //when resetting hard, everything that already has been created must be resetted. Otherwise the system things, they have already been created by Soft Loading.
            if (loadType == LoadType.Hard)
            {
                var objectsToRemove = new List<(GuidPath, object)>();
                foreach (var (guidPath, obj) in _createdObjectLookup)
                {
                    if (guidPath.Scene == RootSaveData.GlobalSaveDataName)
                    {
                        objectsToRemove.Add((guidPath, obj));
                        continue;   //match has been found -> next guidPath
                    }
                    
                    foreach (var saveSceneManager in scenesToLoad)
                    {
                        if (saveSceneManager.Scene.name == guidPath.Scene)
                        {
                            objectsToRemove.Add((guidPath, obj));
                            break;   //match has been found -> next guidPath
                        }
                    }
                }
                
                foreach (var (guidPath, obj) in objectsToRemove)
                {
                    _createdObjectLookup.Remove(guidPath);
                }
            }
            
            
            //perform scriptable object laod
            foreach (var (guidPath, scriptableObject) in uniqueScriptableObjects) 
            {
                if (rootSaveData.GlobalSaveData.TryGetLeafSaveData(guidPath, out var instanceSaveData))
                {
                    if (!TypeUtility.TryConvertTo(scriptableObject, out ISavable targetSavable)) return;
                    
                    var loadDataHandler = new LoadDataHandler(rootSaveData, rootSaveData.GlobalSaveData, instanceSaveData, 
                        _createdObjectLookup, uniqueGameObjects, scriptableObjectsToLoad, uniqueComponents);
                    
                    targetSavable.OnLoad(loadDataHandler);
                    
                    _loadedScriptableObjects.Add(scriptableObject);
                }
            }
            
            //perform load
            foreach (var saveSceneManager in scenesToLoad)
            {
                if (rootSaveData.TryGetSceneData(saveSceneManager.Scene, out var sceneData))
                {
                    saveSceneManager.LoadBranchSaveData(rootSaveData, sceneData.ActiveSaveData, _createdObjectLookup, 
                        uniqueGameObjects, uniqueScriptableObjects, uniqueComponents);
                    _loadedSaveSceneManagers.Add(saveSceneManager);
                }
            }
            
            //on load completed event
            foreach (var saveSceneManager in scenesToLoad)
            {
                saveSceneManager.HandleAfterLoad();
            }
            
            stopwatch.Stop();
            Debug.LogWarning("Time taken: " + stopwatch.ElapsedMilliseconds + " ms");
            
            Debug.LogWarning("Loading Completed!");
        }

        /*
        private void CleanupWeakReferences()
        {
            List<GuidPath> keysToRemove = new();
            foreach (var (guidPath, weakReference) in _createdGuidToObjectLookup)
            {
                if (!weakReference.TryGetTarget(out _))
                {
                    keysToRemove.Add(guidPath);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _createdGuidToObjectLookup.Remove(key);
            }
        }*/

        private Dictionary<GuidPath, ScriptableObject> ParseScriptableObjectWithLoadedScenes(RootSaveData rootSaveData, 
            List<string> requestedScenes, Dictionary<GuidPath, ScriptableObject> uniqueUnloadedScriptableObjects)
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
                    if (ScenesForGlobalLeafSaveDataAreLoaded(requestedScenes, leafSaveData))
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
        
        bool ScenesForGlobalLeafSaveDataAreLoaded(List<string> requiredScenes, LeafSaveData leafSaveData)
        {
            foreach (var referenceGuidPath in leafSaveData.References.Values)
            {
                if (!requiredScenes.Contains(referenceGuidPath.Scene)) return false;
            }

            return true;
        }
        
        private Task<AsyncOperation> UnloadSceneAsync(string scenePath)
        {
            AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(scenePath);
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
