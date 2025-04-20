using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SaveMate.Runtime.Core.DataTransferObject;
using SaveMate.Runtime.Core.EventHandler;
using SaveMate.Runtime.Core.SavableGroupInterfaces;
using SaveMate.Runtime.Core.SaveComponents.AssetScope;
using SaveMate.Runtime.Utility;
using Debug = UnityEngine.Debug;

namespace SaveMate.Runtime.Core.SaveComponents.ManagingScope
{
    public enum LoadType { Hard, Soft }
    
    public class SaveFileContext
    {
        public string FileName { get; }
        private bool IsPersistent => SaveFileUtility.MetaDataExists(_saveMateManager, FileName) && SaveFileUtility.SaveDataExists(_saveMateManager, FileName);

        private readonly SaveMateManager _saveMateManager;
        private readonly AssetRegistry _assetRegistry;
        
        private readonly AsyncOperationQueue _asyncQueue;
        private bool _hasPendingData;
        
        internal RootSaveData RootSaveData;
        internal GuidToCreatedNonUnityObjectLookup GuidToCreatedNonUnityObjectLookup;
        internal ConditionalWeakTable<object, string> SavedNonUnityObjectToGuidLookup;
        internal HashSet<object> SoftLoadedObjects;
        
        
        public SaveFileContext(SaveMateManager saveMateManager, AssetRegistry assetRegistry, string fileName)
        {
            _saveMateManager = saveMateManager;
            _assetRegistry = assetRegistry;
            _asyncQueue = new AsyncOperationQueue();
            FileName = fileName;
        }


        #region Public Methods
        

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
                    if (savableGroup is not ISavableGroupHandler savableGroupHandler) continue;
                    
                    if (savableGroupHandler.SceneName != "DontDestroyOnLoad" && SaveLoadUtility.IsSceneUnloaded(savableGroupHandler.SceneName))
                    {
                        Debug.LogWarning($"[SaveMate] Skipped '{nameof(RestoreSnapshot)}' for scene '{savableGroupHandler.SceneName}': scene is not currently loaded.");
                        continue;
                    }
                    
                    if (savableGroup is INestableSaveGroupHandler nestableSaveGroupHandler)
                    {
                        combinedSavableGroupHandlers.AddRange(nestableSaveGroupHandler.GetSavableGroupHandlers());
                    }
                    combinedSavableGroupHandlers.Add(savableGroupHandler);
                }
                
