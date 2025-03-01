using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.EventHandler;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.UnityComponent
{
    public class SaveSceneManager : MonoBehaviour
    {
        [SerializeField] private SaveLoadManager saveLoadManager;
        [SerializeField] private AssetRegistry assetRegistry;
        
        [Header("Current Scene Events")]
        [SerializeField] private bool loadSceneOnAwake;
        [SerializeField] private SaveSceneManagerDestroyType saveSceneOnDestroy;
        [SerializeField] private SceneManagerEvents sceneManagerEvents;
        
        private static bool _isQuitting;

        public Scene Scene => gameObject.scene;
        public AssetRegistry AssetRegistry => assetRegistry;
        
        private readonly Dictionary<string, Savable> _trackedSavables = new();
        
        internal readonly Dictionary<GameObject, GuidPath> SavableGameObjectToGuidLookup = new();
        internal readonly Dictionary<ScriptableObject, GuidPath> ScriptableObjectToGuidLookup = new();
        internal readonly Dictionary<Component, GuidPath> ComponentToGuidLookup = new();
        
        internal readonly Dictionary<GuidPath, GameObject> GuidToSavableGameObjectLookup = new();
        internal readonly Dictionary<GuidPath, ScriptableObject> GuidToScriptableObjectLookup = new();
        internal readonly Dictionary<GuidPath, Component> GuidToComponentLookup = new();
        

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
                
                var saveSceneManagers = FindObjectsOfType<SaveSceneManager>();

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
        
        #region Unity Lifecycle
        
        
        private void Awake()
        {
            if (assetRegistry == null)
            {
                Debug.LogWarning($"You didn't add an {nameof(AssetRegistry)}. ScriptableObjects and Dynamic Prefab loading is not supported!");
            }
            else
            {
                foreach (var scriptableObjectSavable in assetRegistry.ScriptableObjectSavables)
                {
                    var guidPath = new GuidPath(RootSaveData.GlobalSaveDataName, scriptableObjectSavable.guid);
                    ScriptableObjectToGuidLookup.Add((ScriptableObject)scriptableObjectSavable.unityObject, guidPath);
                    GuidToScriptableObjectLookup.Add(guidPath, (ScriptableObject)scriptableObjectSavable.unityObject);
                }
            }
            
            saveLoadManager.RegisterSaveSceneManager(this);

            if (loadSceneOnAwake)
            {
                LoadScene();
            }
        }
        
        private void OnDestroy()
        {
            if (!_isQuitting)
            {
                switch (saveSceneOnDestroy)
                {
                    case SaveSceneManagerDestroyType.SnapshotScene:
                        SnapshotScene();
                        break;
                    case SaveSceneManagerDestroyType.SaveScene:
                        SaveScene();
                        break;
                    case SaveSceneManagerDestroyType.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            saveLoadManager.UnregisterSaveSceneManager(this);
            //Debug.Log("destroy remove " + saveLoadManager.TrackedSaveSceneManagers.Count);
        }

        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            saveLoadManager.RegisterSaveSceneManager(this);
            //Debug.Log("validate add " + saveLoadManager.TrackedSaveSceneManagers.Count);
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
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
            // Case 1: If the object has no SceneGuid, generate a unique ID
            // Case 2: If an object with the same SceneGuid exists but is different, assign a new unique ID
            if (string.IsNullOrEmpty(savable.SavableGuid) || 
                (_trackedSavables != null && _trackedSavables.TryGetValue(savable.SavableGuid, out var registeredSavable) && registeredSavable != savable))
            {
                var id = GetUniqueID(savable);
                savable.SavableGuid = id;
            }

            // Add the Savable to the lookup, ensuring it is tracked
            AddSavable(savable.SavableGuid, savable);
        }

        internal void UnregisterSavable(Savable savable)
        {
            RemoveSavable(savable);
            savable.SavableGuid = null;
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
        
        
        private bool AddSavable(string id, Savable savable)
        {
            //savable lookup registration
            if (!_trackedSavables.TryAdd(id, savable)) return false;
            
            var savableGuid = new GuidPath(Scene.name, savable.SavableGuid);
            
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

        private bool RemoveSavable(Savable savable)
        {
            if (!_trackedSavables.Remove(savable.SavableGuid)) return false;
            
            var savableGuid = new GuidPath(Scene.name, savable.SavableGuid);
            
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
        
        #region SaveLoad Methods


        [ContextMenu("Snapshot Scene")]
        public void SnapshotScene()
        {
            saveLoadManager.SaveFocus.SnapshotScenes(this);
        }

        [ContextMenu("Write To Disk")]
        public void WriteToDisk()
        {
            saveLoadManager.SaveFocus.WriteToDisk();
        }
        
        [ContextMenu("Save Scene")]
        public void SaveScene()
        {
            saveLoadManager.SaveFocus.SaveScenes(this);
        }

        [ContextMenu("Apply Snapshot")]
        public void ApplySnapshot()
        {
            saveLoadManager.SaveFocus.ApplySnapshotToScenes(this);
        }
        
        [ContextMenu("Load Scene")]
        public void LoadScene()
        {
            saveLoadManager.SaveFocus.LoadScenes(this);
        }
        
        [ContextMenu("Wipe Scene Data")]
        public void WipeSceneData()
        {
            saveLoadManager.SaveFocus.DeleteSceneData(this);
        }
        
        [ContextMenu("Delete Scene Data")]
        public void DeleteSceneData()
        {
            saveLoadManager.SaveFocus.DeleteAll(this);
        }

        [ContextMenu("Reload Then Load Scene")]
        public void ReloadThenLoadScene()
        {
            saveLoadManager.SaveFocus.ReloadThenLoadScenes(this);
        }
        
        
        #endregion
        
        #region Event System
        
        
        public void HandleBeforeSnapshot()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                var beforeSnapshotHandlers = savable.GetComponents<ISaveMateBeforeSnapshotHandler>();
                foreach (var beforeSnapshotHandler in beforeSnapshotHandlers)
                {
                    beforeSnapshotHandler.OnBeforeSnapshot();
                }
            }
            
            sceneManagerEvents.onBeforeSnapshot.Invoke();
        }
        
        public void HandleAfterSnapshot()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                var beforeSnapshotHandlers = savable.GetComponents<ISaveMateAfterSnapshotHandler>();
                foreach (var beforeSnapshotHandler in beforeSnapshotHandlers)
                {
                    beforeSnapshotHandler.OnAfterSnapshot();
                }
            }
            
            sceneManagerEvents.onAfterSnapshot.Invoke();
        }
        
        public void HandleBeforeLoad()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                var beforeLoadHandlers = savable.GetComponents<ISaveMateBeforeLoadHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeLoad();
                }
            }
            
            sceneManagerEvents.onBeforeLoad.Invoke();
        }
        
        public void HandleAfterLoad()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                var beforeLoadHandlers = savable.GetComponents<ISaveMateAfterLoadHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnAfterLoad();
                }
            }
            
            sceneManagerEvents.onAfterLoad.Invoke();
        }
        
        public void HandleBeforeDeleteDiskData()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                var beforeLoadHandlers = savable.GetComponents<ISaveMateBeforeDeleteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeDeleteDiskData();
                }
            }
            
            sceneManagerEvents.onBeforeDeleteDiskData.Invoke();
        }
        
        public void HandleAfterDeleteDiskData()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                var beforeLoadHandlers = savable.GetComponents<ISaveMateAfterDeleteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnAfterDeleteDiskData();
                }
            }
            
            sceneManagerEvents.onAfterDeleteDiskData.Invoke();
        }
        
        public void HandleBeforeWriteToDisk()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                var beforeLoadHandlers = savable.GetComponents<ISaveMateBeforeWriteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnBeforeWriteToDisk();
                }
            }
            
            sceneManagerEvents.onBeforeWriteToDisk.Invoke();
        }
        
        public void HandleAfterWriteToDisk()
        {
            foreach (var savable in _trackedSavables.Values)
            {
                var beforeLoadHandlers = savable.GetComponents<ISaveMateAfterWriteDiskHandler>();
                foreach (var saveMateBeforeLoadHandler in beforeLoadHandlers)
                {
                    saveMateBeforeLoadHandler.OnAfterWriteToDisk();
                }
            }
            
            sceneManagerEvents.onAfterWriteToDisk.Invoke();
        }

        
        #endregion

        #region Save Methods
        
        
        internal SceneData CreateSnapshot(Dictionary<object, GuidPath> processedObjectLookup)
        {
            //prefabs
            PrefabGuidGroup prefabGuidGroup = default;
            if (assetRegistry != null)
            {
                prefabGuidGroup = CreateScenePrefabGroup();
            }
            
            //save data
            var sceneSaveData = new BranchSaveData();
            LazySave(sceneSaveData, processedObjectLookup);
            
            return new SceneData 
            {
                ActivePrefabs = prefabGuidGroup, 
                ActiveSaveData = sceneSaveData
            };
        }
        
        private void LazySave(BranchSaveData branchSaveData, Dictionary<object, GuidPath> processedObjectLookup)
        {
            //iterate over GameObjects with savable component
            foreach (var saveObject in _trackedSavables.Values)
            {
                var savableGuidPath = new GuidPath(Scene.name, saveObject.SavableGuid);
                foreach (var componentContainer in saveObject.SavableLookup)
                {
                    var guidPath = new GuidPath(savableGuidPath, componentContainer.guid);
                    var instanceSaveData = new LeafSaveData();
                
                    branchSaveData.AddLeafSaveData(guidPath, instanceSaveData);
                    
                    if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
            
                    targetSavable.OnSave(new SaveDataHandler(branchSaveData, guidPath, instanceSaveData, processedObjectLookup, 
                        SavableGameObjectToGuidLookup, ScriptableObjectToGuidLookup, ComponentToGuidLookup));
                }
            }
        }
        
        
        #endregion

        #region LoadMethods
        
        
        internal void LoadSnapshot(SceneData loadedSceneData, Dictionary<GuidPath, object> globallyCreatedObjects)
        {
            PrefabGuidGroup currentSavePrefabGuidGroup = default;
            if (assetRegistry != null)
            {
                //instantiating needed prefabs must happen before performing the core load methods, so it doesnt miss out on their Savable Components
                currentSavePrefabGuidGroup = CreateScenePrefabGroup();
                InstantiatePrefabsOnLoad(loadedSceneData.ActivePrefabs, currentSavePrefabGuidGroup);
            }

            LazyLoad(loadedSceneData.ActiveSaveData, globallyCreatedObjects);
            
            //destroy prefabs, that are not present in the save file
            if (assetRegistry != null)
            {
                DestroyPrefabsOnLoad(loadedSceneData.ActivePrefabs, currentSavePrefabGuidGroup);
            }
        }

        private void InstantiatePrefabsOnLoad(PrefabGuidGroup loadedPrefabGuidGroup, PrefabGuidGroup currentPrefabGuidGroup)
        {
            var instantiatedSavables = loadedPrefabGuidGroup.Except(currentPrefabGuidGroup);
            foreach (var prefabsSavable in instantiatedSavables)
            {
                if (assetRegistry.TryGetPrefab(prefabsSavable.PrefabGuid, out Savable savable))
                {
                    /*
                     * When a savable gets instantiated, it will register itself to this SaveSceneManager and apply an ID.
                     * In order to use the ID that got saved, it must be applied before instantiation. Since this is done on
                     * the prefab, it must be undone on the prefab after instantiation.
                     */
                    savable.SavableGuid = prefabsSavable.SavableGuid;
                    var obj = Instantiate(savable);
                    savable.SavableGuid = null;
                }
            }
        }
        
        private void LazyLoad(BranchSaveData branchSaveData, Dictionary<GuidPath, object> globallyCreatedObjects)
        {
            //iterate over GameObjects with savable component
            foreach (var savable in _trackedSavables.Values)
            {
                var savableGuidPath = new GuidPath(Scene.name, savable.SavableGuid);
                
                foreach (var savableComponent in savable.SavableLookup)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath, savableComponent.guid);

                    if (branchSaveData.TryGetLeafSaveData(componentGuidPath, out var instanceSaveData))
                    {
                        var loadDataHandler = new LoadDataHandler(branchSaveData, instanceSaveData, 
                            globallyCreatedObjects, GuidToSavableGameObjectLookup, GuidToScriptableObjectLookup, GuidToComponentLookup);
                        
                        if (!TypeUtility.TryConvertTo(savableComponent.unityObject, out ISavable targetSavable)) return;
                    
                        targetSavable.OnLoad(loadDataHandler);
                    }
                }
            }
        }
        
        private void DestroyPrefabsOnLoad(PrefabGuidGroup storedPrefabGuidGroup, PrefabGuidGroup currentPrefabGuidGroup)
        {
            var destroyedSavables = currentPrefabGuidGroup.Except(storedPrefabGuidGroup);
            foreach (var prefabsSavable in destroyedSavables)
            {
                Destroy(_trackedSavables[prefabsSavable.PrefabGuid].gameObject);
            }
        }

        
        #endregion
        
        #region Save and Load Helper

        
        private PrefabGuidGroup CreateScenePrefabGroup()
        {
            return new PrefabGuidGroup
            {
                Guids = (from savable in _trackedSavables.Values 
                    where !savable.DynamicPrefabSpawningDisabled && assetRegistry.ContainsPrefabGuid(savable.PrefabGuid) 
                    select new SavablePrefabElement { PrefabGuid = savable.PrefabGuid, SavableGuid = savable.SavableGuid})
                    .ToHashSet()
            };
        }

        
        #endregion
        
        #region Events
        
        
        [Serializable]
        private class SceneManagerEvents
        {
            public UnityEvent onBeforeSnapshot;
            public UnityEvent onAfterSnapshot;
            
            public UnityEvent onBeforeLoad;
            public UnityEvent onAfterLoad;
            
            public UnityEvent onBeforeDeleteDiskData;
            public UnityEvent onAfterDeleteDiskData;
            
            public UnityEvent onBeforeWriteToDisk;
            public UnityEvent onAfterWriteToDisk;
        }
        
        public void RegisterAction(UnityAction action, SceneManagerEventType firstEventType, params SceneManagerEventType[] additionalEventTypes)
        {
            foreach (var selectionViewEventType in additionalEventTypes.Append(firstEventType))
            {
                switch (selectionViewEventType)
                {
                    case SceneManagerEventType.OnBeforeSnapshot:
                        sceneManagerEvents.onBeforeSnapshot.AddListener(action);
                        break;
                    case SceneManagerEventType.OnAfterSnapshot:
                        sceneManagerEvents.onAfterSnapshot.AddListener(action);
                        break;
                    case SceneManagerEventType.OnBeforeLoad:
                        sceneManagerEvents.onBeforeLoad.AddListener(action);
                        break;
                    case SceneManagerEventType.OnAfterLoad:
                        sceneManagerEvents.onAfterLoad.AddListener(action);
                        break;
                    case SceneManagerEventType.OnBeforeDeleteDiskData:
                        sceneManagerEvents.onBeforeDeleteDiskData.AddListener(action);
                        break;
                    case SceneManagerEventType.OnAfterDeleteDiskData:
                        sceneManagerEvents.onAfterDeleteDiskData.AddListener(action);
                        break;
                    case SceneManagerEventType.OnBeforeWriteToDisk:
                        sceneManagerEvents.onBeforeWriteToDisk.AddListener(action);
                        break;
                    case SceneManagerEventType.OnAfterWriteToDisk:
                        sceneManagerEvents.onAfterWriteToDisk.AddListener(action);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        public void UnregisterAction(UnityAction action, SceneManagerEventType firstEventType, params SceneManagerEventType[] additionalEventTypes)
        {
            foreach (var selectionViewEventType in additionalEventTypes.Append(firstEventType))
            {
                switch (selectionViewEventType)
                {
                    case SceneManagerEventType.OnBeforeSnapshot:
                        sceneManagerEvents.onBeforeSnapshot.RemoveListener(action);
                        break;
                    case SceneManagerEventType.OnAfterSnapshot:
                        sceneManagerEvents.onAfterSnapshot.RemoveListener(action);
                        break;
                    case SceneManagerEventType.OnBeforeLoad:
                        sceneManagerEvents.onBeforeLoad.RemoveListener(action);
                        break;
                    case SceneManagerEventType.OnAfterLoad:
                        sceneManagerEvents.onAfterLoad.RemoveListener(action);
                        break;
                    case SceneManagerEventType.OnBeforeDeleteDiskData:
                        sceneManagerEvents.onBeforeDeleteDiskData.RemoveListener(action);
                        break;
                    case SceneManagerEventType.OnAfterDeleteDiskData:
                        sceneManagerEvents.onAfterDeleteDiskData.RemoveListener(action);
                        break;
                    case SceneManagerEventType.OnBeforeWriteToDisk:
                        sceneManagerEvents.onBeforeWriteToDisk.RemoveListener(action);
                        break;
                    case SceneManagerEventType.OnAfterWriteToDisk:
                        sceneManagerEvents.onAfterWriteToDisk.RemoveListener(action);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        
        #endregion
    }
}
