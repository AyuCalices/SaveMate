using System;
using System.Collections.Generic;
using System.Data;
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
        SavableMemberObject,
        ObjectConverter,
        Serializable
    }
    
    /// TODO: IEnumerables must act similar to the member system, just that the gathering of the savable elements is different -> counts the same for an savable class attribute
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

            var fieldInfoList = ReflectionUtility.GetFieldInfos<SavableAttribute>(targetObject.GetType());
            foreach (var fieldInfo in fieldInfoList)
            {
                var reflectedField = fieldInfo.GetValue(targetObject);
                memberList.Add(fieldInfo.Name, reflectedField);
                
                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedField is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, fieldInfo.Name);
                ProcessSavableElement(saveElementLookup, reflectedField, path, insertIndex);
            }

            var propertyInfoList = ReflectionUtility.GetPropertyInfos<SavableAttribute>(targetObject.GetType());
            foreach (var propertyInfo in propertyInfoList)
            {
                var reflectedProperty = propertyInfo.GetValue(targetObject);
                memberList.Add(propertyInfo.Name, reflectedProperty);

                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedProperty is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, propertyInfo.Name);
                ProcessSavableElement(saveElementLookup, reflectedProperty, path, insertIndex);
            }
            
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
                saveElement.SaveStrategy = SaveStrategy.SavableMemberObject;
            }
            else if (ConverterFactoryRegistry.HasConverter(targetObject.GetType()))
            {
                saveElement.SaveStrategy = SaveStrategy.ObjectConverter;
            }
            else
            {
                saveElement.SaveStrategy = SaveStrategy.Serializable;
            }
        }

        private DataBufferContainer BuildDataBufferContainer(SaveElementLookup saveElementLookup)
        {
            var dataBufferContainer = new DataBufferContainer();
            
            for (int index = 0; index < saveElementLookup.Count(); index++)
            {
                SaveElement saveElement = saveElementLookup.GetAt(index);
                var creatorGuidPath = saveElement.CreatorGuidPath;
                var saveObject = saveElement.Obj;
                var saveData = new Dictionary<string, object>();
                
                switch (saveElement.SaveStrategy)
                {
                    case SaveStrategy.UnityComponent:
                        dataBufferContainer.SaveBuffers.Add(creatorGuidPath, new ComponentDataBuffer(saveObject.GetType(), creatorGuidPath, saveData));
                        FillSaveMember(saveElement, saveElementLookup, saveData);
                        break;
                    
                    case SaveStrategy.UnityObject:
                        break;
                        //throw new NotImplementedException();
                    
                    case SaveStrategy.SavableMemberObject:
                        dataBufferContainer.SaveBuffers.Add(creatorGuidPath, new ObjectDataBuffer(saveObject.GetType(), creatorGuidPath, saveData));
                        FillSaveMember(saveElement, saveElementLookup, saveData);
                        break;
                    
                    case SaveStrategy.ObjectConverter:
                        var converter = ConverterFactoryRegistry.GetConverter(saveObject.GetType());
                        var objectDataBuffer = new ObjectDataBuffer(saveObject.GetType(), creatorGuidPath, saveData);
                        var serializeReferenceBuilder = new SerializeReferenceBuilder(objectDataBuffer, saveElementLookup, index);
                        converter.OnSave(saveObject, serializeReferenceBuilder);
                        dataBufferContainer.SaveBuffers.Add(creatorGuidPath, objectDataBuffer);
                        break;
                    
                    case SaveStrategy.Serializable:
                        continue;
                    
                    case SaveStrategy.NotSupported:
                        Debug.LogWarning($"The object of type {saveObject.GetType()} is not supported!");
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return dataBufferContainer;
        }
        
        private void FillSaveMember(SaveElement saveElement, SaveElementLookup saveElementLookup, Dictionary<string, object> saveMember)
        {
            //memberList elements are always saved as references -> either from scene or from save data
            foreach (var (objectName, obj) in saveElement.MemberInfoList)
            {
                switch (obj)
                {
                    case null:
                        saveMember.Add(objectName, null);
                        break;
                    default:
                    {
                        if (saveElementLookup.TryGetValue(obj, out var foundSaveElement))
                        {
                            if (foundSaveElement.SaveStrategy == SaveStrategy.Serializable)
                            {
                                saveMember.Add(objectName, foundSaveElement.Obj);
                            }
                            else
                            {
                                saveMember.Add(objectName, foundSaveElement.CreatorGuidPath);
                            }
                        }
                        else
                        {
                            //TODO: overthink the timing of this warning!
                            Debug.LogWarning(
                                $"You need to add a savable component to the origin GameObject of the '{obj.GetType()}' component. Then you need to apply an " +
                                $"ID by adding it into the ReferenceList. This will enable support for component referencing!");
                        }

                        break;
                    }
                }
            }
        }

        private Dictionary<GuidPath, object> PrepareSaveElementInstances(DataBufferContainer dataBufferContainer, List<Savable> savableList, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var createdObjectsLookup = new Dictionary<GuidPath, object>();
            
            foreach (var (guidPath, dataBuffer) in dataBufferContainer.SaveBuffers)
            {
                if (dataBuffer is ComponentDataBuffer componentDataBuffer)
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
                            WriteSavableMember(componentContainer.component, componentDataBuffer.SaveElements, deserializeReferenceBuilder);
                            createdObjectsLookup.Add(guidPath, componentContainer.component);
                        }
                    }
                }
                else if (dataBuffer is ObjectDataBuffer objectDataBuffer)
                {
                    if (ConverterFactoryRegistry.TryGetConverter(objectDataBuffer.SavableType, out IConvertable convertable))
                    {
                        var instance = convertable.OnLoad(objectDataBuffer, deserializeReferenceBuilder);
                        createdObjectsLookup.Add(guidPath, instance);
                    }
                    else
                    {
                        var instance = Activator.CreateInstance(objectDataBuffer.SavableType);
                        WriteSavableMember(instance, objectDataBuffer.SaveElements, deserializeReferenceBuilder);
                        createdObjectsLookup.Add(guidPath, instance);
                    }
                }
            }

            return createdObjectsLookup;
        }

        private void WriteSavableMember(object memberOwner, Dictionary<string, object> savableMember, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            foreach (var (identifier, obj) in savableMember)
            {
                if (obj is GuidPath referenceGuidPath)
                {
                    deserializeReferenceBuilder.EnqueueAction(referenceGuidPath, targetObject => 
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
        public readonly Dictionary<GuidPath, IDataBuffer> SaveBuffers = new();
    }

    public interface IDataBuffer
    {
        GuidPath OriginGuidPath { get; }
    }
    
    [Serializable]
    public class ComponentDataBuffer : BaseReferencableDataBuffer
    {
        public ComponentDataBuffer(Type savableType, GuidPath creatorGuidPath, Dictionary<string, object> saveElements) : base(savableType, creatorGuidPath, saveElements)
        {
        }
    }
    
    [Serializable]
    public class ObjectDataBuffer : BaseReferencableDataBuffer
    {
        public ObjectDataBuffer(Type savableType, GuidPath creatorGuidPath, Dictionary<string, object> saveElements) : base(savableType, creatorGuidPath, saveElements)
        {
        }
    }

    //
    /// <summary>
    /// Buffer for anything, that is serialized by this save system. Only this Buffer supports references.
    /// </summary>
    [Serializable]
    public class BaseReferencableDataBuffer : IDataBuffer
    {
        public GuidPath OriginGuidPath { get; }
        
        public Type SavableType;
        public Dictionary<string, object> SaveElements;

        public BaseReferencableDataBuffer(Type savableType, GuidPath creatorGuidPath,
            Dictionary<string, object> saveElements)
        {
            SavableType = savableType;
            OriginGuidPath = creatorGuidPath;
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

    public class SerializeReferenceBuilder
    {
        private readonly ObjectDataBuffer _objectDataBuffer;
        private readonly SaveElementLookup _saveElementLookup;
        private readonly int _currentIndex;

        public SerializeReferenceBuilder(ObjectDataBuffer objectDataBuffer, SaveElementLookup saveElementLookup, int currentIndex)
        {
            _objectDataBuffer = objectDataBuffer;
            _saveElementLookup = saveElementLookup;
            _currentIndex = currentIndex;
        }

        public void AddSerializable(string uniqueIdentifier, object obj)
        {
            _objectDataBuffer.SaveElements.Add(uniqueIdentifier, obj);
        }

        public object ToSavableObject(string uniqueIdentifier, object obj)
        {
            var guidPath = new GuidPath(_objectDataBuffer.OriginGuidPath, uniqueIdentifier);
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
            AddSerializable(uniqueIdentifier, ToSavableObject(uniqueIdentifier, obj));
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
        
        public void EnqueueAction(object obj, Action<object> onElementCompositeFound)
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
                    
                    onElementCompositeFound.Invoke(value);
                }
                else
                {
                    onElementCompositeFound.Invoke(obj);
                }
            });
        }
        
        public void EnqueueAction(object[] objectGroup, Action<object[]> onElementCompositeFound)
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

                onElementCompositeFound.Invoke(convertedGroup);
            });
        }
    }
}
