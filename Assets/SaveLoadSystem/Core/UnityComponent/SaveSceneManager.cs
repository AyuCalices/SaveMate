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

        private readonly Dictionary<string, Savable> _trackedSavables = new();
        
        internal readonly Dictionary<GameObject, GuidPath> SavableGameObjectToGuidLookup = new();
        internal readonly Dictionary<ScriptableObject, GuidPath> ScriptableObjectToGuidLookup = new();
        internal readonly Dictionary<Component, GuidPath> ComponentToGuidLookup = new();
        
        internal readonly Dictionary<string, GameObject> GuidToSavableGameObjectLookup = new();
        internal readonly Dictionary<string, ScriptableObject> GuidToScriptableObjectLookup = new();
        internal readonly Dictionary<string, Component> GuidToComponentLookup = new();
        

        [InitializeOnLoad]
        private static class SaveObjectDestructionUpdater
        {
            static SaveObjectDestructionUpdater()
            {
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
            }
            
            private static void OnHierarchyChanged()
            {
                if (Application.isPlaying) return;
                
                var saveSceneManagers = FindObjectsOfType<SaveSceneManager>();

                foreach (var saveSceneManager in saveSceneManagers)
                {
                    if (saveSceneManager._trackedSavables == null) return;
                    
                    //remove invalid objects
                    List<string> keysToRemove = new();
                    foreach (var (key, value) in saveSceneManager._trackedSavables)
                    {
                        if (value.IsUnityNull())
                        {
                            keysToRemove.Add(key);
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
                    var guidPath = new GuidPath(scriptableObjectSavable.guid);
                    ScriptableObjectToGuidLookup.Add((ScriptableObject)scriptableObjectSavable.unityObject, guidPath);
                    GuidToScriptableObjectLookup.Add(scriptableObjectSavable.guid, (ScriptableObject)scriptableObjectSavable.unityObject);
                }
            }
            
            saveLoadManager.RegisterSaveSceneManager(gameObject.scene, this);

            if (loadSceneOnAwake)
            {
                LoadScene();
            }
        }
        
        private void OnEnable()
        {
            saveLoadManager.RegisterSaveSceneManager(gameObject.scene, this);
        }
        
        private void OnDisable()
        {
            saveLoadManager.UnregisterSaveSceneManager(gameObject.scene);
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            saveLoadManager.RegisterSaveSceneManager(gameObject.scene, this);
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
            
            saveLoadManager.UnregisterSaveSceneManager(gameObject.scene);
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
            
            GuidToSavableGameObjectLookup.Add(id, savable.gameObject);
            
            var savableGuid = new GuidPath(savable.SavableGuid);
            SavableGameObjectToGuidLookup.TryAdd(savable.gameObject, savableGuid);
            
            //Savable-Component registration.
            foreach (var unityObjectIdentification in savable.DuplicateComponentLookup)
            {
                var componentGuidPath = new GuidPath(savableGuid.TargetGuid, unityObjectIdentification.guid);
                ComponentToGuidLookup.TryAdd((Component)unityObjectIdentification.unityObject, componentGuidPath);
                GuidToComponentLookup.TryAdd(componentGuidPath.ToString(), (Component)unityObjectIdentification.unityObject);
            }
                
            foreach (var unityObjectIdentification in savable.SavableLookup)
            {
                var componentGuidPath = new GuidPath(savableGuid.TargetGuid, unityObjectIdentification.guid);
                ComponentToGuidLookup.TryAdd((Component)unityObjectIdentification.unityObject, componentGuidPath);
                GuidToComponentLookup.TryAdd(componentGuidPath.ToString(), (Component)unityObjectIdentification.unityObject);
            }

            return true;
        }

        private bool RemoveSavable(Savable savable)
        {
            if (!_trackedSavables.Remove(savable.SavableGuid)) return false;
            
            GuidToSavableGameObjectLookup.Remove(savable.SavableGuid);
            SavableGameObjectToGuidLookup.Remove(savable.gameObject);
            
            /*
             * Savables can be removed safely here, because the system is currently not designed to support adding of
             * savable-components during runtime.
             */
            var savableGuid = new GuidPath(savable.SavableGuid);
            if (ComponentToGuidLookup != null)
            {
                foreach (var unityObjectIdentification in savable.DuplicateComponentLookup)
                {
                    ComponentToGuidLookup.Remove((Component)unityObjectIdentification.unityObject);
                    
                    var componentGuidPath = new GuidPath(savableGuid.TargetGuid, unityObjectIdentification.guid);
                    GuidToComponentLookup.Remove(componentGuidPath.ToString());
                }
            }

            if (ComponentToGuidLookup != null)
            {
                foreach (var unityObjectIdentification in savable.SavableLookup)
                {
                    ComponentToGuidLookup.Remove((Component)unityObjectIdentification.unityObject);
                    
                    var componentGuidPath = new GuidPath(savableGuid.TargetGuid, unityObjectIdentification.guid);
                    GuidToComponentLookup.Remove(componentGuidPath.ToString());
                }
            }

            return true;
        }
        
        
        #endregion
        
        #region SaveLoad Methods


        [ContextMenu("Snapshot Scene")]
        public void SnapshotScene()
        {
            saveLoadManager.SaveFocus.SnapshotScenes(gameObject.scene);
        }

        [ContextMenu("Write To Disk")]
        public void WriteToDisk()
        {
            saveLoadManager.SaveFocus.WriteToDisk();
        }
        
        [ContextMenu("Save Scene")]
        public void SaveScene()
        {
            saveLoadManager.SaveFocus.SaveScenes(gameObject.scene);
        }

        [ContextMenu("Apply Snapshot")]
        public void ApplySnapshot()
        {
            saveLoadManager.SaveFocus.ApplySnapshotToScenes(gameObject.scene);
        }
        
        [ContextMenu("Load Scene")]
        public void LoadScene()
        {
            saveLoadManager.SaveFocus.LoadScenes(gameObject.scene);
        }
        
        [ContextMenu("Wipe Scene Data")]
        public void WipeSceneData()
        {
            saveLoadManager.SaveFocus.DeleteSceneData(gameObject.scene);
        }
        
        [ContextMenu("Delete Scene Data")]
        public void DeleteSceneData()
        {
            saveLoadManager.SaveFocus.DeleteAll(gameObject.scene);
        }

        [ContextMenu("Reload Then Load Scene")]
        public void ReloadThenLoadScene()
        {
            saveLoadManager.SaveFocus.ReloadThenLoadScenes(gameObject.scene);
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
        
        
        internal SceneSaveData CreateSnapshot()
        {
            //prepare data
            Dictionary<GuidPath, InstanceSaveData> instanceSaveDataLookup = new();

            List<SavablePrefabElement> savePrefabs = null;
            if (assetRegistry != null)
            {
                savePrefabs = CreateSavablePrefabLookup();
            }
            
            var sceneSaveData = new SceneSaveData(instanceSaveDataLookup, savePrefabs);
            
            //core saving
            LazySave(instanceSaveDataLookup);
            return sceneSaveData;
        }
        
        private void LazySave(Dictionary<GuidPath, InstanceSaveData> instanceSaveDataLookup)
        {
            var processedInstancesLookup = new Dictionary<object, GuidPath>();
            
            //iterate over ScriptableObjects
            //TODO: throw this out of here
            if (assetRegistry != null)
            {
                foreach (var unityObjectIdentification in assetRegistry.ScriptableObjectSavables)
                {
                    var guidPath = new GuidPath(unityObjectIdentification.guid);
                    var instanceSaveData = new InstanceSaveData();

                    instanceSaveDataLookup.Add(guidPath, instanceSaveData);

                    if (!TypeUtility.TryConvertTo(unityObjectIdentification.unityObject, out ISavable targetSavable)) return;

                    targetSavable.OnSave(new SaveDataHandler(guidPath, instanceSaveData, instanceSaveDataLookup,
                        processedInstancesLookup, this));
                }
            }

            //iterate over GameObjects with savable component
            foreach (var saveObject in _trackedSavables.Values)
            {
                var savableGuidPath = new GuidPath(saveObject.SavableGuid);
                foreach (var componentContainer in saveObject.SavableLookup)
                {
                    var guidPath = new GuidPath(savableGuidPath.TargetGuid, componentContainer.guid);
                    var instanceSaveData = new InstanceSaveData();
                
                    instanceSaveDataLookup.Add(guidPath, instanceSaveData);
                    
                    if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
            
                    targetSavable.OnSave(new SaveDataHandler(guidPath, instanceSaveData, instanceSaveDataLookup, 
                        processedInstancesLookup, this));
                }
            }
        }
        
        
        #endregion

        #region LoadMethods
        
        
        internal void LoadSnapshot(SceneSaveData sceneSaveData)
        {
            List<SavablePrefabElement> savePrefabs = null;
            if (assetRegistry != null)
            {
                //instantiating needed prefabs must happen before performing the core load methods, so it doesnt miss out on their Savable Components
                savePrefabs = CreateSavablePrefabLookup();
                InstantiatePrefabsOnLoad(sceneSaveData, savePrefabs);
            }

            LazyLoad(sceneSaveData, new Dictionary<GuidPath, object>());
            
            //destroy prefabs, that are not present in the save file
            if (savePrefabs != null)
            {
                DestroyPrefabsOnLoad(sceneSaveData, savePrefabs);
            }
        }

        private void InstantiatePrefabsOnLoad(SceneSaveData sceneSaveData, List<SavablePrefabElement> savePrefabs)
        {
            var instantiatedSavables = sceneSaveData.SavePrefabs.Except(savePrefabs);
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
        
        private void LazyLoad(SceneSaveData sceneSaveData, Dictionary<GuidPath, object> createdObjectsLookup)
        {
            //iterate over ScriptableObjects
            //TODO: throw this out of here
            if (assetRegistry != null)
            {
                foreach (var componentContainer in assetRegistry.ScriptableObjectSavables)
                {
                    var guidPath = new GuidPath(componentContainer.guid);
                
                    if (sceneSaveData.InstanceSaveDataLookup.TryGetValue(guidPath, out var instanceSaveData))
                    {
                        var loadDataHandler = new LoadDataHandler(sceneSaveData, instanceSaveData, 
                            createdObjectsLookup, this);
                        
                        if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
                    
                        targetSavable.OnLoad(loadDataHandler);
                    }
                }
            }
            
            //iterate over GameObjects with savable component
            foreach (var savable in _trackedSavables.Values)
            {
                var savableGuidPath = new GuidPath(savable.SavableGuid);
                
                foreach (var savableComponent in savable.SavableLookup)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.TargetGuid, savableComponent.guid);

                    if (sceneSaveData.InstanceSaveDataLookup.TryGetValue(componentGuidPath, out var instanceSaveData))
                    {
                        var loadDataHandler = new LoadDataHandler(sceneSaveData, instanceSaveData, 
                            createdObjectsLookup, this);
                        
                        if (!TypeUtility.TryConvertTo(savableComponent.unityObject, out ISavable targetSavable)) return;
                    
                        targetSavable.OnLoad(loadDataHandler);
                    }
                }
            }
        }
        
        private void DestroyPrefabsOnLoad(SceneSaveData sceneSaveData, List<SavablePrefabElement> savePrefabs)
        {
            var destroyedSavables = savePrefabs.Except(sceneSaveData.SavePrefabs);
            foreach (var prefabsSavable in destroyedSavables)
            {
                Destroy(_trackedSavables[prefabsSavable.PrefabGuid].gameObject);
            }
        }

        
        #endregion
        
        #region Save and Load Helper

        
        private List<SavablePrefabElement> CreateSavablePrefabLookup()
        {
            return (from savable in _trackedSavables.Values 
                where !savable.DynamicPrefabSpawningDisabled && assetRegistry.ContainsPrefabGuid(savable.PrefabGuid) 
                select new SavablePrefabElement(savable.PrefabGuid, savable.SavableGuid)).ToList();
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
