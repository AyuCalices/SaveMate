using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadCore.Utility;
using UnityEngine;

namespace SaveLoadCore
{
    public enum SaveStrategy
    {
        NotSupported,
        UnityObject,
        UnityComponent,
        SavableObject,
        ISavable,
        IConvertable,
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

        private SaveElementLookup BuildSaveElementCollections(List<Savable> savableList)
        {
            var saveElementLookup = new SaveElementLookup();
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
        public static void ProcessSavableElement(SaveElementLookup saveElementLookup, object targetObject, GuidPath guidPath, int insertIndex)
        {
            //if the fields and properties was found once, it shall not be created again to avoid a stackoverflow by cyclic references
            if (targetObject == null || saveElementLookup.ContainsElement(targetObject)) return;

            var memberList = new Dictionary<string, object>();
            var saveElement = new SaveElement()
            {
                SaveStrategy = SaveStrategy.NotSupported,
                CreatorGuidPath = guidPath,
                Obj = targetObject,
                MemberInfoList = memberList
            };
            
            saveElementLookup.InsertElement(insertIndex, saveElement);
            insertIndex++;

            //recursion with field and property members
            var fieldInfoList = ReflectionUtility.GetFieldInfos<SavableMemberAttribute>(targetObject.GetType());
            foreach (var fieldInfo in fieldInfoList)
            {
                var reflectedField = fieldInfo.GetValue(targetObject);
                memberList.Add(fieldInfo.Name, reflectedField);
                
                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedField is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, fieldInfo.Name);
                ProcessSavableElement(saveElementLookup, reflectedField, path, insertIndex);
            }

            var propertyInfoList = ReflectionUtility.GetPropertyInfos<SavableMemberAttribute>(targetObject.GetType());
            foreach (var propertyInfo in propertyInfoList)
            {
                var reflectedProperty = propertyInfo.GetValue(targetObject);
                memberList.Add(propertyInfo.Name, reflectedProperty);

                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedProperty is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, propertyInfo.Name);
                ProcessSavableElement(saveElementLookup, reflectedProperty, path, insertIndex);
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
                saveElement.SaveStrategy = SaveStrategy.SavableObject;
            }
            else if (targetObject is ISavable)
            {
                saveElement.SaveStrategy = SaveStrategy.ISavable;
            }
            else if (ConverterRegistry.HasConverter(targetObject.GetType()))
            {
                saveElement.SaveStrategy = SaveStrategy.IConvertable;
            }
            else
            {
                saveElement.SaveStrategy = SaveStrategy.Serializable;
            }
        }

