using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaveLoadSystem.Core.Attributes;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.Converter;
using SaveLoadSystem.Core.Serializable;
using SaveLoadSystem.Utility;
using UnityEngine;

namespace SaveLoadSystem.Core.Component
{
    public class SaveSceneManager : MonoBehaviour
    {
        [SerializeField] private PrefabRegistry prefabRegistry;
        
        [ContextMenu("Save Scene Data")]
        public void SaveSceneData()
        {
            //prepare data
            Dictionary<GuidPath, SaveDataBuffer> saveDataBuffer = new();
            var savableList = UnityObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var saveDataBufferContainer = new SaveDataBufferContainer(saveDataBuffer, CreatePrefabPoolList(savableList));
            var objectReferenceLookup = BuildObjectReferenceLookup(savableList);
            
            //core saving
            var saveElementLookup = BuildSavableElementLookup(savableList);
            BuildDataBufferContainer(saveDataBuffer, saveElementLookup, objectReferenceLookup);
            SaveLoadManager.Save(saveDataBufferContainer);
        }

        [ContextMenu("Load Scene Data")]
        public void LoadSceneData()
        {
            //prepare data
            var dataBufferContainer = SaveLoadManager.Load<SaveDataBufferContainer>();
            var savableList = UnityObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var prefabPoolList = CreatePrefabPoolList(savableList);     //instantiating needed prefabs must happen before performing the core load methods
            InstantiatePrefabsOnLoad(dataBufferContainer, savableList, prefabPoolList);
            
            //core loading
            var referenceBuilder = new DeserializeReferenceBuilder();
            var createdObjectsLookup = PrepareSaveElementInstances(dataBufferContainer, savableList, referenceBuilder);
            var guidPathReferenceLookup = BuildGuidPathReferenceLookup(savableList);
            referenceBuilder.InvokeAll(createdObjectsLookup, guidPathReferenceLookup);
            
            //destroy prefabs, that are not present in the save file
            DestroyPrefabsOnLoad(dataBufferContainer, savableList, prefabPoolList);
        }

        #region Universal

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
                var savableGuidPath = new GuidPath(null, savable.SceneGuid);
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath, componentContainer.guid);
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
                var savableGuidPath = new GuidPath(null, savable.SceneGuid);
                foreach (var componentContainer in savable.SavableList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath, componentContainer.guid);
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
            if (ReflectionUtility.ClassHasAttribute<SavableSchemaAttribute>(targetObject.GetType()))
            {
                fieldInfoList = ReflectionUtility.GetFieldInfos(targetObject.GetType());
                propertyInfoList = ReflectionUtility.GetPropertyInfos(targetObject.GetType());
            }
            else
            {
                fieldInfoList = ReflectionUtility.GetFieldInfosWithAttribute<SavableAttribute>(targetObject.GetType());
                propertyInfoList = ReflectionUtility.GetPropertyInfosWithAttribute<SavableAttribute>(targetObject.GetType());
            }

            //recursion with field and property members
            foreach (var fieldInfo in fieldInfoList)
            {
                var reflectedField = fieldInfo.GetValue(targetObject);
                memberList.Add(fieldInfo.Name, reflectedField);
                
                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedField is UnityEngine.Object) continue;
                
