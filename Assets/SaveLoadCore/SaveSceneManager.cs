using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaveLoadCore.Utility;
using UnityEngine;

namespace SaveLoadCore
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
            var saveElementLookup = BuildSaveElementCollections(savableList);
            var dataBufferContainer = BuildDataBufferContainer(saveElementLookup);
            SaveLoadManager.Save(dataBufferContainer);
        }

        [ContextMenu("Load Scene Data")]
        public void LoadSceneData()
        {
            var savableList = UnityObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var dataBufferContainer = SaveLoadManager.Load<DataBufferContainer>();
            
            var referenceBuilder = new DeserializeReferenceBuilder();
            var createdObjectsLookup = PrepareSaveElementInstances(dataBufferContainer, savableList, referenceBuilder);
            referenceBuilder.InvokeAll(createdObjectsLookup);
        }

        private SavableElementLookup BuildSaveElementCollections(List<Savable> savableList)
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
                
                //TODO: by adding the reference list here, even those references will be saved!
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath, componentContainer.guid);
                    ProcessSavableElement(saveElementLookup, componentContainer.component, componentGuidPath, saveElementLookup.Count());
                }
            }

            return saveElementLookup;
        }

        //target object can have: field/properties | method | class -> but can also be in a collection
        //field/property -> already supports references -> memberSystem
        //method -> not implemented & no references -> converterSystem with memberGathering
        //class -> same like field/property, just different gathering of members -> memberSystem
        //converter -> implemented, but no references -> converterSystem
        //enumerable -> not implemented & no references -> need converter system, because memberSystem doesnt apply
        
        //serializale things should be saved on the object and not as reference -> value types doesnt work: if something is struct with a reference savable on it, it will cause problems -> this is optimisation which will be tacled at the end!
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
            if (ReflectionUtility.ClassHasAttribute<SavableAttribute>(targetObject.GetType()))
            {
                fieldInfoList = ReflectionUtility.GetFieldInfos(targetObject.GetType());
                propertyInfoList = ReflectionUtility.GetPropertyInfos(targetObject.GetType());
            }
            else
            {
                fieldInfoList = ReflectionUtility.GetFieldInfosWithAttribute<SavableMemberAttribute>(targetObject.GetType());
                propertyInfoList = ReflectionUtility.GetPropertyInfosWithAttribute<SavableMemberAttribute>(targetObject.GetType());
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

        private DataBufferContainer BuildDataBufferContainer(SavableElementLookup savableElementLookup)
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
                        
                        HandleSavableMember(saveElement, componentDataBuffer, savableElementLookup);
                        HandleInterfaceOnSave(saveObject, componentDataBuffer, savableElementLookup, index);
                        
                        dataBufferContainer.DataBuffers.Add(creatorGuidPath, componentDataBuffer);
                        break;
                    
                    case SaveStrategy.UnityObject:
                        break;
                    
                    case SaveStrategy.AutomaticSavable:
                        var savableObjectDataBuffer = new DataBuffer(saveElement.SaveStrategy, creatorGuidPath, saveObject.GetType());
                        
                        HandleSavableMember(saveElement, savableObjectDataBuffer, savableElementLookup);
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
            if (!ReflectionUtility.TryConvertTo(saveObject, out ISavable objectSavable)) return;
            
            objectDataBuffer.SetCustomSaveData(new Dictionary<string, object>());
            objectSavable.OnSave(new SaveDataHandler(objectDataBuffer, savableElementLookup, index));
        }
        
        private void HandleSavableMember(SavableElement savableElement, DataBuffer objectDataBuffer, SavableElementLookup savableElementLookup)
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
            if (!ReflectionUtility.TryConvertTo(loadObject, out ISavable objectSavable)) return;
            
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

    public class SavableElementLookup
    {
        private readonly Dictionary<object, SavableElement> _objectLookup = new();
        private readonly List<SavableElement> _saveElementList = new();

        public bool ContainsElement(object saveObject)
        {
            return _objectLookup.ContainsKey(saveObject);
        }
        
        public void InsertElement(int index, SavableElement savableElement)
        {
            _objectLookup.Add(savableElement.Obj, savableElement);
            _saveElementList.Insert(index, savableElement);
        }

        public bool TryGetValue(object saveObject, out SavableElement savableElement)
        {
            return _objectLookup.TryGetValue(saveObject, out savableElement);
        }

        public int Count()
        {
            return _saveElementList.Count;
        }

        public SavableElement GetAt(int index)
        {
            return _saveElementList[index];
        }
    }
    
    public class SavableElement
    {
        public SaveStrategy SaveStrategy;
        public GuidPath CreatorGuidPath;
        public object Obj;
        public Dictionary<string, object> MemberInfoList;
    }

    [Serializable]
    public class DataBufferContainer
    {
        public readonly Dictionary<GuidPath, DataBuffer> DataBuffers = new();
    }

    /// <summary>
    /// Buffer for anything, that is serialized by this save system. Only this Buffer supports references.
    /// </summary>
    [Serializable]
    public class DataBuffer
    {
        public SaveStrategy saveStrategy;
        public GuidPath originGuidPath;
        public Type SavableType;
        public Dictionary<string, object> DefinedSaveData;
        public Dictionary<string, object> CustomSaveData;

        public DataBuffer(SaveStrategy saveStrategy, GuidPath creatorGuidPath, Type savableType)
        {
            this.saveStrategy = saveStrategy;
            originGuidPath = creatorGuidPath;
            SavableType = savableType;
        }

        public void SetDefinedSaveData(Dictionary<string, object> definedSaveData)
        {
            DefinedSaveData = definedSaveData;
        }

        public void SetCustomSaveData(Dictionary<string, object> customSaveData)
        {
            CustomSaveData = customSaveData;
        }
    }

    [Serializable]
    public class GuidPath
    {
        private readonly GuidPath _parent;
        private readonly string _guid;

        public GuidPath(GuidPath parent, string guid)
        {
            _parent = parent;
            _guid = guid;
        }

        public Stack<string> ToStack()
        {
            var stack = new Stack<string>();

            var currentPath = this;
            while (currentPath != null)
            {
                stack.Push(currentPath._guid);
                currentPath = currentPath._parent;
            }

            return stack;
        }

        public override string ToString()
        {
            var pathString = "";
            
            var currentPath = this;
            while (currentPath != null)
            {
                pathString = currentPath._guid + "/" + pathString;
                currentPath = currentPath._parent;
            }

            return pathString;
        }
    }

    public class SaveDataHandler
    {
        private readonly DataBuffer _objectDataBuffer;
        private readonly SavableElementLookup _savableElementLookup;
        private readonly int _currentIndex;

        public SaveDataHandler(DataBuffer objectDataBuffer, SavableElementLookup savableElementLookup, int currentIndex)
        {
            _objectDataBuffer = objectDataBuffer;
            _savableElementLookup = savableElementLookup;
            _currentIndex = currentIndex;
        }

        public void AddSerializable(string uniqueIdentifier, object obj)
        {
            _objectDataBuffer.CustomSaveData.Add(uniqueIdentifier, obj);
        }

        public object ToReferencableObject(string uniqueIdentifier, object obj)
        {
            var guidPath = new GuidPath(_objectDataBuffer.originGuidPath, uniqueIdentifier);
            if (!_savableElementLookup.ContainsElement(obj))
            {
                SaveSceneManager.ProcessSavableElement(_savableElementLookup, obj, guidPath, _currentIndex + 1);
            }
                
            if (_savableElementLookup.TryGetValue(obj, out SavableElement saveElement))
            {
                return saveElement.SaveStrategy == SaveStrategy.Serializable ? saveElement.Obj : saveElement.CreatorGuidPath;
            }

            throw new InvalidOperationException("The object could not be processed or retrieved from the save element lookup.");
        }
        
        public void AddReferencable(string uniqueIdentifier, object obj)
        {
            AddSerializable(uniqueIdentifier, ToReferencableObject(uniqueIdentifier, obj));
        }
    }

    public class LoadDataHandler
    {
        private readonly DataBuffer _loadDataBuffer;
        private readonly DeserializeReferenceBuilder _deserializeReferenceBuilder;
        private readonly Dictionary<GuidPath, object> _createdObjectsLookup;
        private readonly GuidPath _guidPath;

        public LoadDataHandler(DataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder, Dictionary<GuidPath, object> createdObjectsLookup, GuidPath guidPath)
        {
            _loadDataBuffer = loadDataBuffer;
            _deserializeReferenceBuilder = deserializeReferenceBuilder;
            _createdObjectsLookup = createdObjectsLookup;
            _guidPath = guidPath;
        }

        public T GetSaveElement<T>(string identifier)
        {
            return (T)_loadDataBuffer.CustomSaveData[identifier];
        }

        public object GetSaveElement(string identifier)
        {
            return _loadDataBuffer.CustomSaveData[identifier];
        }

        public void InitializeInstance(object obj)
        {
            _createdObjectsLookup.Add(_guidPath, obj);
        }

        public void EnqueueReferenceBuilding(object obj, Action<object> onReferenceFound)
        {
            _deserializeReferenceBuilder.EnqueueReferenceBuilding(obj, onReferenceFound);
        }

        public void EnqueueReferenceBuilding(object[] objectGroup, Action<object[]> onReferenceFound)
        {
            _deserializeReferenceBuilder.EnqueueReferenceBuilding(objectGroup, onReferenceFound);
        }
    }
    
    public class DeserializeReferenceBuilder
    {
        private readonly Queue<Action<Dictionary<GuidPath, object>>> _actionList = new();
        
        public void InvokeAll(Dictionary<GuidPath, object> createdObjectsLookup)
        {
            while (_actionList.Count != 0)
            {
                _actionList.Dequeue().Invoke(createdObjectsLookup);
            }
        }
        
        public void EnqueueReferenceBuilding(object obj, Action<object> onReferenceFound)
        {
            _actionList.Enqueue(createdObjectsLookup =>
            {
                if (obj is GuidPath guidPath)
                {
                    if (!createdObjectsLookup.TryGetValue(guidPath, out object value))
                    {
                        Debug.LogWarning("Wasn't able to find the created object!");
                        return;
                    }
                    
                    onReferenceFound.Invoke(value);
                }
                else
                {
                    onReferenceFound.Invoke(obj);
                }
            });
        }
        
        public void EnqueueReferenceBuilding(object[] objectGroup, Action<object[]> onReferenceFound)
        {
            _actionList.Enqueue(createdObjectsLookup =>
            {
                var convertedGroup = new object[objectGroup.Length];
                
                for (var index = 0; index < objectGroup.Length; index++)
                {
                    if (objectGroup[index] is GuidPath guidPath)
                    {
                        if (!createdObjectsLookup.TryGetValue(guidPath, out object value))
                        {
                            Debug.LogWarning("Wasn't able to find the created object!");
                            return;
                        }

                        convertedGroup[index] = value;
                    }
                    else
                    {
                        convertedGroup[index] = objectGroup[index];
                    }
                }

                onReferenceFound.Invoke(convertedGroup);
            });
        }
    }
}
