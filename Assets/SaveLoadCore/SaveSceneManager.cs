using System;
using System.Collections.Generic;
using System.Reflection;
using SaveLoadCore.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaveLoadCore
{
    public class SaveSceneManager : MonoBehaviour
    {
        //TODO: implement integrity check -> with meta save file + with tests
        
        [ContextMenu("Save Scene Data")]
        public void SaveSceneData()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var sceneLookup = BuildSceneLookup(savableComponents);
            var objectToGuidPathLookup = BuildObjectToGuidPathLookup(savableComponents);
            
            var savableData = CreateSerializeSaveData(sceneLookup, objectToGuidPathLookup);
            SaveLoadManager.Save(savableData);
        }
        
        [ContextMenu("Load Scene Data")]
        public void LoadSceneData()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var sceneLookup = BuildSceneLookup(savableComponents);
            var guidPathToObjectLookup = BuildGuidPathToObjectLookup(savableComponents);
            
            var data = SaveLoadManager.Load<DataContainer>();
            ApplyDeserializedSaveData(data, sceneLookup, guidPathToObjectLookup, () => Debug.LogWarning("Save-Data has extra data!"));
            ApplyDeserializedNullData(data, sceneLookup, () => Debug.LogWarning("Null-Data has extra data!"));
        }
        
        /// <summary>
        /// Builds a SceneLookup object that organizes and stores metadata about savable components and their fields or properties marked with the SavableAttribute, utilizing reflection to gather this information.
        /// </summary>
        /// <param name="savableComponents">The list of savable components the SceneLookup is based on</param>
        /// <returns>Returns a SceneLookup object containing a hierarchical lookup for MemberInfo data related to the savable components</returns>
        private SceneLookup BuildSceneLookup(List<Savable> savableComponents)
        {
            var sceneLookup = new SceneLookup();
            foreach (var savable in savableComponents)
            {
                //gather all components on a savable
                var savableLookup = new SceneLookup.Savable();
                
                foreach (var componentContainer in savable.SavableList)
                {
                    //TODO: this will need recursion for nested objects
                    
                    //gather all fields on a component
                    var objectLookup = new SceneLookup.Savable.Object(componentContainer.component);
                    
                    foreach (var fieldInfo in ReflectionUtility.GetFieldInfos<SavableAttribute>(componentContainer.component.GetType()))
                    {
                        objectLookup.Members.Add(fieldInfo.Name, fieldInfo);
                    }

                    foreach (var propertyInfo in ReflectionUtility.GetPropertyInfos<SavableAttribute>(componentContainer.component.GetType()))
                    {
                        objectLookup.Members.Add(propertyInfo.Name, propertyInfo);
                    }
                    
                    savableLookup.Components.Add(componentContainer.guid, objectLookup);
                }
                
                sceneLookup.Savables.Add(savable.SceneGuid, savableLookup);
            }

            return sceneLookup;
        }

        /// <summary>
        /// Builds a dictionary that maps objects to their corresponding GuidPath, based on the provided list of savable components.
        /// </summary>
        /// <param name="savableComponents">The list of savable components used to construct the lookup dictionary</param>
        /// <returns>Returns a dictionary where the keys are objects and the values are GuidPath instances representing their paths</returns>
        private Dictionary<object, GuidPath> BuildObjectToGuidPathLookup(List<Savable> savableComponents)
        {
            Dictionary<object, GuidPath> referenceLookup = new Dictionary<object, GuidPath>();
            
            foreach (var savableComponent in savableComponents)
            {
                foreach (var componentContainer in savableComponent.ReferenceList)
                {
                    GuidPath guidPath = new GuidPath()
                    {
                        savableGuid = savableComponent.SceneGuid,
                        componentGuid = componentContainer.guid
                    };
                    
                    referenceLookup.Add(componentContainer.component, guidPath);
                }
            }

            return referenceLookup;
        }
        
        /// <summary>
        /// Builds a dictionary that maps GuidPath instances to their corresponding objects, based on the provided list of savable components.
        /// </summary>
        /// <param name="savableComponents">The list of savable components used to construct the lookup dictionary</param>
        /// <returns>Returns a dictionary where the keys are GuidPath instances and the values are objects representing the components</returns>
        private Dictionary<GuidPath, object> BuildGuidPathToObjectLookup(List<Savable> savableComponents)
        {
            Dictionary<GuidPath, object> referenceLookup = new Dictionary<GuidPath, object>();
            
            foreach (var savableComponent in savableComponents)
            {
                foreach (var componentContainer in savableComponent.ReferenceList)
                {
                    GuidPath guidPath = new GuidPath()
                    {
                        savableGuid = savableComponent.SceneGuid,
                        componentGuid = componentContainer.guid
                    };
                    
                    referenceLookup.Add(guidPath, componentContainer.component);
                }
            }

            return referenceLookup;
        }
        
        private DataContainer CreateSerializeSaveData(SceneLookup sceneLookup, Dictionary<object, GuidPath> referenceLookup)
        {
            var dataContainer = new DataContainer();
            
            foreach (var (savableGuid, savableLookup) in sceneLookup.Savables)
            {
                foreach (var (componentGuid, objectLookup) in savableLookup.Components)
                {
                    foreach (var (memberName, memberInfo) in objectLookup.Members)
                    {
                        //TODO: this will need recursion for nested objects
                        
                        //get the actual path to the object (which should be saved)
                        var targetGuidPath = new GuidPath()
                        {
                            savableGuid = savableGuid,
                            componentGuid = componentGuid,
                            memberName = memberName
                        };
                        
                        //get the actual object (which should be saved)
                        var reflectedObject = memberInfo switch
                        {
                            FieldInfo fieldInfo => fieldInfo.GetValue(objectLookup.Owner),
                            PropertyInfo propertyInfo => propertyInfo.GetValue(objectLookup.Owner),
                            _ => throw new NotImplementedException($"The type of member {memberInfo.Name} on path {targetGuidPath.ToString()} is not supported!")
                        };

                        //store the data inside a component
                        if (reflectedObject.IsUnityNull())     //support for null reference
                        {
                            dataContainer.AddNullPath(targetGuidPath);
                        }
                        else if (reflectedObject is Object)     //support for unity objects TODO: prefabs detection and instantiation
                        {
                            if (reflectedObject is Component)
                            {
                                if (referenceLookup.TryGetValue(reflectedObject, out GuidPath path))
                                {
                                    dataContainer.AddObject(path, targetGuidPath);
                                    Debug.Log($"Member '{memberInfo.Name}' correctly stored!");
                                }
                                else
                                {
                                    Debug.LogWarning(
                                        $"You need to add a savable component to the origin GameObject of the '{memberInfo.Name}' component. Then you need to apply an " +
                                        $"ID to the type '{reflectedObject.GetType()}' by adding it into the ReferenceList. This will enable support for component referencing!");
                                }
                            }
                            else
                            {
                                throw new NotImplementedException("Saving Unity Assets is not supported yet!");
                            }
                        }
                        else     //use basic c# serialization
                        {
                            if (!SerializationHelper.IsSerializable(reflectedObject.GetType()))
                            {
                                Debug.LogError($"Type of {reflectedObject.GetType()} is not marked as Serializable!");
                            }
                            
                            dataContainer.AddObject(reflectedObject, targetGuidPath);
                        }
                    }
                }
            }

            return dataContainer;
        }
        
        private void ApplyDeserializedSaveData(DataContainer deserializedDataContainer, SceneLookup sceneLookup, Dictionary<GuidPath, object> referenceLookup, Action onBufferHasExtraData = null)
        {
            foreach (var (obj, guidPathList) in deserializedDataContainer.Lookup)
            {
                foreach (var guidPath in guidPathList)
                {
                    //TODO: prefabs detection and instantiation
                    //TODO: this will need recursion for nested objects
                    
                    if (!TryGetMember(sceneLookup, guidPath, out object memberOwner, out MemberInfo memberInfo))
                    {
                        //Occurence: this is new data (either old version or prefab)
                        onBufferHasExtraData?.Invoke();
                        continue;
                    }

                    if (obj is GuidPath referenceGuidPath)
                    {
                        if (!referenceLookup.TryGetValue(referenceGuidPath, out object reference))
                        {
                            //Occurence: 1. this is new data (either old version or prefab)
                            //           2. component is missing (guid changed or was removed)
                            onBufferHasExtraData?.Invoke();
                            continue;
                        }
                        
                        switch (memberInfo)
                        {
                            case FieldInfo fieldInfo:
                                fieldInfo.SetValue(memberOwner, reference);
                                break;
                            case PropertyInfo propertyInfo:
                                propertyInfo.SetValue(memberOwner, reference);
                                break;
                        }
                    }
                    else
                    {
                        switch (memberInfo)
                        {
                            case FieldInfo fieldInfo:
                                fieldInfo.SetValue(memberOwner, obj);
                                break;
                            case PropertyInfo propertyInfo:
                                propertyInfo.SetValue(memberOwner, obj);
                                break;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies deserialized null data to the specified members within a SceneLookup based on the provided deserialized data container.
        /// </summary>
        /// <param name="deserializedDataContainer">The container holding deserialized data including null path lookups</param>
        /// <param name="sceneLookup">The SceneLookup object used to find the members to be set to null</param>
        /// <param name="onBufferHasExtraData">An optional action to be invoked if there is extra data in the buffer that does not match any members in the SceneLookup</param>
        private void ApplyDeserializedNullData(DataContainer deserializedDataContainer, SceneLookup sceneLookup, Action onBufferHasExtraData = null)
        {
            foreach (var guidPath in deserializedDataContainer.NullPathLookup)
            {
                if (!TryGetMember(sceneLookup, guidPath, out object memberOwner, out MemberInfo memberInfo))
                {
                    //Occurence: this is new data (old version ony i guess)
                    
                    onBufferHasExtraData?.Invoke();
                    continue;
                }
                
                switch (memberInfo)
                {
                    case FieldInfo fieldInfo:
                        fieldInfo.SetValue(memberOwner, null);
                        break;
                    case PropertyInfo propertyInfo:
                        propertyInfo.SetValue(memberOwner, null);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Attempts to retrieve a member and its owner object from the SceneLookup based on the provided GuidPath.
        /// </summary>
        /// <param name="sceneLookup">The SceneLookup object used to locate the member</param>
        /// <param name="guidPath">The GuidPath containing the identifiers for the savable, component, and member</param>
        /// <param name="memberOwner">The output parameter that will hold the owner object of the member if found</param>
        /// <param name="member">The output parameter that will hold the MemberInfo if found</param>
        /// <returns>Returns true if the member is successfully found, otherwise returns false</returns>
        private bool TryGetMember(SceneLookup sceneLookup, GuidPath guidPath, out object memberOwner, out MemberInfo member)
        {
            member = default;
            memberOwner = default;
            
            if (!sceneLookup.Savables.TryGetValue(guidPath.savableGuid, out var savableLookup))
            {
                return false;
            }

            if (!savableLookup.Components.TryGetValue(guidPath.componentGuid, out var objectLookup))
            {
                return false;
            }

            memberOwner = objectLookup.Owner;
            return objectLookup.Members.TryGetValue(guidPath.memberName, out member);
        }
    }

    public class SceneLookup
    {
        public Dictionary<string, Savable> Savables { get; }= new();
        
        public class Savable
        {
            public Dictionary<string, Object> Components { get; } = new();
            
            public class Object
            {
                public Dictionary<string, MemberInfo> Members { get; } = new();
                public object Owner { get; }

                public Object(object owner)
                {
                    Owner = owner;
                }
            }
        }
    }
    
    [Serializable]
    public class DataContainer
    {
        public Dictionary<object, List<GuidPath>> Lookup { get; } = new ();
        public List<GuidPath> NullPathLookup { get; } = new ();

        public void AddNullPath(GuidPath guidPath)
        {
            if (!NullPathLookup.Contains(guidPath))
            {
                NullPathLookup.Add(guidPath);
            }
        }
        
        public void AddObject(object obj, GuidPath guidPath)
        {
            if (!Lookup.TryGetValue(obj, out List<GuidPath> guidPathList))
            {
                guidPathList = new List<GuidPath>();
                Lookup.Add(obj, guidPathList);
            }
            
            guidPathList.Add(guidPath);
        }
    }

    [Serializable]
    public struct GuidPath
    {
        public string savableGuid;
        public string componentGuid;
        public string memberName;

        public override string ToString()
        {
            return $"{savableGuid} | {componentGuid} | {memberName}";
        }
    }
}
