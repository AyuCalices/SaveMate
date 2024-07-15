using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SaveLoadCore.Utility;
using UnityEngine;

namespace SaveLoadCore
{
    /// TODO: IEnumerables must act similar to the member system, just that the gathering of the savable elements is different -> counts the same for an savable class attribute
    public class SaveSceneManager : MonoBehaviour
    {
        [ContextMenu("Save Scene Data")]
        public void SaveSceneData()
        {
            var savableList = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var saveElementLookup = BuildSaveElementCollections(savableList);
            var dataBufferContainer = BuildDataBufferContainer(saveElementLookup);
            SaveLoadManager.Save(dataBufferContainer);
        }

        [ContextMenu("Load Scene Data")]
        public void LoadSceneData()
        {
            var savableList = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var dataBufferContainer = SaveLoadManager.Load<DataBufferContainer>();
            
            var sceneElementComposite = new SceneElementComposite(savableList);
            var referenceBuilder = new ReferenceBuilder(sceneElementComposite);
            CreateSaveElements(dataBufferContainer, sceneElementComposite, referenceBuilder);
            referenceBuilder.BuildReferences();
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
                    ProcessSavableElement(saveElementLookup, componentContainer.component, componentGuidPath);
                }
                
                //TODO: by adding the reference list here, even those references will be saved!
                foreach (var componentContainer in savable.ReferenceList)
                {
                    var componentGuidPath = new GuidPath(savableGuidPath, componentContainer.guid);
                    ProcessSavableElement(saveElementLookup, componentContainer.component, componentGuidPath);
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
        private void ProcessSavableElement(SaveElementLookup saveElementLookup, object targetObject, GuidPath guidPath)
        {
            //if the fields and properties was found once, it shall not be created again to avoid a stackoverflow by cyclic references
            if (targetObject == null || saveElementLookup.ContainsElement(targetObject)) return;

            var memberList = new Dictionary<string, object>();
            var saveElement = new SaveElement()
            {
                CreatorPath = guidPath,
                Obj = targetObject,
                MemberList = memberList
            };
            saveElementLookup.AddElement(targetObject, saveElement);

            var fieldInfoList = ReflectionUtility.GetFieldInfos<SavableAttribute>(targetObject.GetType());
            foreach (var fieldInfo in fieldInfoList)
            {
                var reflectedField = fieldInfo.GetValue(targetObject);
                memberList.Add(fieldInfo.Name, reflectedField);

                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedField is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, fieldInfo.Name);
                ProcessSavableElement(saveElementLookup, reflectedField, path);
            }

            var propertyInfoList = ReflectionUtility.GetPropertyInfos<SavableAttribute>(targetObject.GetType());
            foreach (var propertyInfo in propertyInfoList)
            {
                var reflectedProperty = propertyInfo.GetValue(targetObject);
                memberList.Add(propertyInfo.Name, reflectedProperty);

                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedProperty is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, propertyInfo.Name);
                ProcessSavableElement(saveElementLookup, reflectedProperty, path);
            }
        }

