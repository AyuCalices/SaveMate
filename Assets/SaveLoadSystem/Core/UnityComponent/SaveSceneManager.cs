using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.EventHandler;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
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

        private HashSet<Savable> _savables;
        private static bool _isQuitting;
        
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

        
        internal bool RegisterSavable(Savable savable)
        {
            _savables ??= new HashSet<Savable>();
            
            return _savables.Add(savable);
        }

        internal bool UnregisterSavable(Savable savable)
        {
            _savables ??= new HashSet<Savable>();
            return _savables.Remove(savable);
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
            foreach (var savable in _savables)
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
            foreach (var savable in _savables)
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
            foreach (var savable in _savables)
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
            foreach (var savable in _savables)
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
            foreach (var savable in _savables)
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
            foreach (var savable in _savables)
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
            foreach (var savable in _savables)
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
            foreach (var savable in _savables)
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
            var savableComponents = UnityUtility.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);

            List<SavablePrefabElement> savePrefabs = null;
            if (assetRegistry != null)
            {
                savePrefabs = CreateSavablePrefabLookup(savableComponents);
            }
            
            var sceneSaveData = new SceneSaveData(instanceSaveDataLookup, savePrefabs);
            var assetLookup = BuildAssetToPathLookup();
            var gameObjectLookup = BuildGameObjectToPathLookup(savableComponents);
            var guidComponentLookup = BuildGuidComponentToPathLookup(savableComponents);
            
            //core saving
            LazySave(instanceSaveDataLookup, savableComponents, assetLookup, gameObjectLookup, guidComponentLookup);
            return sceneSaveData;
        }

        private Dictionary<GameObject, GuidPath> BuildGameObjectToPathLookup(List<Savable> savableComponents)
        {
            var unityObjectLookup = new Dictionary<GameObject, GuidPath>();
            
            //iterate over all gameobject with the savable component
            foreach (var savable in savableComponents)
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

        private Dictionary<Component, GuidPath> BuildGuidComponentToPathLookup(List<Savable> savableComponents)
        {
            var guidComponentLookup = new Dictionary<Component, GuidPath>();
            
            //iterate over all gameobject with the savable component
            foreach (var savable in savableComponents)
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
        
        private void LazySave(Dictionary<GuidPath, InstanceSaveData> instanceSaveDataLookup, List<Savable> savableComponents, 
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
            foreach (var savable in savableComponents)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                foreach (var componentContainer in savable.SavableLookup)
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
            var savableComponents = UnityUtility.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);

            List<SavablePrefabElement> savePrefabs = null;
            if (assetRegistry != null)
            {
                //instantiating needed prefabs must happen before performing the core load methods, so it doesnt miss out on their Savable Components
                savePrefabs = CreateSavablePrefabLookup(savableComponents);
                InstantiatePrefabsOnLoad(sceneSaveData, savableComponents, savePrefabs);
            }

            //core loading
            var assetLookup = BuildPathToAssetLookup();
            var gameObjectLookup = BuildPathToUnityObjectLookup(savableComponents);
            var guidComponentLookup = BuildPathToGuidComponentLookup(savableComponents);

            LazyLoad(sceneSaveData, savableComponents, new Dictionary<GuidPath, object>(), 
                assetLookup, gameObjectLookup, guidComponentLookup);
            
            //destroy prefabs, that are not present in the save file
            if (savePrefabs != null)
            {
                DestroyPrefabsOnLoad(sceneSaveData, savableComponents, savePrefabs);
            }
        }

        private void InstantiatePrefabsOnLoad(SceneSaveData sceneSaveData, List<Savable> savableComponents, List<SavablePrefabElement> savePrefabs)
        {
            var instantiatedSavables = sceneSaveData.SavePrefabs.Except(savePrefabs);
            foreach (var prefabsSavable in instantiatedSavables)
            {
                if (assetRegistry.TryGetPrefab(prefabsSavable.PrefabGuid, out Savable savable))
                {
                    var instantiatedSavable = Instantiate(savable);
                    instantiatedSavable.SetSceneGuidGroup(prefabsSavable.SceneGuid);
                    savableComponents.Add(instantiatedSavable);
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
        
        //TODO: implement duplication also for the savable components
        //TODO: merge duplicate savables with duplicate components? or keep them separated?
        private Dictionary<string, GameObject> BuildPathToUnityObjectLookup(List<Savable> savableComponents)
        {
            var unityObjectLookup = new Dictionary<string, GameObject>();
            
            foreach (var savable in savableComponents)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                unityObjectLookup.Add(savableGuidPath.ToString(), savable.gameObject);
            }

            return unityObjectLookup;
        }
        
        private Dictionary<string, Component> BuildPathToGuidComponentLookup(List<Savable> savableComponents)
        {
            var guidComponentLookup = new Dictionary<string, Component>();
            
            foreach (var savable in savableComponents)
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
        
        private void LazyLoad(SceneSaveData sceneSaveData, List<Savable> savableComponents, 
            Dictionary<GuidPath, object> createdObjectsLookup, Dictionary<string, UnityEngine.Object> assetLookup, 
            Dictionary<string, GameObject> gameObjectLookup, Dictionary<string, Component> guidComponentLookup)
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
                            createdObjectsLookup, assetLookup, gameObjectLookup, guidComponentLookup);
                        
                        if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
                    
                        targetSavable.OnLoad(loadDataHandler);
                    }
                }
            }
            
            //iterate over GameObjects with savable component
            foreach (var savable in savableComponents)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                
                foreach (var savableComponent in savable.SavableLookup)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.TargetGuid, savableComponent.guid);

                    if (sceneSaveData.InstanceSaveDataLookup.TryGetValue(componentGuidPath, out var instanceSaveData))
                    {
                        var loadDataHandler = new LoadDataHandler(sceneSaveData, instanceSaveData, 
                            createdObjectsLookup, assetLookup, gameObjectLookup, guidComponentLookup);
                        
                        if (!TypeUtility.TryConvertTo(savableComponent.unityObject, out ISavable targetSavable)) return;
                    
                        targetSavable.OnLoad(loadDataHandler);
                    }
                }
            }
        }
        
        private void DestroyPrefabsOnLoad(SceneSaveData sceneSaveData, List<Savable> savableComponents, List<SavablePrefabElement> savePrefabs)
        {
            var destroyedSavables = savePrefabs.Except(sceneSaveData.SavePrefabs);
            foreach (var prefabsSavable in destroyedSavables)
            {
                foreach (var savable in savableComponents.Where(savable => savable.SceneGuid == prefabsSavable.SceneGuid))
                {
                    Destroy(savable.gameObject);
                    savableComponents.Remove(savable);
                    break;
                }
            }
        }

        
        #endregion
        
        #region Save and Load Helper

        
        private List<SavablePrefabElement> CreateSavablePrefabLookup(List<Savable> savableComponents)
        {
            return (from savable in savableComponents 
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
