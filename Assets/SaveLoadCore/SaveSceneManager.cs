using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaveLoadCore.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaveLoadCore
{
    public class SaveSceneManager : MonoBehaviour
    {
        //TODO: implement integrity check -> with tests
        
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
                var savableLookup = new SceneLookup.Savable();
                foreach (var componentContainer in savable.SavableList)
                {
                    var objectLookup = new SceneLookup.Savable.Object(componentContainer.component);
                    ProcessComponent(objectLookup, componentContainer.component);
                    savableLookup.Components.Add(componentContainer.guid, objectLookup);
                }
                sceneLookup.Savables.Add(savable.SceneGuid, savableLookup);
            }
            
            return sceneLookup;
        }

        private void ProcessComponent(SceneLookup.Savable.Object objectLookup, object reflectedObject)
        {
            if (reflectedObject == null) return;
            
            var fieldInfos = ReflectionUtility.GetFieldInfos<SavableAttribute>(reflectedObject.GetType());
            foreach (var fieldInfo in fieldInfos)
            {
                var reflectedField = fieldInfo.GetValue(reflectedObject);
                var nestedObjectLookup = new SceneLookup.Savable.Object(reflectedField);
                objectLookup.Members.Add(fieldInfo.Name, (fieldInfo, nestedObjectLookup));
                
                ProcessComponent(nestedObjectLookup, reflectedField);
            }

            var propertyInfos = ReflectionUtility.GetPropertyInfos<SavableAttribute>(reflectedObject.GetType());
            foreach (var propertyInfo in propertyInfos)
            {
                var reflectedProperty = propertyInfo.GetValue(reflectedObject);
                var nestedObjectLookup = new SceneLookup.Savable.Object(reflectedProperty);
                objectLookup.Members.Add(propertyInfo.Name, (propertyInfo, nestedObjectLookup));
                
                ProcessComponent(nestedObjectLookup, reflectedProperty);
            }
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
                    GuidPath guidPath = new GuidPath(savableComponent.SceneGuid, componentContainer.guid);
                    
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
                    GuidPath guidPath = new GuidPath(savableComponent.SceneGuid, componentContainer.guid);
                    
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
                    GuidPath targetGuidPath = new GuidPath(savableGuid, componentGuid);
                    FillDataContainerRecursively(dataContainer, objectLookup, referenceLookup, targetGuidPath);
                }
            }

            return dataContainer;
        }
        
        private void FillDataContainerRecursively(DataContainer dataContainer, SceneLookup.Savable.Object objectLookup, Dictionary<object, GuidPath> referenceLookup, GuidPath guidPath)
        {
            foreach (var (memberName, memberInfoTuple) in objectLookup.Members)
            {
                GuidPath targetGuidPath = new GuidPath(guidPath); 
                targetGuidPath.MemberNamePath.Add(memberName);
                
                //get the actual object (which should be saved)
                var reflectedObject = memberInfoTuple.Info switch
                {
                    FieldInfo fieldInfo => fieldInfo.GetValue(objectLookup.Owner),
                    PropertyInfo propertyInfo => propertyInfo.GetValue(objectLookup.Owner),
                    _ => throw new NotImplementedException($"The type of member {memberInfoTuple.Info.Name} on path {targetGuidPath.ToString()} is not supported!")
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
                            Debug.Log($"Member '{memberInfoTuple.Info.Name}' correctly stored!");
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"You need to add a savable component to the origin GameObject of the '{memberInfoTuple.Info.Name}' component. Then you need to apply an " +
                                $"ID to the type '{reflectedObject.GetType()}' by adding it into the ReferenceList. This will enable support for component referencing!");
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("Saving Unity Assets is not supported yet!");
                    }
                }
                else if (reflectedObject is IEnumerable enumerable)
                {
                    Debug.Log(enumerable is Array);
                        
                    foreach (object obj in enumerable)
                    {
                        if (!SerializationHelper.IsSerializable(obj.GetType()))
                        {
                            Debug.LogWarning($"You need to implement Serialization for the type {reflectedObject.GetType()}");
                        }
                    }
                }
                else
                {
                    if (!SerializationHelper.IsSerializable(reflectedObject.GetType()))
                    {
                        Debug.LogWarning($"You need to implement Serialization for the type {reflectedObject.GetType()}");
                    }
                    
                    dataContainer.AddObject(reflectedObject, targetGuidPath);
                }

                FillDataContainerRecursively(dataContainer, memberInfoTuple.SceneLookupObject, referenceLookup, targetGuidPath);
            }
        }
        
        private void ApplyDeserializedSaveData(DataContainer deserializedDataContainer, SceneLookup sceneLookup, Dictionary<GuidPath, object> referenceLookup, Action onBufferHasExtraData = null)
        {
            foreach (var (obj, guidPathList) in deserializedDataContainer.Lookup)
            {
                foreach (var guidPath in guidPathList)
                {
                    //TODO: prefabs detection and instantiation
                    //TODO: this will need recursion for nested objects
                    
                    if (!TryGetMember(sceneLookup, guidPath, out object memberOwner, out var memberInfoTuple))
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
                        
                        switch (memberInfoTuple.Info)
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
                        switch (memberInfoTuple.Info)
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
                if (!TryGetMember(sceneLookup, guidPath, out object memberOwner, out var memberInfoTuple))
                {
                    //Occurence: this is new data (old version ony i guess)
                    
                    onBufferHasExtraData?.Invoke();
                    continue;
                }
                
                switch (memberInfoTuple.Info)
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
        /// <param name="memberTuple">The output parameter that will hold the MemberInfo if found</param>
        /// <returns>Returns true if the member is successfully found, otherwise returns false</returns>
        private bool TryGetMember(SceneLookup sceneLookup, GuidPath guidPath, out object memberOwner, out (MemberInfo Info, SceneLookup.Savable.Object SceneLookupObject) memberTuple)
        {
            memberTuple = default;
            memberOwner = default;
            
            if (!sceneLookup.Savables.TryGetValue(guidPath.SavableGuid, out var savableLookup))
            {
                return false;
            }

            if (!savableLookup.Components.TryGetValue(guidPath.ComponentGuid, out var objectLookup))
            {
                return false;
            }

            memberOwner = objectLookup.Owner;

            var memberNamePathEnumerator = guidPath.MemberNamePath.GetEnumerator();
            SceneLookup.Savable.Object currentObject = objectLookup;

            while (memberNamePathEnumerator.MoveNext())
            {
                var currentMemberName = memberNamePathEnumerator.Current;
                if (currentMemberName == null)
                {
                    return false;
                }
                
                if (!currentObject.Members.TryGetValue(currentMemberName, out var currentMemberInfoTuple))
                {
                    return false;
                }

                memberTuple = currentMemberInfoTuple;
                currentObject = currentMemberInfoTuple.SceneLookupObject;
            }

            return true;
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
                public Dictionary<string, (MemberInfo Info, Object SceneLookupObject)> Members { get; } = new();
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
    public struct GuidPath : IEquatable<GuidPath>
    {
        public readonly string SavableGuid;
        public readonly string ComponentGuid;
        public readonly List<string> MemberNamePath;

        public GuidPath(string savableGuid, string componentGuid)
        {
            SavableGuid = savableGuid;
            ComponentGuid = componentGuid;
            MemberNamePath = new List<string>();
        }
        
        public GuidPath(GuidPath guidPath)
        {
            SavableGuid = guidPath.SavableGuid;
            ComponentGuid = guidPath.ComponentGuid;
            MemberNamePath = guidPath.MemberNamePath.ToList();
        }

        public override string ToString()
        {
            string finalName = $"{SavableGuid} | {ComponentGuid}";
            foreach (var memberName in MemberNamePath)
            {
                finalName += $" | {memberName}";
            }
            
            return ">>>" + finalName + "<<<";
        }

        public bool Equals(GuidPath other)
        {
            // Compare SavableGuid and ComponentGuid
            if (SavableGuid != other.SavableGuid || ComponentGuid != other.ComponentGuid)
            {
                return false;
            }

            // Compare MemberNamePath
            if (MemberNamePath.Count != other.MemberNamePath.Count)
            {
                return false;
            }
        
            for (int i = 0; i < MemberNamePath.Count; i++)
            {
                if (MemberNamePath[i] != other.MemberNamePath[i])
                {
                    return false;
                }
            }
        
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is GuidPath other)
            {
                return Equals(other);
            }
        
            return false;
        }

        public override int GetHashCode()
        {
            int hash = SavableGuid.GetHashCode();
            hash = (hash * 31) + ComponentGuid.GetHashCode();
        
            foreach (var memberName in MemberNamePath)
            {
                hash = (hash * 31) + memberName.GetHashCode();
            }
        
            return hash;
        }

        public static bool operator ==(GuidPath left, GuidPath right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GuidPath left, GuidPath right)
        {
            return !(left == right);
        }
    }
}
