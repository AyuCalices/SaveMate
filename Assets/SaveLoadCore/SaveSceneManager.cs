using System;
using System.Collections.Generic;
using System.Reflection;
using SaveLoadCore.Utility;
using UnityEngine;

namespace SaveLoadCore
{
    public class SaveSceneManager : MonoBehaviour
    {
        [ContextMenu("Save Scene Data")]
        public void SaveSceneData()
        {
            var savableList = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var saveElementLookup = BuildSaveElementLookup(savableList);
            var dataBufferContainer = BuildDataBufferContainer(saveElementLookup);
            SaveLoadManager.Save(dataBufferContainer);
        }

        [ContextMenu("Load Scene Data")]
        public void LoadSceneData()
        {
            var savableList = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var dataBufferContainer = SaveLoadManager.Load<DataBufferContainer>();
            var sceneElementComposite = CreateSaveElements(dataBufferContainer, savableList);
            ApplySaveElementsReferences(dataBufferContainer, sceneElementComposite);
        }

        private SaveElementLookup BuildSaveElementLookup(List<Savable> savableList)
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

        private void ProcessSavableElement(SaveElementLookup saveElementLookup, object targetObject, GuidPath guidPath)
        {
            //if the fields and properties was found once, it shall not be created again to avoid a stackoverflow by cyclic references
            if (targetObject == null || saveElementLookup.Elements.ContainsKey(targetObject)) return;

            var memberList = new List<(MemberInfo MemberInfo, object MemberObject)>();
            var saveElement = new SaveElement()
            {
                CreatorPath = guidPath,
                MemberList = memberList,
                SavableObject = targetObject
            };
            saveElementLookup.Elements.Add(targetObject, saveElement);

            var fieldInfoList = ReflectionUtility.GetFieldInfos<SavableAttribute>(targetObject.GetType());
            foreach (var fieldInfo in fieldInfoList)
            {
                var reflectedField = fieldInfo.GetValue(targetObject);
                memberList.Add((fieldInfo, reflectedField));

                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedField is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, fieldInfo.Name);
                ProcessSavableElement(saveElementLookup, reflectedField, path);
            }

            var propertyInfoList = ReflectionUtility.GetPropertyInfos<SavableAttribute>(targetObject.GetType());
            foreach (var propertyInfo in propertyInfoList)
            {
                var reflectedProperty = propertyInfo.GetValue(targetObject);
                memberList.Add((propertyInfo, reflectedProperty));

                //UnityEngine.Object always exists on a guidPath depth of 2. Processing it would result in a wrong guidPath
                if (reflectedProperty is UnityEngine.Component) continue;
                
                var path = new GuidPath(guidPath, propertyInfo.Name);
                ProcessSavableElement(saveElementLookup, reflectedProperty, path);
            }
        }

        private DataBufferContainer BuildDataBufferContainer(SaveElementLookup saveElementLookup)
        {
            var dataBufferContainer = new DataBufferContainer();

            foreach (var (saveObject, saveElement) in saveElementLookup.Elements)
            {
                var creatorGuidPath = saveElement.CreatorPath;

                //UnityEngine.Object must always be handled as a reference, so it needs a guidPath.
                //In that matter it doesn't matter for them if they contain member marked as [Save].
                if (saveObject is UnityEngine.Object)
                {
                    if (saveObject is UnityEngine.Component)
                    {
                        var saveType = saveElement.SavableObject.GetType();
                        var saveMember = new List<(string, object)>();
                        dataBufferContainer.SaveBuffers.Add(new ComponentDataBuffer(saveType, saveElement.CreatorPath, saveMember));
                        
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
                    var saveType = saveElement.SavableObject.GetType();
                    var saveMember = new List<(string, object)>();
                    dataBufferContainer.SaveBuffers.Add(new ObjectDataBuffer(saveType, saveElement.CreatorPath, saveMember));
                    
                    FillSaveMember(saveElement, saveElementLookup, saveMember);
                }
                //TODO: Serialize custom types
                //If an object doesn't have any member, it is expected, that it is C# Serializable.
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
        
        private void FillSaveMember(SaveElement saveElement, SaveElementLookup saveElementLookup, List<(string, object)> saveMember)
        {
            //memberList elements are always saved as references -> either from scene or from save data
            foreach (var (memberInfo, memberObject) in saveElement.MemberList)
            {
                switch (memberObject)
                {
                    case null:
                        saveMember.Add((memberInfo.Name, null));
                        break;
                    default:
                    {
                        if (saveElementLookup.Elements.TryGetValue(memberObject,
                                out var foundSaveElement))
                        {
                            saveMember.Add((memberInfo.Name, foundSaveElement.CreatorPath));
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"You need to add a savable component to the origin GameObject of the '{memberObject.GetType()}' component. Then you need to apply an " +
                                $"ID by adding it into the ReferenceList. This will enable support for component referencing!");
                        }

                        break;
                    }
                }
            }
        }

        private SceneElementComposite CreateSaveElements(DataBufferContainer dataBufferContainer, List<Savable> savableList)
        {
            var sceneElementComposite = new SceneElementComposite(savableList);
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
                        newElementComposite.LoadMemberData(componentDataBuffer.SaveElements);
                        break;
                    }

                    //load data structure
                    if (!hasFoundComposite && currentComposite is ElementComposite currentElementComposite)
                    {
                        currentElementComposite.LoadObjectData(savablePath, saveBuffer);
                        break;
                    }

                    if (guidPath.Count == 0)
                    {
                        Debug.LogError("The origin of a path was not properly set up!");
                    }
                    
                    currentComposite = newComposite;
                }
            }

