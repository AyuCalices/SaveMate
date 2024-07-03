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
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            SavableLookupSave savableLookupSave = BuildSceneLookup(savableComponents);
            SaveContainer saveContainer = CreateSerializeSaveData(savableLookupSave);
            SaveLoadManager.Save(saveContainer);
        }

        [ContextMenu("Load Scene Data")]
        public void LoadSceneData()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var data = SaveLoadManager.Load<SaveContainer>();
            BuildScene(data, savableComponents);
        }

        private SavableLookupSave BuildSceneLookup(List<Savable> savableComponents)
        {
            SavableLookupSave objectLookupSave = new SavableLookupSave();
            foreach (var savable in savableComponents)
            {
                UsagePath savablePath = new UsagePath(null, savable.SceneGuid);
                
                foreach (var componentContainer in savable.SavableList)
                {
                    UsagePath componentPath = new UsagePath(savablePath, componentContainer.guid);
                    ProcessComponent(objectLookupSave, componentContainer.component, componentPath);
                }
                
                foreach (var componentContainer in savable.ReferenceList)
                {
                    UsagePath componentPath = new UsagePath(savablePath, componentContainer.guid);
                    ProcessComponent(objectLookupSave, componentContainer.component, componentPath);
                }
            }

            return objectLookupSave;
        }

        private void ProcessComponent(SavableLookupSave objectLookupSave, object reflectedObject, UsagePath usagePath)
        {
            //if the fields and properties was found once, it shall not be created again to avoid stackoverflow by cyclic references
            if (reflectedObject == null || objectLookupSave.SceneObjectDataList.ContainsKey(reflectedObject)) return;

            var memberList = new List<(MemberInfo MemberInfo, object MemberObject)>();
            SceneObjectData sceneObjectData = new SceneObjectData()
            {
                CreatorPath = usagePath,
                MemberList = memberList,
                SavableObject = reflectedObject
            };
            objectLookupSave.SceneObjectDataList.Add(reflectedObject, sceneObjectData);

            var fieldInfos = ReflectionUtility.GetFieldInfos<SavableAttribute>(reflectedObject.GetType());
            foreach (var fieldInfo in fieldInfos)
            {
                var reflectedField = fieldInfo.GetValue(reflectedObject);
                memberList.Add((fieldInfo, reflectedField));

                if (reflectedField is UnityEngine.Component) continue;
                UsagePath path = new UsagePath(usagePath, fieldInfo.Name);
                ProcessComponent(objectLookupSave, reflectedField, path);
            }

            var propertyInfos = ReflectionUtility.GetPropertyInfos<SavableAttribute>(reflectedObject.GetType());
            foreach (var propertyInfo in propertyInfos)
            {
                var reflectedProperty = propertyInfo.GetValue(reflectedObject);
                memberList.Add((propertyInfo, reflectedProperty));

                if (reflectedProperty is UnityEngine.Component) continue;
                UsagePath path = new UsagePath(usagePath, propertyInfo.Name);
                ProcessComponent(objectLookupSave, reflectedProperty, path);
            }
        }

        private SaveContainer CreateSerializeSaveData(SavableLookupSave lookupSave)
        {
            SaveContainer saveContainer = new SaveContainer();

            foreach (var (savableObject, sceneObjectData) in lookupSave.SceneObjectDataList)
            {
                var creatorGuidPath = sceneObjectData.CreatorPath;

                //object may has member
                if (savableObject is UnityEngine.Object)
                {
                    if (savableObject is UnityEngine.Component)
                    {
                        AddComponentSaveBuffer(sceneObjectData, saveContainer, lookupSave);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else if (sceneObjectData.MemberList.Count != 0)
                {
                    AddComponentSaveBuffer(sceneObjectData, saveContainer, lookupSave);
                }
                //object has never members at this point
                else if (SerializationHelper.IsSerializable(savableObject.GetType()))
                {
                    saveContainer.SaveBuffers.Add(new SerializeSaveBuffer(savableObject, creatorGuidPath));
                }
                else
                {
                    Debug.LogError($"The object of type {savableObject.GetType()} is not supported!");
                }
            }

            return saveContainer;
        }
        
        private void AddComponentSaveBuffer(SceneObjectData sceneObjectData, SaveContainer saveContainer, SavableLookupSave lookupSave)
        {
            Type saveType = sceneObjectData.SavableObject.GetType();
            List<(string fieldName, object obj)> saveElements = new();
            saveContainer.SaveBuffers.Add(new ComponentSaveBuffer(saveType, sceneObjectData.CreatorPath, saveElements));

            //memberList elements are always saved as references -> either from scene or from save data
            foreach (var (memberInfo, memberObject) in sceneObjectData.MemberList)
            {
                switch (memberObject)
                {
                    case null:
                        saveElements.Add((memberInfo.Name, null));
                        break;
                    default:
                    {
                        if (lookupSave.SceneObjectDataList.TryGetValue(memberObject,
                                out SceneObjectData referencedSceneObjectData))
                        {
                            saveElements.Add((memberInfo.Name, referencedSceneObjectData.CreatorPath));
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

        private void BuildScene(SaveContainer saveContainer, List<Savable> savables)
        {
            SceneComposite sceneComposite = new SceneComposite(savables);
            foreach (var saveBuffer in saveContainer.SaveBuffers)
            {
                var guidPath = saveBuffer.OriginGuidPath.GetPath();

                var savablePath = guidPath.Pop();
                if (!sceneComposite.TryGetComposite(savablePath, out SavableComposite savableComposite))
                {
                    Debug.LogError("Scene Composite doesn't contain a the path to the savable");
                }

                var componentPath = guidPath.Pop();
                if (!savableComposite.TryGetComposite(componentPath, out MemberComposite memberComposite))
                {
                    Debug.LogError("Savable Composite doesn't contain a the path to the component");
                }

                //walk existing member path
                MemberComposite currentComposite = memberComposite;
                while (guidPath.Count != 0)
                {
                    if (!currentComposite.TryGetOrCreateComposite(guidPath, saveBuffer, out MemberComposite newComposite))
                    {
                        Debug.LogError("Savable Composite doesn't contain a the path to the component");
                    }
                    currentComposite = newComposite;
                }

                if (saveBuffer is ComponentSaveBuffer attributeSaveBuffer)
                {
                    currentComposite.ApplySaveElementsToMember(attributeSaveBuffer.SaveElements);
                }
            }
            
            foreach (var saveContainerSaveBuffer in saveContainer.SaveBuffers)
            {
                if (saveContainerSaveBuffer is ComponentSaveBuffer attributeSaveBuffer)
                {
                    var creatorBaseComposite = sceneComposite.FindTargetComposite(attributeSaveBuffer.OriginGuidPath.GetPath());
                    foreach (var (memberName, obj) in attributeSaveBuffer.SaveElements)
                    {
                        if (obj is UsagePath usagePath && creatorBaseComposite is MemberComposite creatorMemberComposite)
                        {
                            var memberInfo = creatorMemberComposite.MemberList.Find(x => x.Name == memberName);
                            if (memberInfo == null)
                            {
                                continue;
                            }

                            if (sceneComposite.FindTargetComposite(usagePath.GetPath()) is not MemberComposite targetComposite) continue;
                            
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
    }

    [Serializable]
    public class SaveContainer
    {
        public readonly List<ISaveBuffer> SaveBuffers = new();
    }

    public interface ISaveBuffer
    {
        UsagePath OriginGuidPath { get; }
    }

    [Serializable]
    public class ComponentSaveBuffer : ISaveBuffer
    {
        public UsagePath OriginGuidPath { get; }
        
        public Type SavableType;
        public List<(string fieldName, object obj)> SaveElements;

        public ComponentSaveBuffer(Type savableType, UsagePath creatorGuidPath,
            List<(string fieldName, object obj)> saveElements)
        {
            SavableType = savableType;
            OriginGuidPath = creatorGuidPath;
            SaveElements = saveElements;
        }
    }
    
    [Serializable]
    public class SerializeSaveBuffer : ISaveBuffer
    {
        public UsagePath OriginGuidPath { get; }
        
        public object Data;

        public SerializeSaveBuffer(object data, UsagePath creatorGuidPath)
        {
            Data = data;
            OriginGuidPath = creatorGuidPath;
        }
    }

    public class SavableLookupSave
    {
        public readonly Dictionary<object, SceneObjectData> SceneObjectDataList = new();
    }

    [Serializable]
    public class UsagePath
    {
        private readonly UsagePath _parentPath;
        private readonly string _identifier;

        public UsagePath(UsagePath parentPath, string identifier)
        {
            _parentPath = parentPath;
            _identifier = identifier;
        }

        public Stack<string> GetPath()
        {
            Stack<string> path = new Stack<string>();

            UsagePath currentPath = this;
            while (currentPath != null)
            {
                path.Push(currentPath._identifier);
                currentPath = currentPath._parentPath;
            }

            return path;
        }

        public override string ToString()
        {
            string path = "";
            
            UsagePath currentPath = this;
            while (currentPath != null)
            {
                path = currentPath._identifier + "/" + path;
                currentPath = currentPath._parentPath;
            }

            return path;
        }
    }
    
    public abstract class BaseComposite
    {
        public readonly Dictionary<string, BaseComposite> Composite = new();
        
        public BaseComposite FindTargetComposite(Stack<string> pathStack)
        {
            if (pathStack.Count == 0)
            {
                return this;
            }
            
            var element = pathStack.Pop();
            if (!Composite.TryGetValue(element, out var value))
            {
                Debug.LogError("Element was not found!");
                return null;
            }

            return value.FindTargetComposite(pathStack);
        }
    }

    public class SceneComposite : BaseComposite
    {
        private readonly List<Savable> _sceneSavable;
        
        public SceneComposite(List<Savable> sceneSavable)
        {
            _sceneSavable = sceneSavable;

            Build();
        }

        private void Build()
        {
            foreach (var savable in _sceneSavable)
            {
                UsagePath nextPath = new UsagePath(null, savable.SceneGuid);
                Composite.Add(savable.SceneGuid, new SavableComposite(nextPath, savable));
            }
        }

        public bool TryGetComposite(string memberName, out SavableComposite savableComposite)
        {
            bool result = Composite.TryGetValue(memberName, out BaseComposite composite);
            savableComposite = composite as SavableComposite;
            return result;
        }
    }

    public class SavableComposite : BaseComposite
    {
        public readonly UsagePath CreatorPath;
        public Savable Savable;

        public SavableComposite(UsagePath creatorPath, Savable savable)
        {
            CreatorPath = creatorPath;
            Savable = savable;

            Build();
        }

        private void Build()
        {
            foreach (var componentsContainer in Savable.SavableList)
            {
                UsagePath nextPath = new UsagePath(CreatorPath, componentsContainer.guid);
                Composite.Add(componentsContainer.guid, new MemberComposite(nextPath, componentsContainer.component));
            }
            
            foreach (var componentsContainer in Savable.ReferenceList)
            {
                UsagePath nextPath = new UsagePath(CreatorPath, componentsContainer.guid);
                Composite.Add(componentsContainer.guid, new MemberComposite(nextPath, componentsContainer.component));
            }
        }

        public bool TryGetComposite(string memberName, out MemberComposite memberComposite)
        {
            bool result = Composite.TryGetValue(memberName, out BaseComposite composite);
            memberComposite = composite as MemberComposite;
            return result;
        }
    }
    
    public class MemberComposite : BaseComposite
    {
        public readonly UsagePath CreatorPath;
        public readonly object SavableObject;
        public readonly List<MemberInfo> MemberList;

        public MemberComposite(UsagePath creatorPath, object savableObject)
        {
            CreatorPath = creatorPath;
            SavableObject = savableObject;
            MemberList = new();

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
                if (obj is null or UsagePath)
                {
                    //References will be applied later, when all objects got created!
                    return;
                }
                
                if (!Composite.ContainsKey(fieldName)) continue;

                var memberInfo = MemberList.Find(x => x.Name == fieldName);
                if (memberInfo == null) continue;
                
                //if is not a class, there is no reference to keep track of -> just add it
                var fieldUsagePath = new UsagePath(CreatorPath, fieldName);
                Composite[fieldName] = new MemberComposite(fieldUsagePath, obj);
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
        
        public bool TryGetOrCreateComposite(Stack<string> path, ISaveBuffer saveBuffer, out MemberComposite memberComposite)
        {
            memberComposite = default;
            var memberName = path.Pop();

            if (TryGetComposite(memberName, out memberComposite) && memberComposite != null)
            {
                return true;
            }

            if (Composite.ContainsKey(memberName) && Composite[memberName] == null && path.Count == 0)
            {
                var memberInfo = MemberList.Find(x => x.Name == memberName);
                if (memberInfo == null) return false;
                
                var fieldUsagePath = new UsagePath(CreatorPath, memberName);
                if (saveBuffer is SerializeSaveBuffer serializeSaveBuffer)
                {
                    memberComposite = new MemberComposite(fieldUsagePath, serializeSaveBuffer.Data);
                    Composite[memberName] = memberComposite;
                
                    switch (memberInfo)
                    {
                        case FieldInfo fieldInfo:
                            fieldInfo.SetValue(SavableObject, serializeSaveBuffer.Data);
                            break;
                        case PropertyInfo propertyInfo:
                            propertyInfo.SetValue(SavableObject, serializeSaveBuffer.Data);
                            break;
                    }
                }
                else if (saveBuffer is ComponentSaveBuffer attributeSaveBuffer)
                {
                    object instance = Activator.CreateInstance(attributeSaveBuffer.SavableType);
                    
                    memberComposite = new MemberComposite(fieldUsagePath, instance);
                    Composite[memberName] = memberComposite;
                
                    switch (memberInfo)
                    {
                        case FieldInfo fieldInfo:
                            fieldInfo.SetValue(SavableObject, instance);
                            break;
                        case PropertyInfo propertyInfo:
                            propertyInfo.SetValue(SavableObject, instance);
                            break;
                    }
                }

                return true;
            }
            
            return false;
        }

        private bool TryGetComposite(string memberName, out MemberComposite memberComposite)
        {
            bool result = Composite.TryGetValue(memberName, out BaseComposite composite);
            memberComposite = composite as MemberComposite;
            return result;
        }
    }

    /// <summary>
    /// represents a savable object. every SavabeObject knows, where they are used, what memberInfo it has and which objects belong to this memberInfo
    /// </summary>
    public class SceneObjectData
    {
        public UsagePath CreatorPath;
        public object SavableObject;
        public List<(MemberInfo MemberInfo, object MemberObject)> MemberList;
    }
}
