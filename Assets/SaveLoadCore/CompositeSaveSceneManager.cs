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
            var objectToGuidPathLookup = BuildObjectToGuidPathLookup(savableComponents);
            
            SaveContainer saveContainer = CreateSerializeSaveData(savableLookupSave, objectToGuidPathLookup);
            SaveLoadManager.Save(saveContainer);
        }

        [ContextMenu("Load Scene Data")]
        public void LoadSceneData()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var guidPathToObjectLookup = BuildGuidPathToObjectLookup(savableComponents);
            
            //TODO: prepare tree structure of current -> problem: cyclic references -> build it on the fly when it is used to apply save data? Or two iterations: one will build dependant of save data (-> probably better because of prefabs)and the other performs serialization

            var data = SaveLoadManager.Load<SaveContainer>();

            var content = CreateSaveLookup(data, savableComponents);
            Debug.Log("o/");
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

                UsagePath path = new UsagePath(usagePath, fieldInfo.Name);
                ProcessComponent(objectLookupSave, reflectedField, path);
            }

            var propertyInfos = ReflectionUtility.GetPropertyInfos<SavableAttribute>(reflectedObject.GetType());
            foreach (var propertyInfo in propertyInfos)
            {
                var reflectedProperty = propertyInfo.GetValue(reflectedObject);
                memberList.Add((propertyInfo, reflectedProperty));

                UsagePath path = new UsagePath(usagePath, propertyInfo.Name);
                ProcessComponent(objectLookupSave, reflectedProperty, path);
            }
        }
        
        /// <summary>
        /// Builds a dictionary that maps objects to their corresponding GuidPath, based on the provided list of savable components.
        /// </summary>
        /// <param name="savableComponents">The list of savable components used to construct the lookup dictionary</param>
        /// <returns>Returns a dictionary where the keys are objects and the values are GuidPath instances representing their paths</returns>
        private Dictionary<object, UsagePath> BuildObjectToGuidPathLookup(List<Savable> savableComponents)
        {
            Dictionary<object, UsagePath> referenceLookup = new Dictionary<object, UsagePath>();
            
            foreach (var savable in savableComponents)
            {
                UsagePath savablePath = new UsagePath(null, savable.SceneGuid);
                foreach (var component in savable.ReferenceList)
                {
                    UsagePath componentPath = new UsagePath(savablePath, component.guid);
                    referenceLookup.Add(component.component, componentPath);
                }
            }

            return referenceLookup;
        }
        
        /// <summary>
        /// Builds a dictionary that maps GuidPath instances to their corresponding objects, based on the provided list of savable components.
        /// </summary>
        /// <param name="savableComponents">The list of savable components used to construct the lookup dictionary</param>
        /// <returns>Returns a dictionary where the keys are GuidPath instances and the values are objects representing the components</returns>
        private Dictionary<UsagePath, object> BuildGuidPathToObjectLookup(List<Savable> savableComponents)
        {
            Dictionary<UsagePath, object> referenceLookup = new Dictionary<UsagePath, object>();
            
            foreach (var savable in savableComponents)
            {
                UsagePath savablePath = new UsagePath(null, savable.SceneGuid);
                foreach (var component in savable.ReferenceList)
                {
                    UsagePath componentPath = new UsagePath(savablePath, component.guid);
                    referenceLookup.Add(componentPath, component.component);
                }
            }

            return referenceLookup;
        }
        
        
        private SaveContainer CreateSerializeSaveData(SavableLookupSave lookupSave, Dictionary<object, UsagePath> referenceLookup)
        {
            SaveContainer saveContainer = new SaveContainer();
            
            foreach (var (_, sceneObjectData) in lookupSave.SceneObjectDataList)
            {
                UsagePath creatorGuidPath = sceneObjectData.CreatorPath;
                Type saveType = sceneObjectData.SavableObject.GetType();
                List<(string fieldName, object obj)> saveElements = new();
                saveContainer.SaveBuffers.Add(new SaveBuffer(saveType, creatorGuidPath, saveElements));
                
                foreach (var (memberInfo, memberObject) in sceneObjectData.MemberList)
                {
                    if (memberObject == null)
                    {
                        saveElements.Add((memberInfo.Name, null));
                    }
                    else if (memberObject.GetType().IsClass)
                    {
                        if (memberObject is UnityEngine.Object)
                        {
                            if (memberObject is UnityEngine.Component)
                            {
                                if (referenceLookup.TryGetValue(memberObject, out UsagePath referencedUsagePath))
                                {
                                    saveElements.Add((memberInfo.Name, referencedUsagePath));
                                }
                                else
                                {
                                    Debug.LogWarning(
                                        $"You need to add a savable component to the origin GameObject of the '{memberObject.GetType()}' component. Then you need to apply an " +
                                        $"ID by adding it into the ReferenceList. This will enable support for component referencing!");
                                }
                            }
                            else
                            {
                                throw new NotImplementedException("Saving Unity Assets is not supported yet!");
                            }
                        }
                        else if (lookupSave.SceneObjectDataList.TryGetValue(memberObject, out SceneObjectData referencedSceneObjectData))
                        {
                            //only for references with [Savable] attribute
                            saveElements.Add((memberInfo.Name, referencedSceneObjectData.CreatorPath));
                        }
                        else
                        {
                            NonAttributeSerialization(saveElements, memberInfo, memberObject);
                        }
                    }
                    else
                    {
                        NonAttributeSerialization(saveElements, memberInfo, memberObject);
                    }
                }
            }
            
            return saveContainer;
        }

        private void NonAttributeSerialization(List<(string fieldName, object obj)> saveElements, MemberInfo memberInfo, object memberObject)
        {
            if (TryCustomSerialization())
            {
                
            }
            else if (SerializationHelper.IsSerializable(memberObject.GetType()))    //maybe add bool for enabling c# serialization
            {
                saveElements.Add((memberInfo.Name, memberObject));
            }
            else
            {
                Debug.LogError($"The object of type {memberObject.GetType()} is not supported!");
            }
        }

        private bool TryCustomSerialization()
        {
            return true;
        }

        private SavableLookupLoad CreateSaveLookup(SaveContainer saveContainer, List<Savable> savables)
        {
            SavableLookupLoad sceneUsagePath = new SavableLookupLoad(null);
            
            foreach (var savableContainerSaveBuffer in saveContainer.SaveBuffers)
            {
                var guidPath = savableContainerSaveBuffer.CreatorGuidPath.GetPath();
                
                var savablePath = guidPath.Pop();
                var savable = savables.Find(x => x.SceneGuid == savablePath);
                if (!sceneUsagePath.TryGetComponent(savablePath, out SavableLookupLoad savableUsagePath))
                {
                    savableUsagePath = new SavableLookupLoad(savable);
                    sceneUsagePath.TryAddComponent(savablePath, savableUsagePath);
                }
                
                var componentPath = guidPath.Pop();
                var component = savable.SavableList.Find(x => x.guid == componentPath).component;
                if (!savableUsagePath.TryGetComponent(componentPath, out SavableLookupLoad componentUsagePath))
                {
                    componentUsagePath = new SavableLookupLoad(component);
                    savableUsagePath.TryAddComponent(componentPath, componentUsagePath);
                }

                SavableLookupLoad currentComponent = componentUsagePath;
                while (guidPath.Count != 0)
                {
                    var nextGuid = guidPath.Pop();
                    
                    if (!currentComponent.TryGetComponent(nextGuid, out SavableLookupLoad baseUsagePath))
                    {
                        var fieldInfo = ReflectionUtility.GetFieldInfo(currentComponent.MemberOwner.GetType(), nextGuid);
                        var propertyInfo = ReflectionUtility.GetPropertyInfo(currentComponent.MemberOwner.GetType(), nextGuid);

                        if (fieldInfo != null)
                        {
                            baseUsagePath = new SavableLookupLoad(fieldInfo.GetValue(currentComponent.MemberOwner), fieldInfo);
                        }
                        else if (propertyInfo != null)
                        {
                            baseUsagePath = new SavableLookupLoad(propertyInfo.GetValue(currentComponent.MemberOwner), propertyInfo);
                        }
                        
                        currentComponent.TryAddComponent(nextGuid, baseUsagePath);
                    }

                    currentComponent = baseUsagePath;
                }
            }

            return sceneUsagePath;
        }
        
        private void ApplySaveData(SaveContainer saveContainer, Dictionary<GuidPath, UsagePath> referenceLookup)
        {
            foreach (var saveContainerSaveBuffer in saveContainer.SaveBuffers)
            {
            }
        }
    }

    [Serializable]
    public class SaveContainer
    {
        public readonly List<SaveBuffer> SaveBuffers = new();
    }

    [Serializable]
    public class SaveBuffer
    {
        public Type SavableType;
        public UsagePath CreatorGuidPath;
        public List<(string fieldName, object obj)> SaveElements;

        public SaveBuffer(Type savableType, UsagePath creatorGuidPath,
            List<(string fieldName, object obj)> saveElements)
        {
            SavableType = savableType;
            CreatorGuidPath = creatorGuidPath;
            SaveElements = saveElements;
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
    }
    
    public class SavableLookupLoad
    {
        public object MemberOwner;
        public MemberInfo Member;
        
        private readonly Dictionary<string, SavableLookupLoad> _memberList = new();

        public SavableLookupLoad(object memberOwner)
        {
            MemberOwner = memberOwner;
        }
        
        public SavableLookupLoad(object memberOwner, MemberInfo member)
        {
            MemberOwner = memberOwner;
            Member = member;
        }

        public bool TryFindMemberInfo(Stack<string> pathStack, out MemberInfo member, out object memberOwner)
        {
            member = default;
            memberOwner = default;
            
            var path = pathStack.Pop();
            if (pathStack.Count == 0)
            {
                member = Member;
                memberOwner = MemberOwner;
                return true;
            }
            
            if (_memberList.TryGetValue(path, out SavableLookupLoad component))
            {
                return component.TryFindMemberInfo(pathStack, out member, out memberOwner);
            }
            
            return false;
        }

        public bool TryAddComponent(string identifier, SavableLookupLoad member)
        {
            return _memberList.TryAdd(identifier, member);
        }
        
        public bool TryGetComponent(string identifier, out SavableLookupLoad savableLookupLoad)
        {
            return _memberList.TryGetValue(identifier, out savableLookupLoad);
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
