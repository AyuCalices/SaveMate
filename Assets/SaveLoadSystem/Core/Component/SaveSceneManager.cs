using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaveLoadSystem.Core.Attributes;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.Converter;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Utility;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace SaveLoadSystem.Core.Component
{
    public class SaveSceneManager : MonoBehaviour
    {
        [SerializeField] private SaveLoadManager saveLoadManager;
        [SerializeField] private PrefabRegistry prefabRegistry;
        
        [Header("Current Scene Events")]
        [SerializeField] private bool loadSceneOnAwake;
        [SerializeField] private SaveSceneManagerDestroyType saveSceneOnDestroy;
        [SerializeField] private SceneManagerEvents sceneManagerEvents;

        private static bool _isQuitting;

        #region Unity Lifecycle

        private void Awake()
        {
            saveLoadManager.RegisterSaveSceneManager(this);

            if (loadSceneOnAwake)
            {
                LoadScene();
            }
        }
        
        private void OnEnable()
        {
            saveLoadManager.RegisterSaveSceneManager(this);
        }
        
        private void OnDisable()
        {
            saveLoadManager.UnregisterSaveSceneManager(this);
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
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
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
            saveLoadManager.SaveFocus.WipeSceneData(gameObject.scene);
        }
        
        [ContextMenu("Delete Scene Data")]
        public void DeleteSceneData()
        {
            saveLoadManager.SaveFocus.DeleteSceneDataFromDisk(gameObject.scene);
        }

        [ContextMenu("Reload Then Load Scene")]
        public void ReloadThenLoadScene()
        {
            saveLoadManager.SaveFocus.ReloadThenLoadScenes(gameObject.scene);
        }

        #endregion

        #region Snapshot
        
        internal SceneDataContainer CreateSnapshot()
        {
            //prepare data
            Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer = new();
            var savableList = UnityUtility.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var saveDataBufferContainer = new SceneDataContainer(saveDataBuffer, CreatePrefabPoolList(savableList));
            var objectReferenceLookup = BuildObjectReferenceLookup(savableList);
            
            //core saving
            var saveElementLookup = BuildSavableElementLookup(savableList);
            BuildDataBufferContainer(saveDataBuffer, saveElementLookup, objectReferenceLookup);
            return saveDataBufferContainer;
        }

        internal void LoadSnapshot(SceneDataContainer sceneDataContainer)
        {
            var savableList = UnityUtility.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var prefabPoolList = CreatePrefabPoolList(savableList);     //instantiating needed prefabs must happen before performing the core load methods
            InstantiatePrefabsOnLoad(sceneDataContainer, savableList, prefabPoolList);
            
            //core loading
            var referenceBuilder = new DeserializeReferenceBuilder();
            var createdObjectsLookup = PrepareSaveElementInstances(sceneDataContainer, savableList, referenceBuilder);
            var guidPathReferenceLookup = BuildGuidPathReferenceLookup(savableList);
            referenceBuilder.InvokeAll(createdObjectsLookup, guidPathReferenceLookup);
            
            //destroy prefabs, that are not present in the save file
            DestroyPrefabsOnLoad(sceneDataContainer, savableList, prefabPoolList);
        }

        private List<(string, string)> CreatePrefabPoolList(List<Savable> savableList)
        {
            return (from savable in savableList 
                where !savable.CustomSpawning && prefabRegistry.ContainsGuid(savable.PrefabGuid) 
                select (savable.PrefabGuid, savable.SceneGuid)).ToList();
        }

        #endregion

        #region Save Methods

        private Dictionary<object, GuidPath> BuildObjectReferenceLookup(List<Savable> savableList)
        {
            var objectReferenceLookup = new Dictionary<object, GuidPath>();
            
            foreach (var savable in savableList)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    objectReferenceLookup.Add(componentContainer.unityObject, componentGuidPath);
                }
            }

            return objectReferenceLookup;
        }
        
        private SavableElementLookup BuildSavableElementLookup(List<Savable> savableList)
        {
            var saveElementLookup = new SavableElementLookup();
            foreach (var savable in savableList)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                foreach (var componentContainer in savable.SavableList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    ProcessSavableElement(saveElementLookup, componentContainer.unityObject, componentGuidPath, saveElementLookup.Count());
                }
            }

            return saveElementLookup;
        }

        public static void ProcessSavableElement(SavableElementLookup savableElementLookup, object targetObject, GuidPath guidPath, int insertIndex)
        {
            //if the fields and properties was found once, it shall not be created again to avoid a stackoverflow by cyclic references
            if (targetObject == null || savableElementLookup.ContainsElement(targetObject)) return;

            var memberList = new Dictionary<string, object>();
            var saveElement = new SavableElement()
            {
                SaveStrategy = SaveStrategy.NotSupported,
                CreatorGuidPath = guidPath,
                Obj = targetObject,
                MemberInfoList = memberList
            };
            
            savableElementLookup.InsertElement(insertIndex, saveElement);
            insertIndex++;

            //initialize fields and properties
            IEnumerable<FieldInfo> fieldInfoList;
            IEnumerable<PropertyInfo> propertyInfoList;
            if (TypeUtility.TryGetAttribute(targetObject.GetType(), out SavableObjectAttribute savableObjectAttribute))
            {
                fieldInfoList = TypeUtility.GetFieldInfos(targetObject.GetType(), savableObjectAttribute.DeclaredOnly);
                propertyInfoList = TypeUtility.GetPropertyInfos(targetObject.GetType(), savableObjectAttribute.DeclaredOnly);
            }
            else
            {
                fieldInfoList = TypeUtility.GetFieldInfosWithAttribute<SavableAttribute>(targetObject.GetType());
                propertyInfoList = TypeUtility.GetPropertyInfosWithAttribute<SavableAttribute>(targetObject.GetType());
            }

            //recursion with field and property members
            foreach (var fieldInfo in fieldInfoList)
            {
                var reflectedField = fieldInfo.GetValue(targetObject);
                memberList.Add(fieldInfo.Name, reflectedField);
                
                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedField is UnityEngine.Object) continue;
                
                var path = new GuidPath(guidPath.FullPath, fieldInfo.Name);
                ProcessSavableElement(savableElementLookup, reflectedField, path, insertIndex);
            }
            
            foreach (var propertyInfo in propertyInfoList)
            {
                var reflectedProperty = propertyInfo.GetValue(targetObject);
                memberList.Add(propertyInfo.Name, reflectedProperty);

                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedProperty is UnityEngine.Object) continue;
                
                var path = new GuidPath(guidPath.FullPath, propertyInfo.Name);
                ProcessSavableElement(savableElementLookup, reflectedProperty, path, insertIndex);
            }
            
            if (targetObject is UnityEngine.Object)
            {
                saveElement.SaveStrategy = SaveStrategy.UnityObject;
            }
            else if (memberList.Count != 0)
            {
                saveElement.SaveStrategy = SaveStrategy.AutomaticSavable;
            }
            else if (targetObject is ISavable)
            {
                saveElement.SaveStrategy = SaveStrategy.CustomSavable;
            }
            else if (ConverterRegistry.HasConverter(targetObject.GetType()))
            {
                saveElement.SaveStrategy = SaveStrategy.CustomConvertable;
            }
            else
            {
                saveElement.SaveStrategy = SaveStrategy.Serializable;
            }
        }

        private void BuildDataBufferContainer(Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer, SavableElementLookup savableElementLookup, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            for (var index = 0; index < savableElementLookup.Count(); index++)
            {
                var saveElement = savableElementLookup.GetAt(index);
                var creatorGuidPath = saveElement.CreatorGuidPath;
                var saveObject = saveElement.Obj;
                
                switch (saveElement.SaveStrategy)
                {
                    case SaveStrategy.NotSupported:
                        Debug.LogWarning($"The object of type {saveObject.GetType()} is not supported!");
                        break;
                    
                    case SaveStrategy.UnityObject:
                        var componentDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        HandleSavableMember(saveElement, componentDataBuffer, savableElementLookup, objectReferenceLookup);
                        HandleInterfaceOnSave(saveObject, saveDataBuffer, componentDataBuffer, savableElementLookup, objectReferenceLookup, index);
                        
                        saveDataBuffer.Add(creatorGuidPath, componentDataBuffer);
                        break;
                    
                    case SaveStrategy.AutomaticSavable:
                        var savableObjectDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        HandleSavableMember(saveElement, savableObjectDataBuffer, savableElementLookup, objectReferenceLookup);
                        HandleInterfaceOnSave(saveObject, saveDataBuffer, savableObjectDataBuffer, savableElementLookup, objectReferenceLookup, index);
                        
                        saveDataBuffer.Add(creatorGuidPath, savableObjectDataBuffer);
                        break;
                    
                    case SaveStrategy.CustomSavable:
                        var savableDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        HandleInterfaceOnSave(saveObject, saveDataBuffer, savableDataBuffer, savableElementLookup, objectReferenceLookup, index);
                        
                        saveDataBuffer.Add(creatorGuidPath, savableDataBuffer);
                        break;
                    
                    case SaveStrategy.CustomConvertable:
                        var convertableDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        var saveDataHandler = new SaveDataHandler(convertableDataBuffer, saveDataBuffer, savableElementLookup, objectReferenceLookup, index);
                        ConverterRegistry.GetConverter(saveObject.GetType()).OnSave(saveObject, saveDataHandler);
                        
                        saveDataBuffer.Add(creatorGuidPath, convertableDataBuffer);
                        break;

                    case SaveStrategy.Serializable:
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private void HandleSavableMember(SavableElement savableElement, SaveDataBuffer objectSaveDataBuffer, SavableElementLookup savableElementLookup, Dictionary<object, GuidPath> referenceLookup)
        {
            if (savableElement.MemberInfoList.Count == 0) return;
            
            foreach (var (objectName, obj) in savableElement.MemberInfoList)
            {
                if (obj == null)
                {
                    objectSaveDataBuffer.SerializableSaveData.Add(objectName, null);
                }
                else
                {
                    if (savableElementLookup.TryGetValue(obj, out var foundSaveElement))
                    {
                        if (foundSaveElement.SaveStrategy == SaveStrategy.Serializable)
                        {
                            objectSaveDataBuffer.SerializableSaveData.Add(objectName, JToken.FromObject(obj));
                        }
                        else
                        {
                            objectSaveDataBuffer.GuidPathSaveData.Add(objectName, foundSaveElement.CreatorGuidPath);
                        }
                    }
                    else if (referenceLookup.TryGetValue(obj, out GuidPath path))
                    {
                        objectSaveDataBuffer.GuidPathSaveData.Add(objectName, path);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"You need to add a savable component to the origin GameObject of the '{obj.GetType()}' component. Then you need to apply an " +
                            $"ID by adding it into the ReferenceList. This will enable support for component referencing!");
                    }
                }
            }
        }

        private void HandleInterfaceOnSave(object saveObject, Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer, SaveDataBuffer objectSaveDataBuffer, SavableElementLookup savableElementLookup, Dictionary<object, GuidPath> objectReferenceLookup, int index)
        {
            if (!TypeUtility.TryConvertTo(saveObject, out ISavable objectSavable)) return;
            
            objectSavable.OnSave(new SaveDataHandler(objectSaveDataBuffer, saveDataBuffer, savableElementLookup, objectReferenceLookup, index));
        }

        #endregion

        #region LoadMethods

        private void InstantiatePrefabsOnLoad(SceneDataContainer sceneDataContainer, List<Savable> savableList, List<(string, string)> currentPrefabList)
        {
            var instantiatedSavables = sceneDataContainer.PrefabList.Except(currentPrefabList);
            foreach (var (prefab, sceneGuid) in instantiatedSavables)
            {
                if (prefabRegistry.TryGetSavable(prefab, out Savable savable))
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
        
        private Dictionary<string, object> BuildGuidPathReferenceLookup(List<Savable> savableList)
        {
            var saveElementLookup = new Dictionary<string, object>();
            
            foreach (var savable in savableList)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    saveElementLookup.Add(componentGuidPath.ToString(), componentContainer.unityObject);
                }
            }

            return saveElementLookup;
        }
        
        private Dictionary<GuidPath, object> PrepareSaveElementInstances(SceneDataContainer sceneDataContainer, List<Savable> savableList, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var createdObjectsLookup = new Dictionary<GuidPath, object>();
            
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
                        HandleUnityObject(savableList, saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
                        break;
                    
                    case SaveStrategy.AutomaticSavable:
                        var savableObjectInstance = Activator.CreateInstance(type);
                        
                        WriteGuidPathSavableMember(savableObjectInstance, saveDataBuffer.GuidPathSaveData, deserializeReferenceBuilder);
                        WriteSerializableSavableMember(savableObjectInstance, saveDataBuffer.SerializableSaveData);
                        HandleOnLoadInterface(savableObjectInstance, saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);

                        createdObjectsLookup.Add(guidPath, savableObjectInstance);
                        break;
                    
                    case SaveStrategy.CustomSavable:
                        var savableInterfaceInstance = Activator.CreateInstance(type);
                        
                        HandleOnLoadInterface(savableInterfaceInstance, saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);

                        createdObjectsLookup.Add(guidPath, savableInterfaceInstance);
                        break;

                    case SaveStrategy.CustomConvertable:
                        if (ConverterRegistry.TryGetConverter(type, out IConvertable convertable))
                        {
                            var loadDataHandler = new LoadDataHandler(saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
                            convertable.OnLoad(loadDataHandler);
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

            return createdObjectsLookup;
        }

        private void HandleUnityObject(List<Savable> savableList, SaveDataBuffer saveDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder, 
            Dictionary<GuidPath, object> createdObjectsLookup, GuidPath guidPath)
        {
            var stack = guidPath.ToStack();
            var searchedSceneGuid = stack.Pop();
            foreach (var savable in savableList)
            {
                if (savable.SceneGuid != searchedSceneGuid) continue;
                        
                var searchedComponentGuid = stack.Pop();
                        
                var combinedList = savable.SavableList.Concat(savable.ReferenceList);
                foreach (var componentContainer in combinedList)
                {
                    if (componentContainer.guid != searchedComponentGuid) continue;
                                
                    WriteGuidPathSavableMember(componentContainer.unityObject, saveDataBuffer.GuidPathSaveData, deserializeReferenceBuilder);
                    WriteSerializableSavableMember(componentContainer.unityObject, saveDataBuffer.SerializableSaveData);
                    HandleOnLoadInterface(componentContainer.unityObject, saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
                                
                    createdObjectsLookup.Add(guidPath, componentContainer.unityObject);
                    return;
                }
            }
        }
        
        private void WriteGuidPathSavableMember(object memberOwner, Dictionary<string, GuidPath> guidPathSavable, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            if (guidPathSavable == null) return;
            
            foreach (var (identifier, obj) in guidPathSavable)
            {
                deserializeReferenceBuilder.EnqueueReferenceBuilding(obj, targetObject => 
                    TypeUtility.TryApplyObjectToMember(memberOwner, identifier, targetObject, true));
            }
        }
        
        private void WriteSerializableSavableMember(object memberOwner, JObject serializableSavables)
        {
            if (serializableSavables == null) return;
            
            foreach (var (identifier, obj) in serializableSavables)
            {
                TypeUtility.TryApplyJsonObjectToMember(memberOwner, identifier, obj);
            }
        }
        
        private void HandleOnLoadInterface(object loadObject, SaveDataBuffer saveDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder, 
            Dictionary<GuidPath, object> createdObjectsLookup, GuidPath guidPath)
        {
            if (!TypeUtility.TryConvertTo(loadObject, out ISavable objectSavable)) return;
            
            objectSavable.OnLoad(new LoadDataHandler(saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath));
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion
    }
}
