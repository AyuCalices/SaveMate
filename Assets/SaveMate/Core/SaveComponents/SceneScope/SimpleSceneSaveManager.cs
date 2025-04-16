using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SaveMate.Core.DataTransferObject;
using SaveMate.Core.EventHandler;
using SaveMate.Core.SavableGroupInterfaces;
using SaveMate.Core.SaveComponents.GameObjectScope;
using SaveMate.Core.SaveComponents.ManagingScope;
using SaveMate.Core.StateSnapshot;
using SaveMate.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveMate.Core.SaveComponents.SceneScope
{
    public class SimpleSceneSaveManager : MonoBehaviour, ISavableGroupHandler, ILoadableGroupHandler
    {
        public string SceneName { get; private set; }
        
        private readonly Dictionary<string, Savable> _trackedSavables = new();
        
        internal readonly Dictionary<GameObject, GuidPath> SavableGameObjectToGuidLookup = new();
        internal readonly Dictionary<Component, GuidPath> ComponentToGuidLookup = new();
        
        internal readonly Dictionary<GuidPath, GameObject> GuidToSavableGameObjectLookup = new();
        internal readonly Dictionary<GuidPath, Component> GuidToComponentLookup = new();
        
        #region Unity Lifecycle

        
        protected virtual void Awake()
        {
            SceneName = gameObject.scene.name;
        }

        protected virtual void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            SceneName = gameObject.scene.name;
        }

        protected virtual void Update()
        {
            if (gameObject.scene.name != SceneName)
            {
                Debug.LogWarning($"The scene in {nameof(SimpleSceneSaveManager)} ('{gameObject.name}') from scene " +
                                 $"'{gameObject.scene.name}' has changed. Despite the switch, {nameof(SimpleSceneSaveManager)} " +
                                 $"remains responsible for '{SceneName}'.");
            }
        }

        
        #endregion
        
        #region Savable Registration
        
         
        /// <summary>
        /// Registers a Savable object in the lookup system, ensuring each object has a unique identifier.
        /// If an object lacks a SceneGuid, a new one is assigned.
        /// If a duplicate ID is found, a new unique ID is generated.
        /// </summary>
        /// <param name="savable">The Savable object to be registered.</param>
        internal void RegisterSavable(Savable savable)
        {
            if (savable == null)
            {
                Debug.LogError("[SaveMate - Internal Error] Attempted to register a null Savable object.");
                return;
            }
            
#if UNITY_EDITOR
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ValidateSavableInEditor(savable);
            }
            else
            {
                if (!ValidateSavableAtRuntime(savable)) return;
            }
#else
            if (!ValidateSavableAtRuntime(savable)) return;
#endif

            // Add the Savable to the lookup, ensuring it is tracked
            InsertSavableIntoLookup(savable.SceneGuid, savable);
        }

        private void ValidateSavableInEditor(Savable savable)
        {
            // Case 1: If the object has no SceneGuid, generate a unique ID
            if (string.IsNullOrEmpty(savable.SceneGuid))
            {
                var id = GetUniqueID(savable);
                savable.SceneGuid = id;
            }

            // Case 2: If another object with the same SceneGuid exists, assign a new unique ID
            if (_trackedSavables != null && _trackedSavables.TryGetValue(savable.SceneGuid, out var registeredSavable) && registeredSavable != savable)
            {
                var id = GetUniqueID(savable);
                savable.SceneGuid = id;
                Debug.LogWarning($"Assigning a new unique ID to '{savable.gameObject.name}' in scene '{SceneName}'" +
                                 $" as the existing GUID was duplicated.");
            }
        }

        [UsedImplicitly]
        private bool ValidateSavableAtRuntime(Savable savable)
        {
            // Case 1: If the object has no SceneGuid, generate a unique ID
            if (string.IsNullOrEmpty(savable.SceneGuid))
            {
                if (!string.IsNullOrEmpty(savable.PrefabGuid))
                {
                    var id = GetUniqueID(savable);
                    savable.SceneGuid = id;
                    return false;
                }

                Debug.LogError("Tried to add an id to a non prefab! This is only allowed inside the EditorApplication");
                return false;
            }
            
            if (_trackedSavables != null && _trackedSavables.TryGetValue(savable.SceneGuid, out var registeredSavable) && registeredSavable != savable)
            {
                Debug.LogError($"Found a duplicated id at runtime! You might applied duplicated guids inside the EditorApplication. This may also occur, when two objects from different scenes with same id's are moved into the same scene!");
                return false;
            }

            return true;
        }
        
        internal void UnregisterSavable(Savable savable)
        {
            RemoveSavableFromLookup(savable);
            savable.SceneGuid = null;
        }

        private string GetUniqueID(Savable savable)
        {
            var guid = "GameObject_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            
            while (_trackedSavables != null && _trackedSavables.ContainsKey(guid))
            {
                guid = "GameObject_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            }

            return guid;
        }
        
        private void InsertSavableIntoLookup(string id, Savable savable)
        {
            //savable lookup registration
            if (!_trackedSavables.TryAdd(id, savable)) return;
            
            var savableGuid = new GuidPath(SceneName, savable.SceneGuid);
            
            GuidToSavableGameObjectLookup.Add(savableGuid, savable.gameObject);
            SavableGameObjectToGuidLookup.TryAdd(savable.gameObject, savableGuid);
            
            //Savable-Component registration
            foreach (var unityObjectIdentification in savable.DuplicateComponentLookup)
            {
                var componentGuidPath = new GuidPath(savableGuid, unityObjectIdentification.guid);
                ComponentToGuidLookup.TryAdd((Component)unityObjectIdentification.unityObject, componentGuidPath);
                GuidToComponentLookup.TryAdd(componentGuidPath, (Component)unityObjectIdentification.unityObject);
            }
                
            foreach (var unityObjectIdentification in savable.SaveStateHandlers)
            {
                var componentGuidPath = new GuidPath(savableGuid, unityObjectIdentification.guid);
                ComponentToGuidLookup.TryAdd((Component)unityObjectIdentification.unityObject, componentGuidPath);
                GuidToComponentLookup.TryAdd(componentGuidPath, (Component)unityObjectIdentification.unityObject);
            }
        }

        private void RemoveSavableFromLookup(Savable savable)
        {
            //savable lookup unregistration
            if (!_trackedSavables.Remove(savable.SceneGuid)) return;
            
            var savableGuid = new GuidPath(SceneName, savable.SceneGuid);
            
            GuidToSavableGameObjectLookup.Remove(savableGuid);
            SavableGameObjectToGuidLookup.Remove(savable.gameObject);
            
            //Savable-Component unregistration
            foreach (var unityObjectIdentification in savable.DuplicateComponentLookup)
            {
                ComponentToGuidLookup.Remove((Component)unityObjectIdentification.unityObject);
                    
                var componentGuidPath = new GuidPath(savableGuid, unityObjectIdentification.guid);
                GuidToComponentLookup.Remove(componentGuidPath);
            }

            foreach (var unityObjectIdentification in savable.SaveStateHandlers)
            {
                ComponentToGuidLookup.Remove((Component)unityObjectIdentification.unityObject);
                    
                var componentGuidPath = new GuidPath(savableGuid, unityObjectIdentification.guid);
                GuidToComponentLookup.Remove(componentGuidPath);
            }
        }
        
        
        #endregion

        #region Interface Implementation: ISavableGroupHandler

        
        void ISavableGroupHandler.OnBeforeCaptureSnapshot()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeSnapshotHandlers = savable.GetComponents<IBeforeCaptureSnapshotHandler>();
                foreach (var beforeSnapshotHandler in beforeSnapshotHandlers)
                {
                    beforeSnapshotHandler.OnBeforeCaptureSnapshot();
                }
            }

            InternalOnBeforeCaptureSnapshot();
        }
        
        void ISavableGroupHandler.CaptureSnapshot(SaveMateManager saveMateManager, SaveFileContext saveFileContext)
        {
            var sceneData = new SceneData 
            {
                ActivePrefabs = GetExistingPrefabsGuid(saveMateManager.GuidToSavablePrefabsLookup), 
                ActiveSaveData = CreateBranchSaveData(saveFileContext, saveMateManager)
            };
                
            saveFileContext.RootSaveData.UpsertSceneData(SceneName, sceneData);
        }

        void ISavableGroupHandler.OnAfterCaptureSnapshot()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeSnapshotHandlers = savable.GetComponents<IAfterCaptureSnapshotHandler>();
                foreach (var beforeSnapshotHandler in beforeSnapshotHandlers)
                {
                    beforeSnapshotHandler.OnAfterCaptureSnapshot();
                }
            }

            InternalOnAfterCaptureSnapshot();
        }

        
        #endregion
        
        #region Private: ISavableGroupHandler
        
        
        private BranchSaveData CreateBranchSaveData(SaveFileContext saveFileContext, SaveMateManager saveMateManager)
        {
            //save data
            var branchSaveData = new BranchSaveData();
            
            foreach (var saveObject in _trackedSavables.Values)
            {
                var savableGuidPath = new GuidPath(SceneName, saveObject.SceneGuid);
                foreach (var componentContainer in saveObject.SaveStateHandlers)
                {
                    if (componentContainer.unityObject is not ISaveStateHandler iSavable) continue;
            
                    var guidPath = new GuidPath(savableGuidPath, componentContainer.guid);
                    var leafSaveData = new LeafSaveData();
                    branchSaveData.UpsertLeafSaveData(guidPath, leafSaveData);
                    
                    iSavable.OnCaptureState(new CreateSnapshotHandler(branchSaveData, leafSaveData, guidPath, SceneName, saveFileContext, saveMateManager));
                    saveFileContext.SoftLoadedObjects.Remove(componentContainer.unityObject);
                }
            }

            return branchSaveData;
        }
        
        
        #endregion

        #region Interface Implementation: ILoadableGroupHandler

        
        void ILoadableGroupHandler.OnBeforeRestoreSnapshot()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<IBeforeRestoreSnapshotHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeRestoreSnapshot();
                }
            }

            InternalOnBeforeRestoreSnapshot();
        }
        
        void ILoadableGroupHandler.OnPrepareSnapshotObjects(SaveMateManager saveMateManager, SaveFileContext saveFileContext, LoadType loadType)
        {
            var currentSavePrefabGuidGroup = GetExistingPrefabsGuid(saveMateManager.GuidToSavablePrefabsLookup);
            SyncScenePrefabsOnLoad(saveFileContext.RootSaveData, saveMateManager.GuidToSavablePrefabsLookup, currentSavePrefabGuidGroup);
        }

        void ILoadableGroupHandler.RestoreSnapshot(SaveMateManager saveMateManager, SaveFileContext saveFileContext, LoadType loadType)
        {
            if (saveFileContext.RootSaveData.TryGetSceneData(SceneName, out var sceneData))
            {
                LoadBranchSaveData(saveFileContext, sceneData.ActiveSaveData, loadType, saveMateManager);
            }
        }
        
        void ILoadableGroupHandler.OnAfterRestoreSnapshot()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<IAfterRestoreSnapshotHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnAfterRestoreSnapshot();
                }
            }

            InternalOnAfterRestoreSnapshot();
        }
        

        #endregion
        
        #region Private: ILoadableGroupHandler
        

        private void SyncScenePrefabsOnLoad(RootSaveData rootSaveData, Dictionary<string, Savable> guidToSavablePrefabsLookup, PrefabGuidGroup currentSavePrefabGuidGroup)
        {
            if (rootSaveData.TryGetSceneData(SceneName, out var sceneData))
            {
                InstantiatePrefabsOnLoad(guidToSavablePrefabsLookup, sceneData.ActivePrefabs, currentSavePrefabGuidGroup);
                DestroyPrefabsOnLoad(sceneData.ActivePrefabs, currentSavePrefabGuidGroup);
            }
        }

        private void InstantiatePrefabsOnLoad(Dictionary<string, Savable> guidToSavablePrefabsLookup, PrefabGuidGroup loadedPrefabGuidGroup, PrefabGuidGroup currentPrefabGuidGroup)
        {
            var instantiatedSavables = loadedPrefabGuidGroup.Except(currentPrefabGuidGroup);
            foreach (var prefabSavable in instantiatedSavables)
            {
                if (guidToSavablePrefabsLookup.TryGetValue(prefabSavable.PrefabGuid, out Savable savable))
                {
                    /*
                     * When a savable gets instantiated, it will register itself to this SaveSceneManager and apply an ID.
                     * In order to use the ID that got saved, it must be applied before instantiation. Since this is done on
                     * the prefab, it must be undone on the prefab after instantiation.
                     */
                    savable.SceneGuid = prefabSavable.SceneGuid;
                    Instantiate(savable);
                    savable.SceneGuid = null;
                }
            }
        }

        private void DestroyPrefabsOnLoad(PrefabGuidGroup storedPrefabGuidGroup, PrefabGuidGroup currentPrefabGuidGroup)
        {
            var destroyedSavables = currentPrefabGuidGroup.Except(storedPrefabGuidGroup);
            foreach (var prefabsSavable in destroyedSavables)
            {
                Destroy(_trackedSavables[prefabsSavable.SceneGuid].gameObject);
            }
        }

        private void LoadBranchSaveData(SaveFileContext saveFileContext, BranchSaveData branchSaveData, LoadType loadType, 
            SaveMateManager saveMateManager)
        {
            //iterate over GameObjects with savable component
            foreach (var savable in _trackedSavables.Values)
            {
                var savableGuidPath = new GuidPath(SceneName, savable.SceneGuid);
                
                foreach (var savableComponent in savable.SaveStateHandlers)
                {
                    //return if it cant be loaded due to soft loading
                    var softLoadedObjects = saveFileContext.SoftLoadedObjects;
                    if (loadType != LoadType.Hard && softLoadedObjects.Contains(savableComponent.unityObject)) return;
                    
                    var componentGuidPath = new GuidPath(savableGuidPath, savableComponent.guid);

                    if (branchSaveData.TryGetLeafSaveData(componentGuidPath, out var instanceSaveData))
                    {
                        if (savableComponent.unityObject is not ISaveStateHandler iSavable) return;
                    
                        var loadDataHandler = new RestoreSnapshotHandler(saveFileContext.RootSaveData, instanceSaveData, loadType, 
                            componentGuidPath, saveFileContext, saveMateManager);
                        
                        iSavable.OnRestoreState(loadDataHandler);
                        softLoadedObjects.Add(savableComponent.unityObject);
                    }
                }
            }
        }

        
        #endregion
        
        #region Event Inheritance
        
        
        protected virtual void InternalOnBeforeCaptureSnapshot() {}
        
        protected virtual void InternalOnAfterCaptureSnapshot() {}
        
        internal void OnBeforeWriteToDisk()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<IBeforeWriteToDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeWriteToDisk();
                }
            }

            InternalOnBeforeWriteToDisk();
        }
        
        protected virtual void InternalOnBeforeWriteToDisk() { }
        
        internal void OnAfterWriteToDisk()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<IAfterWriteToDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnAfterWriteToDisk();
                }
            }

            InternalOnAfterWriteToDisk();
        }
        
        protected virtual void InternalOnAfterWriteToDisk() {}

        internal void OnBeforeReadFromDisk()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeReadFromDiskHandlers = savable.GetComponents<IBeforeReadFromDiskHandler>();
                foreach (var beforeReadFromDiskHandler in beforeReadFromDiskHandlers)
                {
                    beforeReadFromDiskHandler.OnBeforeReadFromDisk();
                }
            }

            InternalOnBeforeReadFromDisk();
        }
        
        protected virtual void InternalOnBeforeReadFromDisk() {}

        internal void OnAfterReadFromDisk()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var afterReadFromDiskHandlers = savable.GetComponents<IAfterReadFromDiskHandler>();
                foreach (var afterReadFromDiskHandler in afterReadFromDiskHandlers)
                {
                    afterReadFromDiskHandler.OnAfterReadFromDisk();
                }
            }

            InternalOnAfterReadFromDisk();
        }
        
        protected virtual void InternalOnAfterReadFromDisk() {}
        
        protected virtual void InternalOnBeforeRestoreSnapshot() {}
        
        protected virtual void InternalOnAfterRestoreSnapshot() {}

        internal void OnBeforeDeleteSnapshotData()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeDeleteSnapshotHandlers = savable.GetComponents<IBeforeDeleteSnapshotData>();
                foreach (var saveMateBeforeLoadHandler in beforeDeleteSnapshotHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeDeleteSnapshotData();
                }
            }

            InternalOnBeforeDeleteSnapshotData();
        }
        
        protected virtual void InternalOnBeforeDeleteSnapshotData() { }
        
        internal void OnAfterDeleteSnapshotData()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var afterDeleteSnapshotHandlers = savable.GetComponents<IAfterDeleteSnapshotData>();
                foreach (var afterDeleteSnapshotHandler in afterDeleteSnapshotHandlers)
                {
                    afterDeleteSnapshotHandler.OnAfterDeleteSnapshotData();
                }
            }

            InternalOnAfterDeleteSnapshotData();
        }
        
        protected virtual void InternalOnAfterDeleteSnapshotData() { }
        
        internal void OnBeforeDeleteDiskData()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<IBeforeDeleteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeDeleteDiskData();
                }
            }

            InternalOnBeforeDeleteDiskData();
        }

        protected virtual void InternalOnBeforeDeleteDiskData() { }

        internal void OnAfterDeleteDiskData()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<IAfterDeleteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnAfterDeleteDiskData();
                }
            }

            InternalOnAfterDeleteDiskData();
        }
        
        protected virtual void InternalOnAfterDeleteDiskData() { }

        
        #endregion
        
        #region Prefab Utillity

        
        private PrefabGuidGroup GetExistingPrefabsGuid(Dictionary<string, Savable> guidToSavablePrefabsLookup)
        {
            return new PrefabGuidGroup
            {
                Guids = (from savable in _trackedSavables.Values
                        where !savable.DisablePrefabSpawning &&
                              guidToSavablePrefabsLookup.ContainsKey(savable.PrefabGuid)
                        select new SavablePrefabElement { PrefabGuid = savable.PrefabGuid, SceneGuid = savable.SceneGuid })
                    .ToHashSet()
            };
        }
        
        
        #endregion
        
        #region Private Classes
        
        
        [InitializeOnLoad]
        private static class SaveObjectDestructionUpdater
        {
            static SaveObjectDestructionUpdater()
            {
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
            }
            
            private static void OnHierarchyChanged()
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                
                var saveSceneManagers = FindObjectsOfType<SimpleSceneSaveManager>();

                foreach (var saveSceneManager in saveSceneManagers)
                {
                    if (saveSceneManager._trackedSavables == null) return;
                    
                    //remove invalid objects
                    List<string> keysToRemove = new();
                    foreach (var (guidPath, savable) in saveSceneManager._trackedSavables)
                    {
                        if (savable.IsUnityNull())
                        {
                            keysToRemove.Add(guidPath);
                        }
                    }

                    foreach (var key in keysToRemove)
                    {
                        saveSceneManager._trackedSavables.Remove(key);
                    }
                    
                    //if a object is getting unlinked -> reset prefab path
                    foreach (var savable in saveSceneManager._trackedSavables.Values)
                    {
                        if (PrefabUtility.GetPrefabInstanceStatus(savable) != PrefabInstanceStatus.Connected)
                        {
                            savable.PrefabGuid = null;
                        }
                    }
                }
            }
        }
        
        
        #endregion
    }
}
