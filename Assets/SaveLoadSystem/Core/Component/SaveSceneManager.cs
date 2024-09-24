using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SaveLoadSystem.Core.Attributes;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.Converter;
using SaveLoadSystem.Core.DataTransferObject;
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
            var objectReferenceLookup = BuildObjectToPathReferenceLookup(savableList);
            
            //core saving
            var sceneObjectsLookup = BuildSavableObjectsLookup(savableList, objectReferenceLookup);
            BuildDataBufferContainer(saveDataBuffer, sceneObjectsLookup, objectReferenceLookup);
            return saveDataBufferContainer;
        }

        internal void LoadSnapshot(SceneDataContainer sceneDataContainer)
        {
            var savableList = UnityUtility.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var prefabPoolList = CreatePrefabPoolList(savableList);     //instantiating needed prefabs must happen before performing the core load methods
            InstantiatePrefabsOnLoad(sceneDataContainer, savableList, prefabPoolList);
            
            //core loading
            var referenceBuilder = new DeserializeReferenceBuilder();
            var objectReferenceLookup = BuildPathToObjectReferenceLookup(savableList);
            var createdObjectsLookup = PrepareSaveElementInstances(sceneDataContainer, objectReferenceLookup, referenceBuilder);
            referenceBuilder.InvokeAll(createdObjectsLookup, objectReferenceLookup);
            
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
        
        private SavableObjectsLookup BuildSavableObjectsLookup(List<Savable> savableList, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            var sceneObjectsLookup = new SavableObjectsLookup();
            
            foreach (var savable in savableList)
            {
                var savableGuidPath = new GuidPath(savable.SceneGuid);
                foreach (var componentContainer in savable.SavableList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath.FullPath, componentContainer.guid);
                    ProcessSavableElement(sceneObjectsLookup, componentContainer.unityObject, componentGuidPath, objectReferenceLookup);
                }
            }

            return sceneObjectsLookup;
        }

        public static void ProcessSavableElement(SavableObjectsLookup savableObjectsLookup, object targetObject, 
            GuidPath guidPath, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            //if the fields and properties was found once, it shall not be created again to avoid a stackoverflow by cyclic references
            if (targetObject.IsUnityNull() || savableObjectsLookup.ContainsElement(targetObject)) return;

            var memberList = new Dictionary<string, object>();
            var saveElement = new SavableElement()
            {
                SaveStrategy = SaveStrategy.NotSupported,
                CreatorGuidPath = guidPath,
                Obj = targetObject,
                MemberInfoList = memberList
            };
            
            savableObjectsLookup.AddElement(saveElement);

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
                
                //UnityEngine.Object's can only be saved as a reference, with two exceptions: MonoBehaviours and Scriptable Objects.
                //Both those Objects may have objects, that are marked to be saved. All MonoBehaviours are tracked by the Savable component without nested objects needed.
                //ScriptableObjects must be handled independently, since their reference path is inside the AssetRegistry.
                if (reflectedField is UnityEngine.ScriptableObject)
                {
                    if (objectReferenceLookup.TryGetValue(reflectedField, out guidPath))
                    {
                        ProcessSavableElement(savableObjectsLookup, reflectedField, guidPath, objectReferenceLookup);
                    }
                    else
                    {
                        Debug.LogWarning($"Tried to save a {typeof(ScriptableObject)}, that was not added to the Asset Registry! Please add it!");
                        continue;
                    }
                }
                else if (reflectedField is UnityEngine.Object) continue;
                
                var path = new GuidPath(guidPath.FullPath, fieldInfo.Name);
                ProcessSavableElement(savableObjectsLookup, reflectedField, path, objectReferenceLookup);
            }
            
            foreach (var propertyInfo in propertyInfoList)
            {
                var reflectedProperty = propertyInfo.GetValue(targetObject);
                memberList.Add(propertyInfo.Name, reflectedProperty);

                //UnityEngine.Object's can only be saved as a reference, with two exceptions: MonoBehaviours and Scriptable Objects.
                //Both those Objects may have objects, that are marked to be saved. All MonoBehaviours are tracked by the Savable component without nested objects needed.
                //ScriptableObjects must be handled independently, since their reference path is inside the AssetRegistry.
                if (reflectedProperty is UnityEngine.ScriptableObject)
                {
                    if (objectReferenceLookup.TryGetValue(reflectedProperty, out guidPath))
                    {
                        ProcessSavableElement(savableObjectsLookup, reflectedProperty, guidPath, objectReferenceLookup);
                    }
                    else
                    {
                        Debug.LogWarning($"Tried to save a {typeof(ScriptableObject)}, that was not added to the Asset Registry! Please add it!");
                        continue;
                    }
                }
                if (reflectedProperty is UnityEngine.Object) continue;
                
                var path = new GuidPath(guidPath.FullPath, propertyInfo.Name);
                ProcessSavableElement(savableObjectsLookup, reflectedProperty, path, objectReferenceLookup);
            }
            
            if (targetObject is UnityEngine.Object)
            {
                saveElement.SaveStrategy = SaveStrategy.UnityObject;
            }
            else if (memberList.Count != 0)
            {
                saveElement.SaveStrategy = SaveStrategy.AutomaticSavable;
            }
            else if (targetObject is ISavable)  //TODO: might never be called for scriptable Object
            {
                saveElement.SaveStrategy = SaveStrategy.CustomSavable;
            }
            else if (TypeConverterRegistry.HasConverter(targetObject.GetType()))
            {
                saveElement.SaveStrategy = SaveStrategy.CustomConvertable;
            }
            else
            {
                saveElement.SaveStrategy = SaveStrategy.Serializable;
            }
        }
        
        /// <summary>
        /// When only using Component-Saving and the Type-Converter, this Method will perform saving without reflection,
        /// which heavily improves performance. You will need the exchange the ProcessSavableElement method with this one.
        /// </summary>
        public static void ProcessSavableElementNonReflection(SavableObjectsLookup savableObjectsLookup, object targetObject, 
            GuidPath guidPath, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            //if the fields and properties was found once, it shall not be created again to avoid a stackoverflow by cyclic references
            if (targetObject.IsUnityNull() || savableObjectsLookup.ContainsElement(targetObject)) return;

            var memberList = new Dictionary<string, object>();
            var saveElement = new SavableElement()
            {
                SaveStrategy = SaveStrategy.NotSupported,
                CreatorGuidPath = guidPath,
                Obj = targetObject,
                MemberInfoList = memberList
            };
            
            savableObjectsLookup.AddElement(saveElement);
            
            if (targetObject is UnityEngine.Object)
            {
                saveElement.SaveStrategy = SaveStrategy.UnityObject;
            }
            else if (targetObject is ISavable)
            {
                saveElement.SaveStrategy = SaveStrategy.CustomSavable;
            }
            else if (TypeConverterRegistry.HasConverter(targetObject.GetType()))
            {
                saveElement.SaveStrategy = SaveStrategy.CustomConvertable;
            }
            else
            {
                saveElement.SaveStrategy = SaveStrategy.Serializable;
            }
        }
        

        private void BuildDataBufferContainer(Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer, 
            SavableObjectsLookup savableObjectsLookup, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            for (var index = 0; index < savableObjectsLookup.Count(); index++)
            {
                var saveElement = savableObjectsLookup.GetAt(index);
                var creatorGuidPath = saveElement.CreatorGuidPath;
                var saveObject = saveElement.Obj;
                
                switch (saveElement.SaveStrategy)
                {
                    case SaveStrategy.NotSupported:
                        Debug.LogWarning($"The object of type {saveObject.GetType()} is not supported!");
                        break;
                    
                    case SaveStrategy.UnityObject:
                        var componentDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, saveObject.GetType());
                        
                        HandleSavableMember(saveElement, componentDataBuffer, savableObjectsLookup, objectReferenceLookup);
                        HandleInterfaceOnSave(creatorGuidPath, saveObject, saveDataBuffer, componentDataBuffer, savableObjectsLookup, objectReferenceLookup);
                        
                        saveDataBuffer.Add(creatorGuidPath, componentDataBuffer);
                        break;
                    
                    case SaveStrategy.AutomaticSavable:
                        var savableObjectDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, saveObject.GetType());
                        
                        HandleSavableMember(saveElement, savableObjectDataBuffer, savableObjectsLookup, objectReferenceLookup);
                        HandleInterfaceOnSave(creatorGuidPath, saveObject, saveDataBuffer, savableObjectDataBuffer, savableObjectsLookup, objectReferenceLookup);
                        
                        saveDataBuffer.Add(creatorGuidPath, savableObjectDataBuffer);
                        break;
                    
                    case SaveStrategy.CustomSavable:
                        var savableDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, saveObject.GetType());
                        
                        HandleInterfaceOnSave(creatorGuidPath, saveObject, saveDataBuffer, savableDataBuffer, savableObjectsLookup, objectReferenceLookup);
                        
                        saveDataBuffer.Add(creatorGuidPath, savableDataBuffer);
                        break;
                    
                    case SaveStrategy.CustomConvertable:
                        var convertableDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, saveObject.GetType());
                        
                        var saveDataHandler = new SaveDataHandler(saveDataBuffer, convertableDataBuffer, creatorGuidPath, savableObjectsLookup, objectReferenceLookup);
                        TypeConverterRegistry.GetConverter(saveObject.GetType()).OnSave(saveObject, saveDataHandler);
                        
                        saveDataBuffer.Add(creatorGuidPath, convertableDataBuffer);
                        break;

                    case SaveStrategy.Serializable:
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private void HandleSavableMember(SavableElement savableElement, SaveDataBuffer objectSaveDataBuffer, 
            SavableObjectsLookup savableObjectsLookup, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            if (savableElement.MemberInfoList.Count == 0) return;
            
            foreach (var (memberName, obj) in savableElement.MemberInfoList)
            {
                if (obj.IsUnityNull())
                {
                    objectSaveDataBuffer.SerializableSaveData.Add(memberName, null);
                }
                else
                {
                    if (savableObjectsLookup.TryGetValue(obj, out var foundSaveElement))
                    {
                        if (foundSaveElement.SaveStrategy == SaveStrategy.Serializable)
                        {
                            objectSaveDataBuffer.SerializableSaveData.Add(memberName, JToken.FromObject(obj));
                        }
                        else
                        {
                            objectSaveDataBuffer.GuidPathSaveData.Add(memberName, foundSaveElement.CreatorGuidPath);
                        }
                    }
                    else if (objectReferenceLookup.TryGetValue(obj, out GuidPath path))
                    {
                        objectSaveDataBuffer.GuidPathSaveData.Add(memberName, path);
                    }
                    else
                    {
                        Debug.LogWarning($"Wasn't able to prepare a reference for saving an object at path '{savableElement.CreatorGuidPath}/{memberName}'! Possible Problems: " +
                                         $"1. In order to support references for runtime objects, a savable component is needed including a identifier for the component. " +
                                         $"2. Maybe you forgot to add your object to the Asset Registry! " +
                                         $"3. The object of the savable doesn't match the object stored inside the Asset Registry (Prefabs are stored as Savable and not as GameObject or Transform).");
                    }
                }
            }
        }

        private void HandleInterfaceOnSave(GuidPath creatorGuidPath, object saveObject, Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer, 
            SaveDataBuffer objectSaveDataBuffer, SavableObjectsLookup savableObjectsLookup, Dictionary<object, GuidPath> objectReferenceLookup)
        {
            if (!TypeUtility.TryConvertTo(saveObject, out ISavable objectSavable)) return;
            
            objectSavable.OnSave(new SaveDataHandler(saveDataBuffer, objectSaveDataBuffer, creatorGuidPath, savableObjectsLookup, objectReferenceLookup));
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
        
        private Dictionary<GuidPath, object> PrepareSaveElementInstances(SceneDataContainer sceneDataContainer, 
            Dictionary<string, object> pathToObjectReferenceLookup, DeserializeReferenceBuilder deserializeReferenceBuilder)
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
                        HandleUnityObject(saveDataBuffer, guidPath, pathToObjectReferenceLookup, createdObjectsLookup, deserializeReferenceBuilder);
                        break;
                    
                    case SaveStrategy.AutomaticSavable:
                        var savableObjectInstance = Activator.CreateInstance(type);
                        
                        WriteGuidPathSavableMember(savableObjectInstance, saveDataBuffer.GuidPathSaveData, deserializeReferenceBuilder);
                        WriteSerializableSavableMember(savableObjectInstance, saveDataBuffer.SerializableSaveData);
                        HandleOnLoadInterface(savableObjectInstance, saveDataBuffer, deserializeReferenceBuilder);

                        createdObjectsLookup.Add(guidPath, savableObjectInstance);
                        break;
                    
                    case SaveStrategy.CustomSavable:
                        var savableInterfaceInstance = Activator.CreateInstance(type);
                        
                        HandleOnLoadInterface(savableInterfaceInstance, saveDataBuffer, deserializeReferenceBuilder);

                        createdObjectsLookup.Add(guidPath, savableInterfaceInstance);
                        break;

                    case SaveStrategy.CustomConvertable:
                        if (TypeConverterRegistry.TryGetConverter(type, out IConvertable convertable))
                        {
                            var loadDataHandler = new LoadDataHandler(saveDataBuffer, deserializeReferenceBuilder);
                            var createdObject = convertable.OnLoad(loadDataHandler);
                            createdObjectsLookup.Add(guidPath, createdObject);
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

        private void HandleUnityObject(SaveDataBuffer saveDataBuffer, GuidPath guidPath, Dictionary<string, object> pathToObjectReferenceLookup, 
            Dictionary<GuidPath, object> createdObjectsLookup, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            if (!pathToObjectReferenceLookup.TryGetValue(guidPath.ToString(), out object obj)) return;
            
            WriteGuidPathSavableMember(obj, saveDataBuffer.GuidPathSaveData, deserializeReferenceBuilder);
            WriteSerializableSavableMember(obj, saveDataBuffer.SerializableSaveData);
            HandleOnLoadInterface(obj, saveDataBuffer, deserializeReferenceBuilder);
            
            createdObjectsLookup.Add(guidPath, obj);
        }
        
        private void WriteGuidPathSavableMember(object memberOwner, Dictionary<string, GuidPath> guidPathSaveData, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            if (guidPathSaveData == null) return;
            
            foreach (var (identifier, obj) in guidPathSaveData)
            {
                deserializeReferenceBuilder.EnqueueReferenceBuilding(obj, targetObject => 
                    TypeUtility.TryApplyObjectToMember(memberOwner, identifier, targetObject, true));
            }
        }
        
        private void WriteSerializableSavableMember(object memberOwner, JObject serializableSaveData)
        {
            if (serializableSaveData == null) return;
            
            foreach (var (identifier, obj) in serializableSaveData)
            {
                TypeUtility.TryApplyJsonObjectToMember(memberOwner, identifier, obj);
            }
        }
        
        private void HandleOnLoadInterface(object objectInstance, SaveDataBuffer saveDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            if (!TypeUtility.TryConvertTo(objectInstance, out ISavable objectSavable)) return;
            
            objectSavable.OnLoad(new LoadDataHandler(saveDataBuffer, deserializeReferenceBuilder));
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
