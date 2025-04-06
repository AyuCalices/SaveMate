using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.EventHandler;
using SaveLoadSystem.Core.UnityComponent;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    public class SimpleSceneSaveManager : MonoBehaviour, 
        ICaptureSnapshotGroupElement, IBeforeCaptureSnapshotHandler, IAfterCaptureSnapshotHandler, 
        IRestoreSnapshotGroupElement, ISaveMateBeforeLoadHandler, ISaveMateAfterLoadHandler
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

            // Add the Savable to the lookup, ensuring it is tracked
            InsertSavableIntoLookup(savable.SceneGuid, savable);
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
        
        
        private bool InsertSavableIntoLookup(string id, Savable savable)
        {
            //savable lookup registration
            if (!_trackedSavables.TryAdd(id, savable)) return false;
            
            var savableGuid = new GuidPath(SceneName, savable.SceneGuid);
            
            GuidToSavableGameObjectLookup.Add(savableGuid, savable.gameObject);
            SavableGameObjectToGuidLookup.TryAdd(savable.gameObject, savableGuid);
            
            //Savable-Component registration.
            foreach (var unityObjectIdentification in savable.DuplicateComponentLookup)
            {
                var componentGuidPath = new GuidPath(savableGuid, unityObjectIdentification.guid);
                ComponentToGuidLookup.TryAdd((Component)unityObjectIdentification.unityObject, componentGuidPath);
                GuidToComponentLookup.TryAdd(componentGuidPath, (Component)unityObjectIdentification.unityObject);
            }
                
            foreach (var unityObjectIdentification in savable.SavableLookup)
            {
                var componentGuidPath = new GuidPath(savableGuid, unityObjectIdentification.guid);
                ComponentToGuidLookup.TryAdd((Component)unityObjectIdentification.unityObject, componentGuidPath);
                GuidToComponentLookup.TryAdd(componentGuidPath, (Component)unityObjectIdentification.unityObject);
            }

            return true;
        }

        private bool RemoveSavableFromLookup(Savable savable)
        {
            if (!_trackedSavables.Remove(savable.SceneGuid)) return false;
            
            var savableGuid = new GuidPath(SceneName, savable.SceneGuid);
            
            GuidToSavableGameObjectLookup.Remove(savableGuid);
            SavableGameObjectToGuidLookup.Remove(savable.gameObject);
            
            /*
             * Savables can be removed safely here, because the system is currently not designed to support adding of new
             * savable-components during runtime.
             */
            if (ComponentToGuidLookup != null)
            {
                foreach (var unityObjectIdentification in savable.DuplicateComponentLookup)
                {
                    ComponentToGuidLookup.Remove((Component)unityObjectIdentification.unityObject);
                    
                    var componentGuidPath = new GuidPath(savableGuid, unityObjectIdentification.guid);
                    GuidToComponentLookup.Remove(componentGuidPath);
                }
            }

            if (ComponentToGuidLookup != null)
            {
                foreach (var unityObjectIdentification in savable.SavableLookup)
                {
                    ComponentToGuidLookup.Remove((Component)unityObjectIdentification.unityObject);
                    
                    var componentGuidPath = new GuidPath(savableGuid, unityObjectIdentification.guid);
                    GuidToComponentLookup.Remove(componentGuidPath);
                }
            }

            return true;
        }
        
        
        #endregion
        
        #region Event System
        
        
        internal virtual void OnBeforeDeleteDiskData()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<ISaveMateBeforeDeleteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeDeleteDiskData();
                }
            }
        }
        
        internal virtual void OnAfterDeleteDiskData()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<ISaveMateAfterDeleteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnAfterDeleteDiskData();
                }
            }
        }
        
        internal virtual void OnBeforeWriteToDisk()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<ISaveMateBeforeWriteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeWriteToDisk();
                }
            }
        }
        
        internal virtual void OnAfterWriteToDisk()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<ISaveMateAfterWriteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnAfterWriteToDisk();
                }
            }
        }

        
        #endregion


        #region CaptureSnapshot

        
        public virtual void OnBeforeCaptureSnapshot()
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
        }
        
        public void CaptureSnapshot(SaveLoadManager saveLoadManager)
        {
            var saveLink = saveLoadManager.CurrentSaveFileContext;
            
            var branchSaveData = CreateBranchSaveData(saveLink.RootSaveData, saveLink, saveLoadManager);
                
            var sceneData = new SceneData 
            {
                ActivePrefabs = GetExistingPrefabsGuid(saveLoadManager.GuidToSavablePrefabsLookup), 
                ActiveSaveData = branchSaveData
            };
                
            saveLink.RootSaveData.SetSceneData(SceneName, sceneData);
        }

        public virtual void OnAfterCaptureSnapshot()
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
        }
        
        private BranchSaveData CreateBranchSaveData(RootSaveData rootSaveData, SaveFileContext saveFileContext, SaveLoadManager saveLoadManager)
        {
            //save data
            var branchSaveData = new BranchSaveData();
            
            foreach (var saveObject in _trackedSavables.Values)
            {
                var savableGuidPath = new GuidPath(SceneName, saveObject.SceneGuid);
                foreach (var componentContainer in saveObject.SavableLookup)
                {
                    if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) continue;
            
                    var guidPath = new GuidPath(savableGuidPath, componentContainer.guid);
                    var leafSaveData = new LeafSaveData();
                    branchSaveData.UpsertLeafSaveData(guidPath, leafSaveData);
                    
                    targetSavable.OnSave(new SaveDataHandler(rootSaveData, leafSaveData, guidPath, SceneName, saveFileContext, saveLoadManager));
                }
            }

            return branchSaveData;
        }

        
        #endregion
        
        #region RestoreSnapshot
        
        
        public virtual void OnBeforeRestoreSnapshot()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<ISaveMateBeforeLoadHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeRestoreSnapshot();
                }
            }
        }

        public void OnPrepareSnapshotObjects(SaveLoadManager saveLoadManager, LoadType loadType)
        {
            var saveLink = saveLoadManager.CurrentSaveFileContext;
            
            var currentSavePrefabGuidGroup = GetExistingPrefabsGuid(saveLoadManager.GuidToSavablePrefabsLookup);
            SyncScenePrefabsOnLoad(saveLink.RootSaveData, saveLoadManager.GuidToSavablePrefabsLookup, currentSavePrefabGuidGroup);
        }

        public void RestoreSnapshot(SaveLoadManager saveLoadManager, LoadType loadType)
        {
            var saveLink = saveLoadManager.CurrentSaveFileContext;
            
            if (saveLink.RootSaveData.TryGetSceneData(SceneName, out var sceneData))
            {
                LoadBranchSaveData(saveLink.RootSaveData, sceneData.ActiveSaveData, loadType, 
                    saveLoadManager.CurrentSaveFileContext, saveLoadManager);
            }
        }
        
        public virtual void OnAfterRestoreSnapshot()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                if (savable.IsUnityNull()) continue;
                
                var beforeLoadHandlers = savable.GetComponents<ISaveMateAfterLoadHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnAfterRestoreSnapshot();
                }
            }
        }

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

        private void LoadBranchSaveData(RootSaveData rootSaveData, BranchSaveData branchSaveData, LoadType loadType, 
            SaveFileContext saveFileContext, SaveLoadManager saveLoadManager)
        {
            //iterate over GameObjects with savable component
            foreach (var savable in _trackedSavables.Values)
            {
                var savableGuidPath = new GuidPath(SceneName, savable.SceneGuid);
                
                foreach (var savableComponent in savable.SavableLookup)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath, savableComponent.guid);

                    if (branchSaveData.TryGetLeafSaveData(componentGuidPath, out var instanceSaveData))
                    {
                        if (!TypeUtility.TryConvertTo(savableComponent.unityObject, out ISavable targetSavable)) return;
                    
                        var loadDataHandler = new LoadDataHandler(rootSaveData, branchSaveData, instanceSaveData, loadType, 
                            SceneName, saveFileContext, saveLoadManager);
                        
                        targetSavable.OnLoad(loadDataHandler);
                    }
                }
            }
        }

        
        #endregion
        
        #region Prefab Handling

        
        private PrefabGuidGroup GetExistingPrefabsGuid(Dictionary<string, Savable> guidToSavablePrefabsLookup)
        {
            return new PrefabGuidGroup
            {
                Guids = (from savable in _trackedSavables.Values
                        where !savable.DynamicPrefabSpawningDisabled &&
                              guidToSavablePrefabsLookup.ContainsKey(savable.PrefabGuid)
                        select new SavablePrefabElement { PrefabGuid = savable.PrefabGuid, SceneGuid = savable.SceneGuid })
                    .ToHashSet()
            };
        }
        
        
        #endregion
        
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
    }

    internal interface IGetCaptureSnapshotGroupElementHandler
    {
        List<ICaptureSnapshotGroupElement> GetCaptureSnapshotGroupElements();
    }

    internal interface IGetRestoreSnapshotGroupElementHandler
    {
        List<IRestoreSnapshotGroupElement> GetRestoreSnapshotGroupElements();
    }

    public interface ICaptureSnapshotGroupElement
    {
        string SceneName { get; }
        
        void CaptureSnapshot(SaveLoadManager saveLoadManager);
    }
    
    public interface IRestoreSnapshotGroupElement
    {
        string SceneName { get; }
        
        void OnPrepareSnapshotObjects(SaveLoadManager saveLoadManager, LoadType loadType);
        
        void RestoreSnapshot(SaveLoadManager saveLoadManager, LoadType loadType);
    }
}
