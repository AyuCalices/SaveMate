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

            var content = BuildSceneComposite(data, savableComponents);
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


        private SaveContainer CreateSerializeSaveData(SavableLookupSave lookupSave,
            Dictionary<object, UsagePath> referenceLookup)
        {
            SaveContainer saveContainer = new SaveContainer();

            foreach (var (_, sceneObjectData) in lookupSave.SceneObjectDataList)
            {
                var creatorGuidPath = sceneObjectData.CreatorPath;
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
                        else if (lookupSave.SceneObjectDataList.TryGetValue(memberObject,
                                     out SceneObjectData referencedSceneObjectData))
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

        private void NonAttributeSerialization(List<(string fieldName, object obj)> saveElements, MemberInfo memberInfo,
            object memberObject)
        {
            if (TryCustomSerialization())
            {

            }
            else if
                (SerializationHelper.IsSerializable(memberObject
                    .GetType())) //maybe add bool for enabling c# serialization
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
            return false;
        }

        private SceneComposite BuildSceneComposite(SaveContainer saveContainer, List<Savable> savables)
        {
            List<(MemberComposite, string, UsagePath)> referenceResolveList = new();
            
            SceneComposite sceneComposite = new SceneComposite(savables);
            sceneComposite.BuildMemberList();

            foreach (var saveBuffer in saveContainer.SaveBuffers)
            {
                var guidPath = saveBuffer.CreatorGuidPath.GetPath();

                var savablePath = guidPath.Pop();
                if (!sceneComposite.TryGetComposite(savablePath, out SavableComposite savableComposite))
                {
                    Debug.LogError("Scene Composite doesn't contain a the path to the savable");
                }
                savableComposite.BuildMemberList();

                var componentPath = guidPath.Pop();
                if (!savableComposite.TryGetComposite(componentPath, out MemberComposite memberComposite))
                {
                    Debug.LogError("Savable Composite doesn't contain a the path to the component");
                }
                memberComposite.BuildMemberList();

                //walk existing member path
                MemberComposite currentComposite = memberComposite;
                while (guidPath.Count != 0)
                {
                    var memberPath = guidPath.Pop();
                    if (!currentComposite.TryGetComposite(memberPath, out MemberComposite newComposite))
                    {
                        Debug.LogError("Savable Composite doesn't contain a the path to the component");
                    }
                    newComposite.BuildMemberList();
                    currentComposite = newComposite;
                }
                
                //create member at new object
                foreach (var (memberName, obj) in saveBuffer.SaveElements)
                {
                    if (obj is UsagePath usagePath)
                    {
                        referenceResolveList.Add((currentComposite, memberName, usagePath));
                        currentComposite.WriteMember(memberName, null);
                    }
                    else
                    {
                        currentComposite.WriteMember(memberName, obj);
                    }
                }
            }
            
            foreach (var (baseComposite, memberName, usagePath) in referenceResolveList)
            {
                var foundElement = baseComposite.MemberList.Find(x => x.Name == memberName);
                if (foundElement == null)
                {
                    continue;
                } 
                
                switch (foundElement)
                {
                    case FieldInfo fieldInfo:
                        var fieldValue = fieldInfo.GetValue(baseComposite.GetCompositeObject());
                        if (fieldValue == null)
                        {
                            var composite = sceneComposite.FindTargetComposite(usagePath.GetPath());
                            fieldInfo.SetValue(baseComposite.GetCompositeObject(), composite.GetCompositeObject());
                        }
                        break;
                    case PropertyInfo propertyInfo:
                        var propertyValue = propertyInfo.GetValue(baseComposite.GetCompositeObject());
                        if (propertyValue == null)
                        {
                            var composite = sceneComposite.FindTargetComposite(usagePath.GetPath());
                            propertyInfo.SetValue(baseComposite.GetCompositeObject(), composite.GetCompositeObject());
                        }
                        break;
                }
            }

            return sceneComposite;
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
    
    public abstract class BaseComposite
    {
        public readonly Dictionary<string, BaseComposite> Composite = new();
        private bool _isBuildComplete;

        public abstract object GetCompositeObject();
        
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

        public void BuildMemberList()
        {
            if (_isBuildComplete) return;
            
            InternalBuildMemberList();
            _isBuildComplete = true;
        }

        protected abstract void InternalBuildMemberList();
    }

    public class SceneComposite : BaseComposite
    {
        public List<Savable> SceneSavable;
        
        public SceneComposite(List<Savable> sceneSavable)
        {
            SceneSavable = sceneSavable;
        }

        public override object GetCompositeObject()
        {
            return SceneSavable;
        }

        protected override void InternalBuildMemberList()
        {
            foreach (var savable in SceneSavable)
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
        }

        public override object GetCompositeObject()
        {
            return Savable;
        }

        protected override void InternalBuildMemberList()
        {
            foreach (var componentsContainer in Savable.SavableList)
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
        }

        public override object GetCompositeObject()
        {
            return SavableObject;
        }

        protected override void InternalBuildMemberList()
        {
            var fieldList = ReflectionUtility.GetFieldInfos<SavableAttribute>(SavableObject.GetType());
            foreach (var fieldInfo in fieldList)
            {
                MemberList.Add(fieldInfo);
                var fieldValue = fieldInfo.GetValue(SavableObject);
                if (fieldValue != null)
                {
                    var fieldUsagePath = new UsagePath(CreatorPath, fieldInfo.Name);
                    Composite[fieldInfo.Name] = new MemberComposite(fieldUsagePath, fieldValue);
                }
            }
            
            var propertyList = ReflectionUtility.GetPropertyInfos<SavableAttribute>(SavableObject.GetType());
            foreach (var propertyInfo in propertyList)
            {
                MemberList.Add(propertyInfo);
                var propertyValue = propertyInfo.GetValue(SavableObject);
                if (propertyValue != null)
                {
                    var fieldUsagePath = new UsagePath(CreatorPath, propertyInfo.Name);
                    Composite[propertyInfo.Name] = new MemberComposite(fieldUsagePath, propertyValue);
                }
            }
        }
        
        public bool TryGetComposite(string memberName, out MemberComposite memberComposite)
        {
            bool result = Composite.TryGetValue(memberName, out BaseComposite composite);
            memberComposite = composite as MemberComposite;
            return result;
        }

        public void WriteMember(string memberName, object obj)
        {
            var memberInfo = MemberList.Find(x => x.Name == memberName);
            if (memberInfo == null)
            {
                Debug.LogWarning($"Couldn't find a fitting member for {memberName} of type {obj}!");
            }
            
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    fieldInfo.SetValue(SavableObject, obj);
                    if (obj != null)
                    {
                        var fieldUsagePath = new UsagePath(CreatorPath, fieldInfo.Name);
                        Composite[fieldInfo.Name] = new MemberComposite(fieldUsagePath, obj);
                    }
                    break;
                case PropertyInfo propertyInfo:
                    propertyInfo.SetValue(SavableObject, obj);
                    if (obj != null)
                    {
                        var propertyUsagePath = new UsagePath(CreatorPath, propertyInfo.Name);
                        Composite[propertyInfo.Name] = new MemberComposite(propertyUsagePath, obj);
                    }
                    break;
            }
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