        private DataBufferContainer BuildDataBufferContainer(SaveElementLookup saveElementLookup)
        {
            var dataBufferContainer = new DataBufferContainer();
            
            for (var index = 0; index < saveElementLookup.Count(); index++)
            {
                var saveElement = saveElementLookup.GetAt(index);
                var creatorGuidPath = saveElement.CreatorGuidPath;
                var saveObject = saveElement.Obj;
                
                switch (saveElement.SaveStrategy)
                {
                    case SaveStrategy.NotSupported:
                        Debug.LogWarning($"The object of type {saveObject.GetType()} is not supported!");
                        break;
                    
                    case SaveStrategy.UnityComponent:
                        var componentSaveData = new Dictionary<string, object>();
                        var componentDataBuffer = new DataBuffer(SaveStrategy.UnityComponent, creatorGuidPath, saveObject.GetType(), componentSaveData);
                        
                        HandleSavableMember(saveElement, saveElementLookup, componentSaveData);
                        HandleInterfaceOnSave(saveObject, componentDataBuffer, saveElementLookup, index);
                        
                        dataBufferContainer.DataBuffers.Add(creatorGuidPath, componentDataBuffer);
                        break;
                    
                    case SaveStrategy.UnityObject:
                        break;
                    
                    case SaveStrategy.SavableObject:
                        var savableObjectData = new Dictionary<string, object>();
                        var savableObjectDataBuffer = new DataBuffer(SaveStrategy.SavableObject, creatorGuidPath, saveObject.GetType(), savableObjectData);
                        
                        HandleSavableMember(saveElement, saveElementLookup, savableObjectData);
                        HandleInterfaceOnSave(saveObject, savableObjectDataBuffer, saveElementLookup, index);
                        
                        dataBufferContainer.DataBuffers.Add(creatorGuidPath, savableObjectDataBuffer);
                        break;
                    
                    case SaveStrategy.ISavable:
                        var savableSaveData = new Dictionary<string, object>();
                        var savableDataBuffer = new DataBuffer(SaveStrategy.SavableObject, creatorGuidPath, saveObject.GetType(), savableSaveData);
                        
                        HandleInterfaceOnSave(saveObject, savableDataBuffer, saveElementLookup, index);
                        
                        dataBufferContainer.DataBuffers.Add(creatorGuidPath, savableDataBuffer);
                        break;
                    
                    case SaveStrategy.IConvertable:
                        var convertableSaveData = new Dictionary<string, object>();
                        var convertableDataBuffer = new DataBuffer(SaveStrategy.SavableObject, creatorGuidPath, saveObject.GetType(), convertableSaveData);
                        
                        var saveDataHandler = new SaveDataHandler(convertableDataBuffer, saveElementLookup, index);
                        ConverterRegistry.GetConverter(saveObject.GetType()).OnSave(saveObject, saveDataHandler);
                        
                        dataBufferContainer.DataBuffers.Add(creatorGuidPath, convertableDataBuffer);
                        break;

                    case SaveStrategy.Serializable:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return dataBufferContainer;
        }

        private void HandleInterfaceOnSave(object saveObject, DataBuffer objectDataBuffer, SaveElementLookup saveElementLookup, int index)
        {
            if (!ReflectionUtility.TryConvertTo(saveObject, out ISavable objectSavable)) return;
            
            objectSavable.OnSave(new SaveDataHandler(objectDataBuffer, saveElementLookup, index));
        }
        
        private void HandleSavableMember(SaveElement saveElement, SaveElementLookup saveElementLookup, Dictionary<string, object> saveMember)
        {
            foreach (var (objectName, obj) in saveElement.MemberInfoList)
            {
                if (obj == null)
                {
                    saveMember.Add(objectName, null);
                }
                else
                {
                    if (saveElementLookup.TryGetValue(obj, out var foundSaveElement))
                    {
                        saveMember.Add(objectName, foundSaveElement.SaveStrategy == SaveStrategy.Serializable ? 
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
                                
                                WriteSavableMember(componentContainer.component, dataBuffer.SaveElements, deserializeReferenceBuilder);
                                HandleInterfaceOnLoad(componentContainer.component, dataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
                                
                                createdObjectsLookup.Add(guidPath, componentContainer.component);
                            }
                        }
                        break;
                    
                    case SaveStrategy.SavableObject:
                        var savableObjectInstance = Activator.CreateInstance(dataBuffer.SavableType);
                        
                        WriteSavableMember(savableObjectInstance, dataBuffer.SaveElements, deserializeReferenceBuilder);
                        HandleInterfaceOnLoad(savableObjectInstance, dataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);

                        createdObjectsLookup.Add(guidPath, savableObjectInstance);
                        break;
                    
                    case SaveStrategy.ISavable:
                        var savableInterfaceInstance = Activator.CreateInstance(dataBuffer.SavableType);
                        
                        HandleInterfaceOnLoad(savableInterfaceInstance, dataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);

                        createdObjectsLookup.Add(guidPath, savableInterfaceInstance);
                        break;

                    case SaveStrategy.IConvertable:
                        if (ConverterRegistry.TryGetConverter(dataBuffer.SavableType, out IConvertable convertable))
                        {
                            var loadDataHandler = new LoadDataHandler(dataBuffer, deserializeReferenceBuilder, createdObjectsLookup, guidPath);
                            convertable.OnLoad(loadDataHandler);
                        }
                        break;
                    
                    case SaveStrategy.Serializable:
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

    public class SaveElementLookup
    {
        private readonly Dictionary<object, SaveElement> _objectLookup = new();
        private readonly List<SaveElement> _saveElementList = new();

        public bool ContainsElement(object saveObject)
        {
            return _objectLookup.ContainsKey(saveObject);
        }

        public bool TryAddElement(SaveElement saveElement)
        {
            if (_objectLookup.TryAdd(saveElement.Obj, saveElement))
            {
                _saveElementList.Add(saveElement);
                return true;
            }

            Debug.LogWarning("Save Object already contained in the SaveElementLookup!");
            return false;
        }

        public void AddElement(SaveElement saveElement)
        {
            _objectLookup.Add(saveElement.Obj, saveElement);
            _saveElementList.Add(saveElement);
        }
        
        public void InsertElement(int index, SaveElement saveElement)
        {
            _objectLookup.Add(saveElement.Obj, saveElement);
            _saveElementList.Insert(index, saveElement);
        }

        public bool TryGetValue(object saveObject, out SaveElement saveElement)
        {
            return _objectLookup.TryGetValue(saveObject, out saveElement);
        }

        public int Count()
        {
            return _saveElementList.Count;
        }

        public SaveElement GetAt(int index)
        {
            return _saveElementList[index];
        }

        public bool TryInsertAt(int index, object saveObject, SaveElement saveElement)
        {
            if (_objectLookup.TryAdd(saveObject, saveElement))
            {
                _saveElementList.Insert(index, saveElement);
                return true;
            }

            Debug.LogWarning("Save Object already contained in the SaveElementLookup!");
            return false;
        }
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
        public Dictionary<string, object> SaveElements;

        public DataBuffer(SaveStrategy saveStrategy, GuidPath creatorGuidPath, Type savableType, Dictionary<string, object> saveElements)
        {
            this.saveStrategy = saveStrategy;
            originGuidPath = creatorGuidPath;
            SavableType = savableType;
            SaveElements = saveElements;
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
    
    /// <summary>
    /// represents a savable object. every SavabeObject knows, where they are used, what memberInfo it has and which objects belong to this memberInfo
    /// </summary>
    public class SaveElement
    {
        public SaveStrategy SaveStrategy;
        public GuidPath CreatorGuidPath;
        public object Obj;
        public Dictionary<string, object> MemberInfoList;
    }

    public class SaveDataHandler
    {
        private readonly DataBuffer _objectDataBuffer;
        private readonly SaveElementLookup _saveElementLookup;
        private readonly int _currentIndex;

        public SaveDataHandler(DataBuffer objectDataBuffer, SaveElementLookup saveElementLookup, int currentIndex)
        {
            _objectDataBuffer = objectDataBuffer;
            _saveElementLookup = saveElementLookup;
            _currentIndex = currentIndex;
        }

        public void AddSerializable(string uniqueIdentifier, object obj)
        {
            _objectDataBuffer.SaveElements.Add(uniqueIdentifier, obj);
        }

        public object ToReferencableObject(string uniqueIdentifier, object obj)
        {
            var guidPath = new GuidPath(_objectDataBuffer.originGuidPath, uniqueIdentifier);
            if (!_saveElementLookup.ContainsElement(obj))
            {
                SaveSceneManager.ProcessSavableElement(_saveElementLookup, obj, guidPath, _currentIndex + 1);
            }
                
            if (_saveElementLookup.TryGetValue(obj, out SaveElement saveElement))
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
            return (T)_loadDataBuffer.SaveElements[identifier];
        }

        public object GetSaveElement(string identifier)
        {
            return _loadDataBuffer.SaveElements[identifier];
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
