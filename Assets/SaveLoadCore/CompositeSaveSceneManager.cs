using System;
using System.Collections.Generic;
using System.Reflection;
using SaveLoadCore.Utility;
using UnityEngine;

namespace SaveLoadCore
{
    public class CompositeSaveSceneManager : MonoBehaviour
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

                if (reflectedField is UnityEngine.Component)
                {
                    Debug.LogWarning($"Tried to process an object that derives from {nameof(UnityEngine.Component)}. " +
                                     $"This is not allowed, because they always exist on a {nameof(GuidPath)} depth of 2!");
                    continue;
                }
                var path = new GuidPath(guidPath, fieldInfo.Name);
                ProcessSavableElement(saveElementLookup, reflectedField, path);
            }

            var propertyInfoList = ReflectionUtility.GetPropertyInfos<SavableAttribute>(targetObject.GetType());
            foreach (var propertyInfo in propertyInfoList)
            {
                var reflectedProperty = propertyInfo.GetValue(targetObject);
                memberList.Add((propertyInfo, reflectedProperty));

                if (reflectedProperty is UnityEngine.Component)
                {
                    Debug.LogWarning($"Tried to process an object that derives from {nameof(UnityEngine.Component)}. " +
                                     $"This is not allowed, because they always exist on a {nameof(GuidPath)} depth of 2!");
                    continue;
                }
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
                        AddComponentSaveBuffer(saveElement, dataBufferContainer, saveElementLookup);
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
                    AddComponentSaveBuffer(saveElement, dataBufferContainer, saveElementLookup);
                }
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
        
        private void AddComponentSaveBuffer(SaveElement saveElement, DataBufferContainer dataBufferContainer, SaveElementLookup saveElementLookup)
        {
            var saveType = saveElement.SavableObject.GetType();
            var saveMember = new List<(string fieldName, object obj)>();
            dataBufferContainer.SaveBuffers.Add(new ComponentDataBuffer(saveType, saveElement.CreatorPath, saveMember));

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

                //add savables to composite
                var savablePath = guidPath.Pop();
                if (!sceneElementComposite.TryGetComposite(savablePath, out SavableElementComposite savableComposite))
                {
                    Debug.LogWarning("Scene Composite doesn't contain a the path to the savable");
                    continue;
                }

                //add savable-components to composite
                var componentPath = guidPath.Pop();
                if (!savableComposite.TryGetComposite(componentPath, out ElementComposite memberComposite))
                {
                    Debug.LogWarning("Savable Composite doesn't contain a the path to the component");
                    continue;
                }

                //iterate over nested classes/structs
                ElementComposite currentComposite = memberComposite;
                while (guidPath.Count != 0)
                {
                    if (!currentComposite.TryGetOrCreateComposite(guidPath, saveBuffer, out ElementComposite newComposite))
                    {
                        Debug.LogWarning("Savable Composite doesn't contain a the path to the component");
                        continue;
                    }
                    currentComposite = newComposite;
                }

                //apply data
                if (saveBuffer is ComponentDataBuffer attributeSaveBuffer)
                {
                    currentComposite.ApplySaveElementsToMember(attributeSaveBuffer.SaveElements);
                }
                else
                {
                    Debug.LogWarning($"The type {saveBuffer.GetType()} is not a {nameof(ComponentDataBuffer)}, which is required!");
                }
            }

            return sceneElementComposite;
        }
        
        private void ApplySaveElementsReferences(DataBufferContainer dataBufferContainer, SceneElementComposite sceneElementComposite)
        {
            foreach (var saveContainerSaveBuffer in dataBufferContainer.SaveBuffers)
            {
                if (saveContainerSaveBuffer is not ComponentDataBuffer attributeSaveBuffer)
                {
                    Debug.LogWarning($"The type {saveContainerSaveBuffer.GetType()} is not a {nameof(ComponentDataBuffer)}, which is required!");
                    continue;
                }
                
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
    public class ComponentDataBuffer : IDataBuffer
    {
        public GuidPath OriginGuidPath { get; }
        
        public Type SavableType;
        public List<(string fieldName, object obj)> SaveElements;

        public ComponentDataBuffer(Type savableType, GuidPath creatorGuidPath,
            List<(string fieldName, object obj)> saveElements)
        {
            SavableType = savableType;
            OriginGuidPath = creatorGuidPath;
            SaveElements = saveElements;
        }
    }
    
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

        public bool TryGetComposite(string memberName, out SavableElementComposite savableElementComposite)
        {
            var containsValue = Composite.TryGetValue(memberName, out BaseElementComposite composite);
            savableElementComposite = composite as SavableElementComposite;
            return containsValue;
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

        public bool TryGetComposite(string memberName, out ElementComposite elementComposite)
        {
            var containsValue = Composite.TryGetValue(memberName, out BaseElementComposite composite);
            elementComposite = composite as ElementComposite;
            return containsValue;
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

        public void ApplySaveElementsToMember(List<(string, object)> saveElements)
        {
            foreach (var (fieldName, obj) in saveElements)
            {
                //References will be applied later, when all objects got created!
                if (obj is null or GuidPath) return;

                if (!Composite.ContainsKey(fieldName))
                {
                    Debug.LogWarning("Wasn't able to find the corresponding composite!");
                    continue;
                }

                var memberInfo = MemberList.Find(x => x.Name == fieldName);
                if (memberInfo == null)
                {
                    Debug.LogWarning("Wasn't able to find the corresponding member!");
                    continue;
                }
                
                //if is not a class, there is no reference to keep track of -> just add it
                var fieldUsagePath = new GuidPath(CreatorPath, fieldName);
                Composite[fieldName] = new ElementComposite(fieldUsagePath, obj);
                switch (memberInfo)
                {
                    case FieldInfo fieldInfo:
                        fieldInfo.SetValue(SavableObject, obj);
                        break;
                    case PropertyInfo propertyInfo:
                        propertyInfo.SetValue(SavableObject, obj);
                        break;
                }
            }
        }
        
        public bool TryGetOrCreateComposite(Stack<string> path, IDataBuffer dataBuffer, out ElementComposite elementComposite)
        {
            elementComposite = default;
            var memberName = path.Pop();

            if (TryGetComposite(memberName, out elementComposite) && elementComposite != null) return true;

            if (!Composite.ContainsKey(memberName) || Composite[memberName] != null || path.Count != 0)
            {
                Debug.LogWarning("The requirement to create the required SaveElement was not met!");
                return false;
            }
            
            var memberInfo = MemberList.Find(x => x.Name == memberName);
            if (memberInfo == null)
            {
                Debug.LogWarning("Wasn't able to find the corresponding member!");
                return false;
            }
                
            var fieldUsagePath = new GuidPath(CreatorPath, memberName);
            switch (dataBuffer)
            {
                case SerializeDataBuffer serializeSaveBuffer:
                    elementComposite = new ElementComposite(fieldUsagePath, serializeSaveBuffer.Data);
                    Composite[memberName] = elementComposite;
                
                    switch (memberInfo)
                    {
                        case FieldInfo fieldInfo:
                            fieldInfo.SetValue(SavableObject, serializeSaveBuffer.Data);
                            break;
                        case PropertyInfo propertyInfo:
                            propertyInfo.SetValue(SavableObject, serializeSaveBuffer.Data);
                            break;
                    }

                    break;
                case ComponentDataBuffer attributeSaveBuffer:
                {
                    //TODO: even though this never gonna happen for components, because they always lie on a guidPth depth of 2, its still the same data object
                    object instance = Activator.CreateInstance(attributeSaveBuffer.SavableType);
                    
                    elementComposite = new ElementComposite(fieldUsagePath, instance);
                    Composite[memberName] = elementComposite;
                
                    switch (memberInfo)
                    {
                        case FieldInfo fieldInfo:
                            fieldInfo.SetValue(SavableObject, instance);
                            break;
                        case PropertyInfo propertyInfo:
                            propertyInfo.SetValue(SavableObject, instance);
                            break;
                    }

                    break;
                }
                default:
                    Debug.LogWarning($"Type of {dataBuffer.GetType()} is not supported for creation!");
                    break;
            }

            return true;
        }

        private bool TryGetComposite(string memberName, out ElementComposite elementComposite)
        {
            var containsValue = Composite.TryGetValue(memberName, out BaseElementComposite composite);
            elementComposite = composite as ElementComposite;
            return containsValue;
        }
    }
}