                InternalCaptureSnapshot(combinedSavableGroupHandlers);
                return Task.CompletedTask;
            });
        }
        
        internal void WriteToDisk()
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (!_hasPendingData)
                {
                    Debug.LogWarning($"[SaveMate] {nameof(WriteToDisk)} aborted: No pending data to save on disk!");
                    return;
                }
                
                OnBeforeWriteToDisk();
                
                var metaData = new SaveMetaData()
                {
                    SaveVersion = _saveMateManager.SaveVersion,
                    ModificationDate = DateTime.Now,
                    CustomData = _saveMateManager.CustomMetaData
                };
                
                await SaveFileUtility.WriteDataAsync(_saveMateManager, _saveMateManager, FileName, metaData, RootSaveData);

                _hasPendingData = false;

                OnAfterWriteToDisk();
                
                Debug.Log($"[SaveMate] {nameof(WriteToDisk)} successful!");
            });
        }
        
        internal void ReadFromDisk()
        {
            _asyncQueue.Enqueue(async () =>
            {
                if (!SaveFileUtility.SaveDataExists(_saveMateManager, FileName) 
                    || !SaveFileUtility.MetaDataExists(_saveMateManager, FileName)) return;

                if (!IsPersistent)
                {
                    Debug.LogWarning($"[SaveMate] '{nameof(ReadFromDisk)}' aborted: No persistent save data found on disk.");
                }
                
                //only load saveData, if it is persistent and not initialized
                if (IsPersistent && RootSaveData == null)
                {
                    OnBeforeReadFromDisk();
                    
                    RootSaveData = await SaveFileUtility.ReadSaveDataSecureAsync(_saveMateManager, _saveMateManager.SaveVersion, _saveMateManager, FileName);
                    GuidToCreatedNonUnityObjectLookup = new GuidToCreatedNonUnityObjectLookup();
                    SavedNonUnityObjectToGuidLookup = new ConditionalWeakTable<object, string>();
                    SoftLoadedObjects = new HashSet<object>();
                    
                    OnAfterReadFromDisk();
                    
                    Debug.Log($"[SaveMate] {nameof(ReadFromDisk)} successful!");
                }
            });
        }

        internal void RestoreSnapshot(LoadType loadType, params ILoadableGroup[] loadableGroups)
        {
            _asyncQueue.Enqueue(() =>
            {
                if (RootSaveData == null)
                {
                    Debug.LogWarning($"[SaveMate] {nameof(RestoreSnapshot)} aborted: No save data found in memory. Ensure '{nameof(ReadFromDisk)}' was called before attempting to load.");
                    return Task.CompletedTask;
                }

                //convert into required handlers
                var combinedLoadGroupHandlers = new List<ILoadableGroupHandler>();
                foreach (var loadableGroup in loadableGroups)
                {
                    if (loadableGroup is not ILoadableGroupHandler loadableGroupHandler) continue;
                    
                    if (loadableGroupHandler.SceneName != "DontDestroyOnLoad" && SaveLoadUtility.IsSceneUnloaded(loadableGroupHandler.SceneName))
                    {
                        Debug.LogWarning($"[SaveMate] Skipped '{nameof(RestoreSnapshot)}' for scene '{loadableGroupHandler.SceneName}': scene is not currently loaded.");
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
                _hasPendingData = true;
                
                OnAfterDeleteSnapshotData();

                Debug.Log($"[SaveMate] {nameof(DeleteSnapshotData)} successful!");
                return Task.CompletedTask;
            });
        }
        
        internal void DeleteDiskData()
        {
            _asyncQueue.Enqueue(async () =>
            {
                OnBeforeDeleteDiskData();

                await SaveFileUtility.DeleteAsync(_saveMateManager, FileName);

                OnAfterDeleteDiskData();
                
                Debug.Log($"[SaveMate] {nameof(DeleteDiskData)} successful!");
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

            foreach (var trackedSaveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
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
            
            foreach (var trackedSaveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
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

            foreach (var trackedSaveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
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
            
            foreach (var trackedSaveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
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
            
            foreach (var trackedSaveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
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
            
            foreach (var trackedSaveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
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
            
            foreach (var trackedSaveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
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
            
            foreach (var trackedSaveSceneManager in _saveMateManager.GetTrackedSaveSceneManagers())
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
                savableGroupHandler.CaptureSnapshot(_saveMateManager, this);
            }
            _hasPendingData = true;
            
            OnAfterCaptureSnapshot(savableGroupHandlers);
            
            stopwatch.Stop();
            
            if (savableGroupHandlers.Count == 0)
            {
                Debug.Log($"[SaveMate] {nameof(CaptureSnapshot)} successful!");
            }
            else if (savableGroupHandlers.Count == 1)
            {
                Debug.Log($"[SaveMate] {nameof(CaptureSnapshot)} for scene {savableGroupHandlers[0].SceneName} successful! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
            else
            {
                Debug.Log($"[SaveMate] {nameof(CaptureSnapshot)} for {savableGroupHandlers.Count} scenes successful! Time taken: {stopwatch.ElapsedMilliseconds} ms");
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
                loadableGroupHandler.OnPrepareSnapshotObjects(_saveMateManager, this, loadType);
            }
            
            foreach (var loadableGroupHandler in loadableGroupHandlers)
            {
                loadableGroupHandler.RestoreSnapshot(_saveMateManager, this, loadType);
            }
            
            GuidToCreatedNonUnityObjectLookup.CompleteLoading();
            
            OnAfterRestoreSnapshot(loadableGroupHandlers);
            
            stopwatch.Stop();
            if (loadableGroupHandlers.Count == 0)
            {
                Debug.Log($"[SaveMate] {nameof(RestoreSnapshot)} successful!");
            }
            else if (loadableGroupHandlers.Count == 1)
            {
                Debug.Log($"[SaveMate] {nameof(RestoreSnapshot)} for scene {loadableGroupHandlers[0].SceneName} successful! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
            else
            {
                Debug.Log($"[SaveMate] {nameof(RestoreSnapshot)} for {loadableGroupHandlers.Count} scenes successful! Time taken: {stopwatch.ElapsedMilliseconds} ms");
            }
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
