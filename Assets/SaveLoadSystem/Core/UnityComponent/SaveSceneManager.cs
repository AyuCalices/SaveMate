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

        private Dictionary<string, Savable> _saveObjectLookup;
        private static bool _isQuitting;

        [InitializeOnLoad]
        private static class SaveObjectDestructionUpdater
        {
            static SaveObjectDestructionUpdater()
            {
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
            }
            
            private static void OnHierarchyChanged()
            {
                var saveSceneManagers = FindObjectsOfType<SaveSceneManager>();

                foreach (var saveSceneManager in saveSceneManagers)
                {
                    if (saveSceneManager._saveObjectLookup == null) return;
                    
                    //remove invalid objects
                    List<string> keysToRemove = new();
                    foreach (var (key, value) in saveSceneManager._saveObjectLookup)
                    {
                        if (value.IsUnityNull())
                        {
                            keysToRemove.Add(key);
                        }
                    }

                    foreach (var key in keysToRemove)
                    {
                        saveSceneManager._saveObjectLookup.Remove(key);
                    }
                    
                    //if a object is getting unlinked -> reset prefab path
                    foreach (var savable in saveSceneManager._saveObjectLookup.Values)
                    {
                        if (PrefabUtility.GetPrefabInstanceStatus(savable) != PrefabInstanceStatus.Connected)
                        {
                            savable.SetPrefabPath(null);
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
            if (string.IsNullOrEmpty(savable.SceneGuid) || 
                (_saveObjectLookup != null && _saveObjectLookup.TryGetValue(savable.SceneGuid, out var registeredSavable) && registeredSavable != savable))
            {
                var id = GetUniqueID(savable);
                savable.SetSceneGuidGroup(id);
            }

            // Add the Savable to the lookup, ensuring it is tracked
            AddSavable(savable.SceneGuid, savable);
        }

        internal void UnregisterSavable(Savable savable)
        {
            RemoveSavable(savable.SceneGuid);
            savable.SetSceneGuidGroup(null);
        }

        private string GetUniqueID(Savable savable)
        {
            var guid = "GameObject_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            
            while (_saveObjectLookup != null && _saveObjectLookup.ContainsKey(guid))
            {
                guid = "GameObject_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            }

            return guid;
        }

        private bool AddSavable(string id, Savable savable)
        {
            _saveObjectLookup ??= new Dictionary<string, Savable>();

            return _saveObjectLookup.TryAdd(id, savable);
        }

        private bool RemoveSavable(string id)
        {
            return _saveObjectLookup != null && _saveObjectLookup.Remove(id);
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
            foreach (var savable in _saveObjectLookup.Values)
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
            foreach (var savable in _saveObjectLookup.Values)
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
            foreach (var savable in _saveObjectLookup.Values)
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
            foreach (var savable in _saveObjectLookup.Values)
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
            foreach (var savable in _saveObjectLookup.Values)
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
            foreach (var savable in _saveObjectLookup.Values)
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
            foreach (var savable in _saveObjectLookup.Values)
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
            foreach (var savable in _saveObjectLookup.Values)
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
                savePrefabs = CreateSavablePrefabLookup(_saveObjectLookup);
            }
            
            var sceneSaveData = new SceneSaveData(instanceSaveDataLookup, savePrefabs);
            var assetLookup = BuildAssetToPathLookup();
            var gameObjectLookup = BuildGameObjectToPathLookup(_saveObjectLookup);
            var guidComponentLookup = BuildGuidComponentToPathLookup(_saveObjectLookup);
            
            //core saving
            LazySave(instanceSaveDataLookup, _saveObjectLookup, assetLookup, gameObjectLookup, guidComponentLookup);
            return sceneSaveData;
        }

        private Dictionary<GameObject, GuidPath> BuildGameObjectToPathLookup(Dictionary<string, Savable> saveObjectLookup)
        {
            var unityObjectLookup = new Dictionary<GameObject, GuidPath>();
            
            //iterate over all gameobject with the savable component
            foreach (var savable in saveObjectLookup.Values)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                unityObjectLookup.Add(savable.gameObject, savableGuidPath);
            }

            return unityObjectLookup;
        }

        private Dictionary<UnityEngine.Object, GuidPath> BuildAssetToPathLookup()
        {
            var unityObjectLookup = new Dictionary<UnityEngine.Object, GuidPath>();
            
            //iterate over all elements inside the asset registry
            if (assetRegistry != null)
            {
                foreach (var componentsContainer in assetRegistry.GetSavableAssets())
                {
                    var componentGuidPath = new GuidPath(componentsContainer.guid);
                    unityObjectLookup.Add(componentsContainer.unityObject, componentGuidPath);
                }
            }

            return unityObjectLookup;
        }

        private Dictionary<Component, GuidPath> BuildGuidComponentToPathLookup(Dictionary<string, Savable> saveObjectLookup)
        {
            var guidComponentLookup = new Dictionary<Component, GuidPath>();
            
            //iterate over all gameobject with the savable component
            foreach (var savable in saveObjectLookup.Values)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                
                foreach (var unityObjectIdentification in savable.DuplicateComponentLookup)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.TargetGuid, unityObjectIdentification.guid);
                    guidComponentLookup.Add((Component)unityObjectIdentification.unityObject, componentGuidPath);
                }
                
                foreach (var unityObjectIdentification in savable.SavableLookup)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.TargetGuid, unityObjectIdentification.guid);
                    guidComponentLookup.Add((Component)unityObjectIdentification.unityObject, componentGuidPath);
                }
            }

            return guidComponentLookup;
        }
        
        private void LazySave(Dictionary<GuidPath, InstanceSaveData> instanceSaveDataLookup, Dictionary<string, Savable> saveObjectLookup, 
            Dictionary<UnityEngine.Object, GuidPath> assetLookup, Dictionary<GameObject, GuidPath> gameObjectLookup, 
            Dictionary<Component, GuidPath> guidComponentLookup)
        {
            var processedInstancesLookup = new Dictionary<object, GuidPath>();
            
            //iterate over ScriptableObjects
            if (assetRegistry != null)
            {
                foreach (var unityObjectIdentification in assetRegistry.ScriptableObjectSavables)
                {
                    var guidPath = new GuidPath(unityObjectIdentification.guid);
                    var instanceSaveData = new InstanceSaveData();

                    instanceSaveDataLookup.Add(guidPath, instanceSaveData);

                    if (!TypeUtility.TryConvertTo(unityObjectIdentification.unityObject, out ISavable targetSavable)) return;

                    targetSavable.OnSave(new SaveDataHandler(guidPath, instanceSaveData, instanceSaveDataLookup,
                        processedInstancesLookup, assetLookup, gameObjectLookup, guidComponentLookup));
                }
            }

            //iterate over GameObjects with savable component
            foreach (var saveObject in saveObjectLookup.Values)
            {
                var savableGuidPath = new GuidPath(saveObject.SceneGuid);
                foreach (var componentContainer in saveObject.SavableLookup)
                {
                    var guidPath = new GuidPath(savableGuidPath.TargetGuid, componentContainer.guid);
                    var instanceSaveData = new InstanceSaveData();
                
                    instanceSaveDataLookup.Add(guidPath, instanceSaveData);
                    
                    if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
            
                    targetSavable.OnSave(new SaveDataHandler(guidPath, instanceSaveData, instanceSaveDataLookup, 
                        processedInstancesLookup, assetLookup, gameObjectLookup, guidComponentLookup));
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
                savePrefabs = CreateSavablePrefabLookup(_saveObjectLookup);
                InstantiatePrefabsOnLoad(sceneSaveData, savePrefabs);
            }

            //core loading
            var assetLookup = BuildPathToAssetLookup();
            var guidComponentLookup = BuildPathToGuidComponentLookup(_saveObjectLookup);

            LazyLoad(sceneSaveData, _saveObjectLookup, new Dictionary<GuidPath, object>(), 
                assetLookup, guidComponentLookup);
            
            //destroy prefabs, that are not present in the save file
            if (savePrefabs != null)
            {
                DestroyPrefabsOnLoad(sceneSaveData, _saveObjectLookup, savePrefabs);
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
                    savable.SetSceneGuidGroup(prefabsSavable.SceneGuid);
                    Instantiate(savable);
                    savable.SetSceneGuidGroup(null);
                }
            }
        }
        
        private Dictionary<string, UnityEngine.Object> BuildPathToAssetLookup()
        {
            var assetLookup = new Dictionary<string, UnityEngine.Object>();
            
            if (assetRegistry != null)
            {
                foreach (var componentsContainer in assetRegistry.GetSavableAssets())
                {
                    assetLookup.Add(componentsContainer.guid, componentsContainer.unityObject);
                }
            }

            return assetLookup;
        }
        
        private Dictionary<string, Component> BuildPathToGuidComponentLookup(Dictionary<string, Savable> saveObjectLookup)
        {
            var guidComponentLookup = new Dictionary<string, Component>();
            
            foreach (var savable in saveObjectLookup.Values)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                foreach (var componentContainer in savable.DuplicateComponentLookup)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.TargetGuid, componentContainer.guid);
                    guidComponentLookup.Add(componentGuidPath.ToString(), (Component)componentContainer.unityObject);
                }
                
                foreach (var componentContainer in savable.SavableLookup)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.TargetGuid, componentContainer.guid);
                    guidComponentLookup.Add(componentGuidPath.ToString(), (Component)componentContainer.unityObject);
                }
            }

            return guidComponentLookup;
        }
        
        private void LazyLoad(SceneSaveData sceneSaveData, Dictionary<string, Savable> saveObjectLookup, 
            Dictionary<GuidPath, object> createdObjectsLookup, Dictionary<string, UnityEngine.Object> assetLookup, 
            Dictionary<string, Component> guidComponentLookup)
        {
            //iterate over ScriptableObjects
            if (assetRegistry != null)
            {
                foreach (var componentContainer in assetRegistry.ScriptableObjectSavables)
                {
                    var guidPath = new GuidPath(componentContainer.guid);
                
                    if (sceneSaveData.InstanceSaveDataLookup.TryGetValue(guidPath, out var instanceSaveData))
                    {
                        var loadDataHandler = new LoadDataHandler(sceneSaveData, instanceSaveData, 
                            createdObjectsLookup, assetLookup, saveObjectLookup, guidComponentLookup);
                        
                        if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
                    
                        targetSavable.OnLoad(loadDataHandler);
                    }
                }
            }
            
            //iterate over GameObjects with savable component
            foreach (var savable in saveObjectLookup.Values)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                
                foreach (var savableComponent in savable.SavableLookup)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.TargetGuid, savableComponent.guid);

                    if (sceneSaveData.InstanceSaveDataLookup.TryGetValue(componentGuidPath, out var instanceSaveData))
                    {
                        var loadDataHandler = new LoadDataHandler(sceneSaveData, instanceSaveData, 
                            createdObjectsLookup, assetLookup, saveObjectLookup, guidComponentLookup);
                        
                        if (!TypeUtility.TryConvertTo(savableComponent.unityObject, out ISavable targetSavable)) return;
                    
                        targetSavable.OnLoad(loadDataHandler);
                    }
                }
            }
        }
        
        private void DestroyPrefabsOnLoad(SceneSaveData sceneSaveData, Dictionary<string, Savable> savableComponents, List<SavablePrefabElement> savePrefabs)
        {
            var destroyedSavables = savePrefabs.Except(sceneSaveData.SavePrefabs);
            foreach (var prefabsSavable in destroyedSavables)
            {
                Destroy(savableComponents[prefabsSavable.SceneGuid]);
            }
        }

        
        #endregion
        
        #region Save and Load Helper

        
        private List<SavablePrefabElement> CreateSavablePrefabLookup(Dictionary<string, Savable> savableComponents)
        {
            return (from savable in savableComponents.Values 
                where !savable.DynamicPrefabSpawningDisabled && assetRegistry.ContainsPrefabGuid(savable.PrefabGuid) 
                select new SavablePrefabElement(savable.PrefabGuid, savable.SceneGuid)).ToList();
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