                var path = new GuidPath(guidPath, fieldInfo.Name);
                ProcessSavableElement(savableElementLookup, reflectedField, path, insertIndex);
            }
            
            foreach (var propertyInfo in propertyInfoList)
            {
                var reflectedProperty = propertyInfo.GetValue(targetObject);
                memberList.Add(propertyInfo.Name, reflectedProperty);

                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedProperty is UnityEngine.Object) continue;
                
                var path = new GuidPath(guidPath, propertyInfo.Name);
                ProcessSavableElement(savableElementLookup, reflectedProperty, path, insertIndex);
            }
            
            //define save strategy TODO: to strategy pattern
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
                        HandleInterfaceOnSave(saveObject, componentDataBuffer, savableElementLookup, objectReferenceLookup, index);
                        
                        saveDataBuffer.Add(creatorGuidPath, componentDataBuffer);
                        break;
                    
                    case SaveStrategy.AutomaticSavable:
                        var savableObjectDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        HandleSavableMember(saveElement, savableObjectDataBuffer, savableElementLookup, objectReferenceLookup);
                        HandleInterfaceOnSave(saveObject, savableObjectDataBuffer, savableElementLookup, objectReferenceLookup, index);
                        
                        saveDataBuffer.Add(creatorGuidPath, savableObjectDataBuffer);
                        break;
                    
                    case SaveStrategy.CustomSavable:
                        var savableDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        HandleInterfaceOnSave(saveObject, savableDataBuffer, savableElementLookup, objectReferenceLookup, index);
                        
                        saveDataBuffer.Add(creatorGuidPath, savableDataBuffer);
                        break;
                    
                    case SaveStrategy.CustomConvertable:
                        var convertableDataBuffer = new SaveDataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        convertableDataBuffer.SetCustomSaveData(new Dictionary<string, object>());
                        var saveDataHandler = new SaveDataHandler(convertableDataBuffer, savableElementLookup, objectReferenceLookup, index);
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

            var saveData = new Dictionary<string, object>();
            objectSaveDataBuffer.SetDefinedSaveData(saveData);
            
            foreach (var (objectName, obj) in savableElement.MemberInfoList)
            {
                if (obj == null)
                {
                    saveData.Add(objectName, null);
                }
                else
                {
                    if (savableElementLookup.TryGetValue(obj, out var foundSaveElement))
                    {
                        saveData.Add(objectName, foundSaveElement.SaveStrategy == SaveStrategy.Serializable ? 
                            foundSaveElement.Obj : foundSaveElement.CreatorGuidPath);
                    }
                    else if (referenceLookup.TryGetValue(obj, out GuidPath path))
                    {
                        saveData.Add(objectName, path);
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

        private void HandleInterfaceOnSave(object saveObject, SaveDataBuffer objectSaveDataBuffer, SavableElementLookup savableElementLookup, Dictionary<object, GuidPath> objectReferenceLookup, int index)
        {
            if (!TypeUtility.TryConvertTo(saveObject, out ISavable objectSavable)) return;
            
            objectSaveDataBuffer.SetCustomSaveData(new Dictionary<string, object>());
            objectSavable.OnSave(new SaveDataHandler(objectSaveDataBuffer, savableElementLookup, objectReferenceLookup, index));
        }

        #endregion

        #region LoadMethods

        private void InstantiatePrefabsOnLoad(SaveDataBufferContainer saveDataBufferContainer, List<Savable> savableList, List<(string, string)> currentPrefabList)
        {
            var instantiatedSavables = saveDataBufferContainer.PrefabList.Except(currentPrefabList);
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
        
        private void DestroyPrefabsOnLoad(SaveDataBufferContainer saveDataBufferContainer, List<Savable> savableList, List<(string, string)> currentPrefabList)
        {
            var destroyedSavables = currentPrefabList.Except(saveDataBufferContainer.PrefabList);
            foreach (var (prefab, sceneGuid) in destroyedSavables)
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
                var savableGuidPath = new GuidPath(null, savable.SceneGuid);
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath, componentContainer.guid);
                    saveElementLookup.Add(componentGuidPath.ToString(), componentContainer.unityObject);
                }
            }

            return saveElementLookup;
        }
        
        private Dictionary<GuidPath, object> PrepareSaveElementInstances(SaveDataBufferContainer saveDataBufferContainer, List<Savable> savableList, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var createdObjectsLookup = new Dictionary<GuidPath, object>();
            
            foreach (var (guidPath, saveDataBuffer) in saveDataBufferContainer.SaveDataBuffers)
            {
                switch (saveDataBuffer.saveStrategy)
                {
                    case SaveStrategy.NotSupported:
                        Debug.LogWarning($"The object of type {saveDataBuffer.SavableType} is not supported!");
                        break;
                    
                    case SaveStrategy.UnityObject:
                        HandleUnityObject(savableList, saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
                        break;
                    
                    case SaveStrategy.AutomaticSavable:
                        var savableObjectInstance = Activator.CreateInstance(saveDataBuffer.SavableType);
                        
                        WriteSavableMember(savableObjectInstance, saveDataBuffer.DefinedSaveData, deserializeReferenceBuilder);
                        HandleOnLoadInterface(savableObjectInstance, saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);

                        createdObjectsLookup.Add(guidPath, savableObjectInstance);
                        break;
                    
                    case SaveStrategy.CustomSavable:
                        var savableInterfaceInstance = Activator.CreateInstance(saveDataBuffer.SavableType);
                        
                        HandleOnLoadInterface(savableInterfaceInstance, saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);

                        createdObjectsLookup.Add(guidPath, savableInterfaceInstance);
                        break;

                    case SaveStrategy.CustomConvertable:
                        if (ConverterRegistry.TryGetConverter(saveDataBuffer.SavableType, out IConvertable convertable))
                        {
                            var loadDataHandler = new LoadDataHandler(saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
                            convertable.OnLoad(loadDataHandler);
                        }
                        break;
                    
                    case SaveStrategy.Serializable:
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
                                
                    WriteSavableMember(componentContainer.unityObject, saveDataBuffer.DefinedSaveData, deserializeReferenceBuilder);
                    HandleOnLoadInterface(componentContainer.unityObject, saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
                                
                    createdObjectsLookup.Add(guidPath, componentContainer.unityObject);
                    return;
                }
            }
        }
        
        private void WriteSavableMember(object memberOwner, Dictionary<string, object> savableMember, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            if (savableMember == null) return;
            
            foreach (var (identifier, obj) in savableMember)
            {
                if (obj is GuidPath referenceGuidPath)
                {
                    deserializeReferenceBuilder.EnqueueReferenceBuilding(referenceGuidPath, targetObject => 
                        ReflectionUtility.TryApplyMemberValue(memberOwner, identifier, targetObject, true));
                }
                else
                {
                    ReflectionUtility.TryApplyMemberValue(memberOwner, identifier, obj);
                }
            }
        }
        
        private void HandleOnLoadInterface(object loadObject, SaveDataBuffer saveDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder, 
            Dictionary<GuidPath, object> createdObjectsLookup, GuidPath guidPath)
        {
            if (!TypeUtility.TryConvertTo(loadObject, out ISavable objectSavable)) return;
            
            objectSavable.OnLoad(new LoadDataHandler(saveDataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath));
        }

        #endregion
    }
}
