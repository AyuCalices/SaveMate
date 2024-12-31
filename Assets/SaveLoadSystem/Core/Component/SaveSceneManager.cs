using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.Converter;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.EventHandler;
using SaveLoadSystem.Utility;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

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

        #region Snapshot
        
        internal SceneDataContainer CreateSnapshot()
        {
            //prepare data
            Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer = new();
            var savableList = UnityUtility.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var saveDataBufferContainer = new SceneDataContainer(saveDataBuffer, CreatePrefabPoolList(savableList));
            var objectReferenceLookup = BuildObjectToPathReferenceLookup(savableList);
            
            //core saving
            BuildSavableObjectsLookup(savableList, saveDataBuffer, objectReferenceLookup);
            return saveDataBufferContainer;
        }

        internal void LoadSnapshot(SceneDataContainer sceneDataContainer)
        {
            var savableList = UnityUtility.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var prefabPoolList = CreatePrefabPoolList(savableList);     //instantiating needed prefabs must happen before performing the core load methods
            InstantiatePrefabsOnLoad(sceneDataContainer, savableList, prefabPoolList);
            
            //core loading
            var pathToObjectReferenceLookup = BuildPathToObjectReferenceLookup(savableList);
            PrepareSaveElementInstances(sceneDataContainer, pathToObjectReferenceLookup, out var createdObjectsLookup, out var instancesList);
            HandleInstancesOnLoad(instancesList, pathToObjectReferenceLookup, createdObjectsLookup);
            
            //destroy prefabs, that are not present in the save file
            DestroyPrefabsOnLoad(sceneDataContainer, savableList, prefabPoolList);
        }

        private List<(string, string)> CreatePrefabPoolList(List<Savable> savableList)
        {
            return (from savable in savableList 
                where !savable.DynamicPrefabSpawningDisabled && assetRegistry.PrefabLookup.ContainsPrefabGuid(savable.PrefabGuid) 
                select (savable.PrefabGuid, savable.SceneGuid)).ToList();
        }

        #endregion

        #region Save Methods

        private Dictionary<object, GuidPath> BuildObjectToPathReferenceLookup(List<Savable> savableList)
        {
            var objectReferenceLookup = new Dictionary<object, GuidPath>();
            
            foreach (var savable in savableList)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                
                foreach (var componentContainer in savable.SavableList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    objectReferenceLookup.Add(componentContainer.unityObject, componentGuidPath);
                }
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    objectReferenceLookup.Add(componentContainer.unityObject, componentGuidPath);
                }
            }
            
            foreach (var componentsContainer in assetRegistry.GetCombinedEnumerable())
            {
                var componentGuidPath = new GuidPath(componentsContainer.guid);
                objectReferenceLookup.Add(componentsContainer.unityObject, componentGuidPath);
            }

            return objectReferenceLookup;
        }
        
        private void BuildSavableObjectsLookup(List<Savable> savableList, 
            Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            var processedSavablesLookup = new Dictionary<object, GuidPath>();
            
            foreach (var savable in savableList)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                foreach (var componentContainer in savable.SavableList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    //TODO: at this point it is always unityObject - maybe simplify
                    ProcessAsSaveReferencable(processedSavablesLookup, componentContainer.unityObject, componentGuidPath, saveDataBuffer, objectReferenceLookup);
                }
            }
        }
        
        /// <summary>
        /// When only using Component-Saving and the Type-Converter, this Method will perform saving without reflection,
        /// which heavily improves performance. You will need the exchange the ProcessSavableElement method with this one.
        /// </summary>
        public static void ProcessAsSaveReferencable(Dictionary<object, GuidPath> processedSavablesLookup, object targetObject, 
            GuidPath guidPath, Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer, 
            Dictionary<object, GuidPath> objectReferenceLookup)  //TODO: objectReferenceLookup might be needed for scriptable object
        {
            //if the fields and properties was found once, it shall not be created again to avoid a stackoverflow by cyclic references
            if (targetObject.IsUnityNull() || !processedSavablesLookup.TryAdd(targetObject, guidPath)) return;

            if (targetObject is UnityEngine.Object)
            {
                var componentDataBuffer = new SaveDataBuffer(SaveStrategy.UnityObject, targetObject.GetType());
                
                saveDataBuffer.Add(guidPath, componentDataBuffer);
                
                HandleInterfaceOnSave(guidPath, targetObject, saveDataBuffer, componentDataBuffer, processedSavablesLookup, objectReferenceLookup);
            }
            else if (targetObject is ISavable)
            {
                var savableDataBuffer = new SaveDataBuffer(SaveStrategy.CustomSavable, targetObject.GetType());
                
                saveDataBuffer.Add(guidPath, savableDataBuffer);
                        
                HandleInterfaceOnSave(guidPath, targetObject, saveDataBuffer, savableDataBuffer, processedSavablesLookup, objectReferenceLookup);
            }
            else if (TypeConverterRegistry.HasConverter(targetObject.GetType()))
            {
                var convertableDataBuffer = new SaveDataBuffer(SaveStrategy.CustomConvertable, targetObject.GetType());
                
                saveDataBuffer.Add(guidPath, convertableDataBuffer);
                        
                var saveDataHandler = new SaveDataHandler(saveDataBuffer, convertableDataBuffer, guidPath, processedSavablesLookup, objectReferenceLookup);
                TypeConverterRegistry.GetConverter(targetObject.GetType()).OnSave(targetObject, saveDataHandler);
            }
            else
            {
                var componentDataBuffer = new SaveDataBuffer(SaveStrategy.Serializable, targetObject.GetType());
                
                saveDataBuffer.Add(guidPath, componentDataBuffer);
                
                componentDataBuffer.CustomSerializableSaveData.Add("SerializeRef", JToken.FromObject(targetObject));
            }
        }

        private static void HandleInterfaceOnSave(GuidPath creatorGuidPath, object saveObject, Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer, 
            SaveDataBuffer objectSaveDataBuffer, Dictionary<object, GuidPath> processedSavablesLookup, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            if (!TypeUtility.TryConvertTo(saveObject, out ISavable objectSavable)) return;
            
            objectSavable.OnSave(new SaveDataHandler(saveDataBuffer, objectSaveDataBuffer, creatorGuidPath, processedSavablesLookup, objectReferenceLookup));
        }

        #endregion

        #region LoadMethods

        private void InstantiatePrefabsOnLoad(SceneDataContainer sceneDataContainer, List<Savable> savableList, List<(string, string)> currentPrefabList)
        {
            var instantiatedSavables = sceneDataContainer.PrefabList.Except(currentPrefabList);
            foreach (var (prefab, sceneGuid) in instantiatedSavables)
            {
                if (assetRegistry.PrefabLookup.TryGetPrefab(prefab, out Savable savable))
                {
                    var instantiatedSavable = Instantiate(savable);
                    instantiatedSavable.SetSceneGuidGroup(sceneGuid);
                    savableList.Add(instantiatedSavable);
                }
            }
        }
        
        private void DestroyPrefabsOnLoad(SceneDataContainer sceneDataContainer, List<Savable> savableList, List<(string, string)> currentPrefabList)
        {
            var destroyedSavables = currentPrefabList.Except(sceneDataContainer.PrefabList);
            foreach (var (_, sceneGuid) in destroyedSavables)
            {
                foreach (var savable in savableList.Where(savable => savable.SceneGuid == sceneGuid))
                {
                    Destroy(savable.gameObject);
                    savableList.Remove(savable);
                    break;
                }
            }
        }
        
        private Dictionary<string, object> BuildPathToObjectReferenceLookup(List<Savable> savableList)
        {
            var objectReferenceLookup = new Dictionary<string, object>();
            
            foreach (var savable in savableList)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                
                foreach (var componentContainer in savable.SavableList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    objectReferenceLookup.Add(componentGuidPath.ToString(), componentContainer.unityObject);
                }
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    objectReferenceLookup.Add(componentGuidPath.ToString(), componentContainer.unityObject);
                }
            }
            
            foreach (var componentsContainer in assetRegistry.GetCombinedEnumerable())
            {
                objectReferenceLookup.Add(componentsContainer.guid, componentsContainer.unityObject);
            }

            return objectReferenceLookup;
        }
        
        private void PrepareSaveElementInstances(SceneDataContainer sceneDataContainer, Dictionary<string, object> pathToObjectReferenceLookup,
            out Dictionary<GuidPath, object> createdObjectsLookup, out List<(SaveDataBuffer, object)>  instancesList)
        {
            createdObjectsLookup = new Dictionary<GuidPath, object>();
            instancesList = new List<(SaveDataBuffer, object)>();
            
            foreach (var (guidPath, saveDataBuffer) in sceneDataContainer.SaveObjectLookup)
            {
                var type = Type.GetType(saveDataBuffer.SavableType);
                if (type == null)
                {
                    Debug.LogWarning("Couldn't convert the contained type!");
                    continue;
                }
                
                switch (saveDataBuffer.SaveStrategy)
                {
                    case SaveStrategy.NotSupported:
                        Debug.LogWarning($"The object of type {saveDataBuffer.SavableType} is not supported!");
                        break;
                    
                    case SaveStrategy.UnityObject:
                        if (!pathToObjectReferenceLookup.TryGetValue(guidPath.ToString(), out var unityObj)) continue;
                        
                        createdObjectsLookup.Add(guidPath, unityObj);
                        instancesList.Add((saveDataBuffer, unityObj));
                        break;
                    
                    case SaveStrategy.CustomSavable:
                        var customSavableObj = Activator.CreateInstance(type);

                        createdObjectsLookup.Add(guidPath, customSavableObj);
                        instancesList.Add((saveDataBuffer, customSavableObj));
                        break;

                    case SaveStrategy.CustomConvertable:

                        if (TypeConverterRegistry.TryGetConverter(type, out IConvertable convertable))
                        {
                            var customConvertableObj = convertable.OnBeginLoad(new SimpleLoadDataHandler(saveDataBuffer));
                            
                            createdObjectsLookup.Add(guidPath, customConvertableObj);
                            instancesList.Add((saveDataBuffer, customConvertableObj));
                        }
                        break;
                    
                    case SaveStrategy.Serializable:
                        var serializableInstance = saveDataBuffer.CustomSerializableSaveData["Serializable"]?.ToObject(type);
                        createdObjectsLookup.Add(guidPath, serializableInstance);
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private void HandleInstancesOnLoad(List<(SaveDataBuffer, object)> instancesList, 
            Dictionary<string, object> pathToObjectReferenceLookup, Dictionary<GuidPath, object> createdObjectsLookup)
        {
            foreach (var (saveDataBuffer, instance) in instancesList)
            {
                var loadDataHandler = new LoadDataHandler(saveDataBuffer, pathToObjectReferenceLookup, createdObjectsLookup);
                if (saveDataBuffer.SaveStrategy is SaveStrategy.CustomConvertable)
                {
                    if (TypeConverterRegistry.TryGetConverter(instance.GetType(), out IConvertable convertable))
                    {
                        convertable.OnLoad(instance, loadDataHandler);
                    }
                }
                else
                {
                    if (!TypeUtility.TryConvertTo(instance, out ISavable objectSavable)) return;
                    
                    objectSavable.OnLoad(loadDataHandler);
                }
            }
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
