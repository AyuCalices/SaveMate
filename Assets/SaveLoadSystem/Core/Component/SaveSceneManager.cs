using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.EventHandler;
using SaveLoadSystem.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace SaveLoadSystem.Core.Component
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
            var sceneSaveData = new SceneSaveData(instanceSaveDataLookup, CreateSavablePrefabLookup(savableComponents));
            var unityObjectLookup = BuildUnityObjectToPathLookup(savableComponents);
            
            //core saving
            LazySave(instanceSaveDataLookup, savableComponents, unityObjectLookup);
            return sceneSaveData;
        }

        private Dictionary<object, GuidPath> BuildUnityObjectToPathLookup(List<Savable> savableComponents)
        {
            var unityObjectLookup = new Dictionary<object, GuidPath>();
            
            //iterate over all gameobject with the savable component
            foreach (var savable in savableComponents)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                
                foreach (var componentContainer in savable.SavableList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    unityObjectLookup.Add(componentContainer.unityObject, componentGuidPath);
                }
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    unityObjectLookup.Add(componentContainer.unityObject, componentGuidPath);
                }
            }
            
            //iterate over all elements inside the asset registry
            foreach (var componentsContainer in assetRegistry.GetCombinedEnumerable())
            {
                var componentGuidPath = new GuidPath(componentsContainer.guid);
                unityObjectLookup.Add(componentsContainer.unityObject, componentGuidPath);
            }

            return unityObjectLookup;
        }
        
        private void LazySave(Dictionary<GuidPath, InstanceSaveData> instanceSaveDataLookup, 
            List<Savable> savableComponents, Dictionary<object, GuidPath> unityObjectLookup)
        {
            var processedInstancesLookup = new Dictionary<object, GuidPath>();
            
            //iterate over ScriptableObjects
            foreach (var componentContainer in assetRegistry.ScriptableObjectRegistry.Savables)
            {
                var guidPath = new GuidPath(componentContainer.guid);
                var instanceSaveData = new InstanceSaveData();
                
                instanceSaveDataLookup.Add(guidPath, instanceSaveData);
                
                if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
            
                targetSavable.OnSave(new SaveDataHandler(guidPath, instanceSaveData, instanceSaveDataLookup, processedInstancesLookup, unityObjectLookup));
            }
            
            //iterate over GameObjects with savable component
            foreach (var savable in savableComponents)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                foreach (var componentContainer in savable.SavableList)
                {
                    var guidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    var instanceSaveData = new InstanceSaveData();
                
                    instanceSaveDataLookup.Add(guidPath, instanceSaveData);
                    
                    if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
            
                    targetSavable.OnSave(new SaveDataHandler(guidPath, instanceSaveData, instanceSaveDataLookup, processedInstancesLookup, unityObjectLookup));
                }
            }
        }
        
        
        #endregion

        #region LoadMethods
        
        
        internal void LoadSnapshot(SceneSaveData sceneSaveData)
        {
            var savableComponents = UnityUtility.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var savablePrefabList = CreateSavablePrefabLookup(savableComponents);     //instantiating needed prefabs must happen before performing the core load methods
            InstantiatePrefabsOnLoad(sceneSaveData, savableComponents, savablePrefabList);
            
            //core loading
            var unityObjectLookup = BuildPathToUnityObjectLookup(savableComponents);

            LazyLoad(sceneSaveData, savableComponents, unityObjectLookup, new Dictionary<GuidPath, object>());
            
            //destroy prefabs, that are not present in the save file
            DestroyPrefabsOnLoad(sceneSaveData, savableComponents, savablePrefabList);
        }

        private void InstantiatePrefabsOnLoad(SceneSaveData sceneSaveData, List<Savable> savableComponents, List<(string, string)> savablePrefabList)
        {
            var instantiatedSavables = sceneSaveData.SavablePrefabList.Except(savablePrefabList);
            foreach (var (prefab, sceneGuid) in instantiatedSavables)
            {
                if (assetRegistry.PrefabRegistry.TryGetPrefab(prefab, out Savable savable))
                {
                    var instantiatedSavable = Instantiate(savable);
                    instantiatedSavable.SetSceneGuidGroup(sceneGuid);
                    savableComponents.Add(instantiatedSavable);
                }
            }
        }
        
        private Dictionary<string, object> BuildPathToUnityObjectLookup(List<Savable> savableComponents)
        {
            var createdInstancesLookup = new Dictionary<string, object>();
            
            foreach (var savable in savableComponents)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                
                foreach (var componentContainer in savable.SavableList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    createdInstancesLookup.Add(componentGuidPath.ToString(), componentContainer.unityObject);
                }
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    createdInstancesLookup.Add(componentGuidPath.ToString(), componentContainer.unityObject);
                }
            }
            
            foreach (var componentsContainer in assetRegistry.GetCombinedEnumerable())
            {
                createdInstancesLookup.Add(componentsContainer.guid, componentsContainer.unityObject);
            }

            return createdInstancesLookup;
        }
        
        private void LazyLoad(SceneSaveData sceneSaveData, List<Savable> savableComponents,  
            Dictionary<string, object> unityObjectLookup, Dictionary<GuidPath, object> createdObjectsLookup)
        {
            //iterate over ScriptableObjects
            foreach (var componentContainer in assetRegistry.ScriptableObjectRegistry.Savables)
            {
                var guidPath = new GuidPath(componentContainer.guid);
                
                if (sceneSaveData.InstanceSaveDataLookup.TryGetValue(guidPath, out var instanceSaveData))
                {
                    var loadDataHandler = new LoadDataHandler(sceneSaveData, instanceSaveData, unityObjectLookup, createdObjectsLookup);
                        
                    if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
                    
                    targetSavable.OnLoad(loadDataHandler);
                }
            }
            
            //iterate over GameObjects with savable component
            foreach (var savable in savableComponents)
            {
                var sceneGuidPath = new GuidPath(savable.SceneGuid);
                foreach (var componentContainer in savable.SavableList)
                {
                    var componentGuidPath = new GuidPath(sceneGuidPath.FullPath, componentContainer.guid);

                    if (sceneSaveData.InstanceSaveDataLookup.TryGetValue(componentGuidPath, out var instanceSaveData))
                    {
                        var loadDataHandler = new LoadDataHandler(sceneSaveData, instanceSaveData, unityObjectLookup, createdObjectsLookup);
                        
                        if (!TypeUtility.TryConvertTo(componentContainer.unityObject, out ISavable targetSavable)) return;
                    
                        targetSavable.OnLoad(loadDataHandler);
                    }
                }
            }
        }
        
        private void DestroyPrefabsOnLoad(SceneSaveData sceneSaveData, List<Savable> savableComponents, List<(string, string)> savablePrefabList)
        {
            var destroyedSavables = savablePrefabList.Except(sceneSaveData.SavablePrefabList);
            foreach (var (_, sceneGuid) in destroyedSavables)
            {
                foreach (var savable in savableComponents.Where(savable => savable.SceneGuid == sceneGuid))
                {
                    Destroy(savable.gameObject);
                    savableComponents.Remove(savable);
                    break;
                }
            }
        }

        
        #endregion
        
        #region Save and Load Helper

        
        private List<(string, string)> CreateSavablePrefabLookup(List<Savable> savableComponents)
        {
            return (from savable in savableComponents 
                where !savable.DynamicPrefabSpawningDisabled && assetRegistry.PrefabRegistry.ContainsPrefabGuid(savable.PrefabGuid) 
                select (savable.PrefabGuid, savable.SceneGuid)).ToList();
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
