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
    public enum SaveStrategy
    {
        NotSupported,
        UnityObject,
        UnityComponent,
        AutomaticSavable,
        CustomSavable,
        CustomConvertable,
        Serializable
    }
    
    public class SaveSceneManager : MonoBehaviour
    {
        [ContextMenu("Save Scene Data")]
        public void SaveSceneData()
        {
            var savableList = UnityObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var objectReferenceLookup = BuildObjectReferenceLookup(savableList);
            var saveElementLookup = BuildSavableElementLookup(savableList);
            var dataBufferContainer = BuildDataBufferContainer(saveElementLookup, objectReferenceLookup);
            SaveLoadManager.Save(dataBufferContainer);
        }

        [ContextMenu("Load Scene Data")]
        public void LoadSceneData()
        {
            var savableList = UnityObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var dataBufferContainer = SaveLoadManager.Load<DataBufferContainer>();
            
            var referenceBuilder = new DeserializeReferenceBuilder();
            var createdObjectsLookup = PrepareSaveElementInstances(dataBufferContainer, savableList, referenceBuilder);
            var guidPathReferenceLookup = BuildGuidPathReferenceLookup(savableList);
            referenceBuilder.InvokeAll(createdObjectsLookup, guidPathReferenceLookup);
        }

        private Dictionary<object, GuidPath> BuildObjectReferenceLookup(List<Savable> savableList)
        {
            var saveElementLookup = new Dictionary<object, GuidPath>();
            
            foreach (var savable in savableList)
            {
                var savableGuidPath = new GuidPath(null, savable.SceneGuid);
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath, componentContainer.guid);
                    saveElementLookup.Add(componentContainer.component, componentGuidPath);
                }
            }

            return saveElementLookup;
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
                    ProcessSavableElement(saveElementLookup, componentContainer.component, componentGuidPath, saveElementLookup.Count());
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
                if (reflectedField is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, fieldInfo.Name);
                ProcessSavableElement(savableElementLookup, reflectedField, path, insertIndex);
            }
            
            foreach (var propertyInfo in propertyInfoList)
            {
                var reflectedProperty = propertyInfo.GetValue(targetObject);
                memberList.Add(propertyInfo.Name, reflectedProperty);

                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedProperty is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, propertyInfo.Name);
                ProcessSavableElement(savableElementLookup, reflectedProperty, path, insertIndex);
            }
            
            //define save strategy TODO: to strategy pattern
            if (targetObject is UnityEngine.Object)
            {
                if (targetObject is UnityEngine.Component)
                {
                    saveElement.SaveStrategy = SaveStrategy.UnityComponent;
                }
                else
                {
                    saveElement.SaveStrategy = SaveStrategy.UnityObject;
                }
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

        private DataBufferContainer BuildDataBufferContainer(SavableElementLookup savableElementLookup, Dictionary<object, GuidPath> referenceLookup)
        {
            var dataBufferContainer = new DataBufferContainer();
            
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
                    
                    case SaveStrategy.UnityComponent:
                        var componentDataBuffer = new DataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        HandleSavableMember(saveElement, componentDataBuffer, savableElementLookup, referenceLookup);
                        HandleInterfaceOnSave(saveObject, componentDataBuffer, savableElementLookup, index);
                        
                        dataBufferContainer.DataBuffers.Add(creatorGuidPath, componentDataBuffer);
                        break;
                    
                    case SaveStrategy.UnityObject:
                        break;
                    
                    case SaveStrategy.AutomaticSavable:
                        var savableObjectDataBuffer = new DataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        HandleSavableMember(saveElement, savableObjectDataBuffer, savableElementLookup, referenceLookup);
                        HandleInterfaceOnSave(saveObject, savableObjectDataBuffer, savableElementLookup, index);
                        
                        dataBufferContainer.DataBuffers.Add(creatorGuidPath, savableObjectDataBuffer);
                        break;
                    
                    case SaveStrategy.CustomSavable:
                        var savableDataBuffer = new DataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        HandleInterfaceOnSave(saveObject, savableDataBuffer, savableElementLookup, index);
                        
                        dataBufferContainer.DataBuffers.Add(creatorGuidPath, savableDataBuffer);
                        break;
                    
                    case SaveStrategy.CustomConvertable:
                        var convertableDataBuffer = new DataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        convertableDataBuffer.SetCustomSaveData(new Dictionary<string, object>());
                        var saveDataHandler = new SaveDataHandler(convertableDataBuffer, savableElementLookup, index);
                        ConverterRegistry.GetConverter(saveObject.GetType()).OnSave(saveObject, saveDataHandler);
                        
                        dataBufferContainer.DataBuffers.Add(creatorGuidPath, convertableDataBuffer);
                        break;

                    case SaveStrategy.Serializable:
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return dataBufferContainer;
        }

        private void HandleInterfaceOnSave(object saveObject, DataBuffer objectDataBuffer, SavableElementLookup savableElementLookup, int index)
        {
            if (!TypeUtility.TryConvertTo(saveObject, out ISavable objectSavable)) return;
            
            objectDataBuffer.SetCustomSaveData(new Dictionary<string, object>());
            objectSavable.OnSave(new SaveDataHandler(objectDataBuffer, savableElementLookup, index));
        }
        
        private void HandleSavableMember(SavableElement savableElement, DataBuffer objectDataBuffer, SavableElementLookup savableElementLookup, Dictionary<object, GuidPath> referenceLookup)
        {
            if (savableElement.MemberInfoList.Count == 0) return;

            var saveData = new Dictionary<string, object>();
            objectDataBuffer.SetDefinedSaveData(saveData);
            
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
                        //TODO: overthink the timing of this warning!
                        Debug.LogWarning(
                            $"You need to add a savable component to the origin GameObject of the '{obj.GetType()}' component. Then you need to apply an " +
                            $"ID by adding it into the ReferenceList. This will enable support for component referencing!");
                    }
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
                    saveElementLookup.Add(componentGuidPath.ToString(), componentContainer.component);
                }
            }

            return saveElementLookup;
        }

        private Dictionary<GuidPath, object> PrepareSaveElementInstances(DataBufferContainer dataBufferContainer, List<Savable> savableList, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var createdObjectsLookup = new Dictionary<GuidPath, object>();
            
            foreach (var (guidPath, dataBuffer) in dataBufferContainer.DataBuffers)
            {
                switch (dataBuffer.saveStrategy)
                {
                    case SaveStrategy.NotSupported:
                        Debug.LogWarning($"The object of type {dataBuffer.SavableType} is not supported!");
                        break;
                    
                    case SaveStrategy.UnityObject:
                        break;
                    
                    case SaveStrategy.UnityComponent:
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
                                
                                WriteSavableMember(componentContainer.component, dataBuffer.DefinedSaveData, deserializeReferenceBuilder);
                                HandleInterfaceOnLoad(componentContainer.component, dataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
                                
                                createdObjectsLookup.Add(guidPath, componentContainer.component);
                            }
                        }
                        break;
                    
                    case SaveStrategy.AutomaticSavable:
                        var savableObjectInstance = Activator.CreateInstance(dataBuffer.SavableType);
                        
                        WriteSavableMember(savableObjectInstance, dataBuffer.DefinedSaveData, deserializeReferenceBuilder);
                        HandleInterfaceOnLoad(savableObjectInstance, dataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);

                        createdObjectsLookup.Add(guidPath, savableObjectInstance);
                        break;
                    
                    case SaveStrategy.CustomSavable:
                        var savableInterfaceInstance = Activator.CreateInstance(dataBuffer.SavableType);
                        
                        HandleInterfaceOnLoad(savableInterfaceInstance, dataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);

                        createdObjectsLookup.Add(guidPath, savableInterfaceInstance);
                        break;

                    case SaveStrategy.CustomConvertable:
                        if (ConverterRegistry.TryGetConverter(dataBuffer.SavableType, out IConvertable convertable))
                        {
                            var loadDataHandler = new LoadDataHandler(dataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
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
        
        private void HandleInterfaceOnLoad(object loadObject, DataBuffer dataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder, 
            Dictionary<GuidPath, object> createdObjectsLookup, GuidPath guidPath)
        {
            if (!TypeUtility.TryConvertTo(loadObject, out ISavable objectSavable)) return;
            
            objectSavable.OnLoad(new LoadDataHandler(dataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath));
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
    }
}