        private DataBufferContainer BuildDataBufferContainer(SaveElementLookup saveElementLookup)
        {
            var dataBufferContainer = new DataBufferContainer();
            
            for (int index = 0; index < saveElementLookup.Count(); index++)
            {
                SaveElement saveElement = saveElementLookup.GetAt(index);
                var creatorGuidPath = saveElement.CreatorPath;
                var saveObject = saveElement.Obj;

                //UnityEngine.Object must always be handled as a reference, so it needs a guidPath.
                //In that matter it doesn't matter for them if they contain member marked as [Save].
                if (saveObject is UnityEngine.Object)
                {
                    if (saveObject is UnityEngine.Component)
                    {
                        //TODO: either field/property or method or class attribute system here
                        
                        var saveType = saveObject.GetType();
                        var saveMember = new Dictionary<string, object>();
                        dataBufferContainer.SaveBuffers.Add(new ComponentDataBuffer(saveType, creatorGuidPath, saveMember));
                        
                        FillSaveMember(saveElement, saveElementLookup, saveMember);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                //If a class or struct contains member marked as [Save], they are handled as references like UnityEngine.Object.
                //This makes sure, that member can be saved as references for custom classes and structs
                else if (saveElement.MemberList.Count != 0)
                {
                    //TODO: either field/property or method or class attribute system here
                    
                    var saveType = saveObject.GetType();
                    var saveMember = new Dictionary<string, object>();
                    dataBufferContainer.SaveBuffers.Add(new ObjectDataBuffer(saveType, creatorGuidPath, saveMember));
                    
                    FillSaveMember(saveElement, saveElementLookup, saveMember);
                }
                //If an object doesn't have any member: it is a serializable or has a converter
                else if (ConverterFactoryRegistry.TryGetConverter(saveObject.GetType(), out IConvertable convertable))
                {
                    var saveType = saveObject.GetType();
                    var saveMember = new Dictionary<string, object>();
                    var objectDataBuffer = new ObjectDataBuffer(saveType, creatorGuidPath, saveMember);
                    dataBufferContainer.SaveBuffers.Add(objectDataBuffer);
                    
                    convertable.OnSave(objectDataBuffer, saveObject, saveElementLookup, index);
                }
                //TODO: somehow save this directly on the saveBuffer
                else if (SerializationHelper.IsSerializable(saveObject.GetType()))
                {
                    dataBufferContainer.SaveBuffers.Add(new SerializeDataBuffer(saveObject, creatorGuidPath));
                }
                else
                {
                    Debug.LogWarning($"The object of type {saveObject.GetType()} is not supported!");
                }
            }

            return dataBufferContainer;
        }
        
        private void FillSaveMember(SaveElement saveElement, SaveElementLookup saveElementLookup, Dictionary<string, object> saveMember)
        {
            //memberList elements are always saved as references -> either from scene or from save data
            foreach (var (objectName, obj) in saveElement.MemberList)
            {
                switch (obj)
                {
                    case null:
                        saveMember.Add(objectName, null);
                        break;
                    default:
                    {
                        if (saveElementLookup.TryGetValue(obj,
                                out var foundSaveElement))
                        {
                            saveMember.Add(objectName, foundSaveElement.CreatorPath);
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"You need to add a savable component to the origin GameObject of the '{obj.GetType()}' component. Then you need to apply an " +
                                $"ID by adding it into the ReferenceList. This will enable support for component referencing!");
                        }

                        break;
                    }
                }
            }
        }

        private void CreateSaveElements(DataBufferContainer dataBufferContainer, SceneElementComposite sceneElementComposite, ReferenceBuilder referenceBuilder)
        {
            foreach (var saveBuffer in dataBufferContainer.SaveBuffers)
            {
                var guidPath = saveBuffer.OriginGuidPath.ToStack();
                
                BaseElementComposite currentComposite = sceneElementComposite;
                while (guidPath.Count != 0)
                {
                    var savablePath = guidPath.Pop();
                    bool hasFoundComposite = currentComposite.TryGetComposite(savablePath, out BaseElementComposite newComposite);

                    //load saved member of component
                    if (hasFoundComposite 
                        && currentComposite is SavableElementComposite 
                        && newComposite is ElementComposite newElementComposite
                        && saveBuffer is ComponentDataBuffer componentDataBuffer)
                    {
                        //TODO: either field/property or method or class attribute system here
                        newElementComposite.LoadMemberData(referenceBuilder, componentDataBuffer.SaveElements);
                        break;
                    }

                    //load data structure
                    if (!hasFoundComposite && currentComposite is ElementComposite currentElementComposite)
                    {
                        currentElementComposite.LoadObjectData(referenceBuilder, savablePath, saveBuffer);
                        break;
                    }

                    if (guidPath.Count == 0)
                    {
                        Debug.LogError("The origin of a path was not properly set up!");
                    }
                    
                    currentComposite = newComposite;
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

        public bool TryAddElement(object saveObject, SaveElement saveElement)
        {
            if (_objectLookup.TryAdd(saveObject, saveElement))
            {
                _saveElementList.Add(saveElement);
                return true;
            }

            Debug.LogWarning("Save Object already contained in the SaveElementLookup!");
            return false;
        }

        public void AddElement(object saveObject, SaveElement saveElement)
        {
            _objectLookup.Add(saveObject, saveElement);
            _saveElementList.Add(saveElement);
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
        public readonly List<IDataBuffer> SaveBuffers = new();
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
    
    /// <summary>
    /// Buffer just for serializable objects
    /// </summary>
    [Serializable]
    public class SerializeDataBuffer : IDataBuffer
    {
        public GuidPath OriginGuidPath { get; }
        
        public object Data;

        public SerializeDataBuffer(object data, GuidPath creatorGuidPath)
        {
            Data = data;
            OriginGuidPath = creatorGuidPath;
        }
    }

    //TODO: different types of guidPath -> each one will know how to be found
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
        public GuidPath CreatorPath;
        public object Obj;
        public Dictionary<string, object> MemberList;
    }
    
    public abstract class BaseElementComposite
    {
        public readonly Dictionary<string, BaseElementComposite> Composite = new();
        
        public BaseElementComposite FindTargetComposite(Stack<string> pathStack)
        {
            if (pathStack.Count == 0) return this;
            
            var element = pathStack.Pop();
            if (Composite.TryGetValue(element, out var value)) return value.FindTargetComposite(pathStack);
            
            Debug.LogWarning("Wasn't able to find the corresponding composite!");
            return null;
        }
        
        public bool TryGetComposite(string memberName, out BaseElementComposite composite)
        {
            bool hasValue = Composite.TryGetValue(memberName, out composite);
            return hasValue && composite != null;
        }
    }

    public class SceneElementComposite : BaseElementComposite
    {
        private readonly List<Savable> _sceneSavable;
        
        public SceneElementComposite(List<Savable> sceneSavable)
        {
            _sceneSavable = sceneSavable;

            Build();
        }

        private void Build()
        {
            foreach (var savable in _sceneSavable)
            {
                var nextGuidPath = new GuidPath(null, savable.SceneGuid);
                Composite.Add(savable.SceneGuid, new SavableElementComposite(nextGuidPath, savable));
            }
        }
    }

    public class SavableElementComposite : BaseElementComposite
    {
        private readonly GuidPath _creatorPath;
        private readonly Savable _savable;

        public SavableElementComposite(GuidPath creatorPath, Savable savable)
        {
            _creatorPath = creatorPath;
            _savable = savable;

            Build();
        }

        private void Build()
        {
            foreach (var componentContainer in _savable.SavableList)
            {
                var nextGuidPath = new GuidPath(_creatorPath, componentContainer.guid);
                Composite.Add(componentContainer.guid, new ElementComposite(nextGuidPath, componentContainer.component));
            }
            
            foreach (var componentContainer in _savable.ReferenceList)
            {
                var nextGuidPath = new GuidPath(_creatorPath, componentContainer.guid);
                Composite.Add(componentContainer.guid, new ElementComposite(nextGuidPath, componentContainer.component));
            }
        }
    }
    
    public class ElementComposite : BaseElementComposite
    {
        public readonly GuidPath CreatorPath;
        public readonly object SavableObject;
        public readonly List<MemberInfo> MemberList;

        public ElementComposite(GuidPath creatorPath, object savableObject)
        {
            CreatorPath = creatorPath;
            SavableObject = savableObject;
            MemberList = new List<MemberInfo>();

            Build();
        }

        private void Build()
        {
            if (SavableObject == null) return;
            
            var fieldList = ReflectionUtility.GetFieldInfos<SavableAttribute>(SavableObject.GetType());
            foreach (var fieldInfo in fieldList)
            {
                MemberList.Add(fieldInfo);
                Composite[fieldInfo.Name] = null;
            }
            
            var propertyList = ReflectionUtility.GetPropertyInfos<SavableAttribute>(SavableObject.GetType());
            foreach (var propertyInfo in propertyList)
            {
                MemberList.Add(propertyInfo);
                Composite[propertyInfo.Name] = null;
            }
        }
        
        public void LoadObjectData(ReferenceBuilder referenceBuilder, string currentMemberName, IDataBuffer dataBuffer)
        {
            if (!Composite.ContainsKey(currentMemberName) || Composite[currentMemberName] != null)
            {
                Debug.LogWarning("The requirement to create the required SaveElement was not met!");
                return;
            }
            
            //TODO: there might be a name dependant to a list, that needs to be resolved!
            var memberInfo = MemberList.Find(x => x.Name == currentMemberName);
            
            switch (dataBuffer)
            {
                case ObjectDataBuffer objectSaveBuffer:
                {
                    if (ConverterFactoryRegistry.TryGetConverter(objectSaveBuffer.SavableType, out IConvertable convertable))
                    {
                        convertable.OnLoad(objectSaveBuffer, referenceBuilder, data =>
                        {
                            UpdateComposite(this, currentMemberName, data, out ElementComposite newElementComposite);
                            if (memberInfo != null)
                            {
                                ReflectionUtility.ApplyMemberValue(memberInfo, SavableObject, data);
                            }
                            return newElementComposite;
                        });
                    }
                    else
                    {
                        if (memberInfo == null)
                        {
                            Debug.LogWarning("Wasn't able to find the corresponding member!");
                            return;
                        }
                        
                        object instance = Activator.CreateInstance(objectSaveBuffer.SavableType);
                        UpdateComposite(this, currentMemberName, instance, out ElementComposite elementComposite);
                        ReflectionUtility.ApplyMemberValue(memberInfo, SavableObject, instance);
                        elementComposite.LoadMemberData(referenceBuilder, objectSaveBuffer.SaveElements);
                    }

                    break;
                }
                case SerializeDataBuffer serializeSaveBuffer:
                    if (memberInfo == null)
                    {
                        Debug.LogWarning("Wasn't able to find the corresponding member!");
                        return;
                    }
                    
                    UpdateComposite(this, currentMemberName, serializeSaveBuffer.Data, out _);
                    ReflectionUtility.ApplyMemberValue(memberInfo, SavableObject, serializeSaveBuffer.Data);
                    break;
                default:
                    Debug.LogWarning($"Type of {dataBuffer.GetType()} is not supported for loading!");
                    break;
            }
        }
        
        public void LoadMemberData(ReferenceBuilder referenceBuilder, Dictionary<string, object> saveElements)
        {
            foreach (var (memberName, obj) in saveElements)
            {
                if (!Composite.ContainsKey(memberName))
                {
                    Debug.LogWarning("Wasn't able to find the corresponding composite!");
                    continue;
                }

                var memberInfo = MemberList.Find(x => x.Name == memberName);
                if (memberInfo == null)
                {
                    Debug.LogWarning("Wasn't able to find the corresponding member!");
                    continue;
                }
                
                //currently, everything is a guidPath
                if (obj is GuidPath targetGuidPath)
                {
                    referenceBuilder.StoreAction(targetGuidPath, composite => 
                        ReflectionUtility.ApplyMemberValue(memberInfo, SavableObject, composite.SavableObject));
                }
                //this should only happen, if it is serializable
                else
                {
                    UpdateComposite(this, memberName, obj, out _);
                    ReflectionUtility.ApplyMemberValue(memberInfo, SavableObject, obj);
                }
            }
        }
        
        public static void UpdateComposite(ElementComposite elementComposite, string memberName, object data, out ElementComposite newElementComposite)
        {
            var fieldUsagePath = new GuidPath(elementComposite.CreatorPath, memberName);
            newElementComposite = new ElementComposite(fieldUsagePath, data);
            elementComposite.Composite[memberName] = newElementComposite;
        }
    }
    
    public class ReferenceBuilder
    {
        private readonly List<Action> _actionList = new();

        private readonly SceneElementComposite _sceneElementComposite;

        public ReferenceBuilder(SceneElementComposite sceneElementComposite)
        {
            _sceneElementComposite = sceneElementComposite;
        }

        public void BuildReferences()
        {
            foreach (var action in _actionList)
            {
                action.Invoke();
            }
        }
        
        public void StoreAction(GuidPath targetGuidPath, Action<ElementComposite> onElementCompositeFound)
        {
            _actionList.Add(() =>
            {
                if (_sceneElementComposite.FindTargetComposite(targetGuidPath.ToStack()) is not ElementComposite targetComposite)
                {
                    Debug.LogWarning("Wasn't able to find the corresponding composite!");
                    return;
                }
                
                onElementCompositeFound.Invoke(targetComposite);
            });
        }
    }
}