            return sceneElementComposite;
        }
        
        private void ApplySaveElementsReferences(DataBufferContainer dataBufferContainer, SceneElementComposite sceneElementComposite)
        {
            foreach (var saveContainerSaveBuffer in dataBufferContainer.SaveBuffers)
            {
                if (saveContainerSaveBuffer is not BaseReferencableDataBuffer attributeSaveBuffer) continue;
                
                var creatorBaseComposite = sceneElementComposite.FindTargetComposite(attributeSaveBuffer.OriginGuidPath.ToStack());
                foreach (var (memberName, obj) in attributeSaveBuffer.SaveElements)
                {
                    if (obj is GuidPath usagePath && creatorBaseComposite is ElementComposite creatorMemberComposite)
                    {
                        var memberInfo = creatorMemberComposite.MemberList.Find(x => x.Name == memberName);
                        if (memberInfo == null)
                        {
                            Debug.LogWarning("Wasn't able to find the corresponding member!");
                            continue;
                        }

                        if (sceneElementComposite.FindTargetComposite(usagePath.ToStack()) is not ElementComposite
                            targetComposite)
                        {
                            Debug.LogWarning("Wasn't able to find the corresponding composite!");
                            continue;
                        }
                            
                        switch (memberInfo)
                        {
                            case FieldInfo fieldInfo:
                                fieldInfo.SetValue(creatorMemberComposite.SavableObject, targetComposite.SavableObject);
                                break;
                            case PropertyInfo propertyInfo:
                                propertyInfo.SetValue(creatorMemberComposite.SavableObject, targetComposite.SavableObject);
                                break;
                        }
                    }
                }
            }
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
        public ComponentDataBuffer(Type savableType, GuidPath creatorGuidPath, List<(string fieldName, object obj)> saveElements) : base(savableType, creatorGuidPath, saveElements)
        {
        }
    }
    
    [Serializable]
    public class ObjectDataBuffer : BaseReferencableDataBuffer
    {
        public ObjectDataBuffer(Type savableType, GuidPath creatorGuidPath, List<(string fieldName, object obj)> saveElements) : base(savableType, creatorGuidPath, saveElements)
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
        public List<(string fieldName, object obj)> SaveElements;

        public BaseReferencableDataBuffer(Type savableType, GuidPath creatorGuidPath,
            List<(string fieldName, object obj)> saveElements)
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
    
    public class SaveElementLookup
    {
        public readonly Dictionary<object, SaveElement> Elements = new();
    }
    
    /// <summary>
    /// represents a savable object. every SavabeObject knows, where they are used, what memberInfo it has and which objects belong to this memberInfo
    /// </summary>
    public class SaveElement
    {
        public GuidPath CreatorPath;
        public object SavableObject;
        public List<(MemberInfo MemberInfo, object MemberObject)> MemberList;
    }
    
    public abstract class BaseElementComposite
    {
        protected readonly Dictionary<string, BaseElementComposite> Composite = new();
        
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
                Composite[fieldInfo.Name] = default;
            }
            
            var propertyList = ReflectionUtility.GetPropertyInfos<SavableAttribute>(SavableObject.GetType());
            foreach (var propertyInfo in propertyList)
            {
                MemberList.Add(propertyInfo);
                Composite[propertyInfo.Name] = default;
            }
        }
        
        public void LoadObjectData(string currentMemberName, IDataBuffer dataBuffer)
        {
            if (!Composite.ContainsKey(currentMemberName) || Composite[currentMemberName] != null)
            {
                Debug.LogWarning("The requirement to create the required SaveElement was not met!");
                return;
            }
            
            var memberInfo = MemberList.Find(x => x.Name == currentMemberName);
            if (memberInfo == null)
            {
                Debug.LogWarning("Wasn't able to find the corresponding member!");
                return;
            }
            
            switch (dataBuffer)
            {
                case ObjectDataBuffer objectSaveBuffer:
                {
                    object instance = Activator.CreateInstance(objectSaveBuffer.SavableType);
                    ApplyData(memberInfo, currentMemberName, instance, out ElementComposite elementComposite);
                    elementComposite.LoadMemberData(objectSaveBuffer.SaveElements);
                    break;
                }
                case SerializeDataBuffer serializeSaveBuffer:
                    ApplyData(memberInfo, currentMemberName, serializeSaveBuffer.Data, out _);
                    break;
                default:
                    Debug.LogWarning($"Type of {dataBuffer.GetType()} is not supported for loading!");
                    break;
            }
        }
        
        public void LoadMemberData(List<(string, object)> saveElements)
        {
            foreach (var (memberName, obj) in saveElements)
            {
                //References will be applied later, when all objects got created!
                if (obj is GuidPath) return;

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
                
                ApplyData(memberInfo, memberName, obj, out _);
            }
        }
        
        private void ApplyData(MemberInfo memberInfo, string memberName, object data, out ElementComposite elementComposite)
        {
            if (data != null && !data.GetType().IsSerializable)
            {
                Debug.LogWarning("not serializable!");
            }
            
            var fieldUsagePath = new GuidPath(CreatorPath, memberName);
            elementComposite = new ElementComposite(fieldUsagePath, data);
            Composite[memberName] = elementComposite;
                
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    fieldInfo.SetValue(SavableObject, data);
                    break;
                case PropertyInfo propertyInfo:
                    propertyInfo.SetValue(SavableObject, data);
                    break;
            }
        }
    }
}
