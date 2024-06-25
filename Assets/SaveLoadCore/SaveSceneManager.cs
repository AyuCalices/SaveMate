using System;
using System.Collections.Generic;
using System.Reflection;
using SaveLoadCore.Utility;
using UnityEngine;

namespace SaveLoadCore
{
    public class SaveSceneManager : MonoBehaviour
    {
        //one list with the connection of scene guid and savableObject guid
        //-> TODO: each savable attribute thing needs a guid -> would need to be recursive for reference types. Or does it? When gathering a savable, it will be added to a list. every time an equal object gets added, a wrapper for this object will register the source id path: sceneId/ComponentId/FieldName/FieldName/FieldName ... must probably be recursive. 
        //one list with all the unique savableObjects -> the unique objects still need to be serializable
        //-> type based conversion
        
        //TODO: implement integrity check
        [ContextMenu("Gather")]
        public void GatherSavableComponents()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var sceneLookup = BuildSceneLookup(savableComponents);
            var objectToGuidPathLookup = BuildObjectToGuidPathLookup(savableComponents);
            
            var savableData = CreateSerializeSaveData(sceneLookup, objectToGuidPathLookup);
            SaveLoadManager.Save(savableData);
        }

        //TODO: reapply Data -> write tests
        [ContextMenu("Apply")]
        public void ApplySavableComponents()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var sceneBuffer = BuildSceneLookup(savableComponents);
            var guidPathToObjectLookup = BuildGuidPathToObjectLookup(savableComponents);
            
            var data = SaveLoadManager.Load<DataContainer>();
            ApplyDeserializedSaveData(data, sceneBuffer, guidPathToObjectLookup, () => Debug.LogWarning("Save-Data has extra data!"));
            ApplyDeserializedNullData(data, sceneBuffer, () => Debug.LogWarning("Null-Data has extra data!"));
        }

        private Dictionary<object, GuidPath> BuildObjectToGuidPathLookup(List<Savable> savableComponents)
        {
            Dictionary<object, GuidPath> referenceLookup = new Dictionary<object, GuidPath>();
            
            foreach (var savableComponent in savableComponents)
            {
                //TODO: -> create own reference builder that has a HashSet<referenceObject, guid> reference! there are probably two different needed: one for serializaing and one for deserializaing
                foreach (var componentsContainer in savableComponent.ReferenceList)
                {
                    GuidPath guidPath = new GuidPath()
                    {
                        savableGuid = savableComponent.SceneGuid,
                        componentGuid = componentsContainer.guid
                    };
                    
                    referenceLookup.Add(componentsContainer.component, guidPath);
                }
            }

            return referenceLookup;
        }
        
        private Dictionary<GuidPath, object> BuildGuidPathToObjectLookup(List<Savable> savableComponents)
        {
            Dictionary<GuidPath, object> referenceLookup = new Dictionary<GuidPath, object>();
            
            foreach (var savableComponent in savableComponents)
            {
                //TODO: -> create own reference builder that has a HashSet<referenceObject, guid> reference! there are probably two different needed: one for serializaing and one for deserializaing
                foreach (var componentsContainer in savableComponent.ReferenceList)
                {
                    GuidPath guidPath = new GuidPath()
                    {
                        savableGuid = savableComponent.SceneGuid,
                        componentGuid = componentsContainer.guid
                    };
                    
                    referenceLookup.Add(guidPath, componentsContainer.component);
                }
            }

            return referenceLookup;
        }

        private SceneLookup BuildSceneLookup(List<Savable> savableComponents)
        {
            var sceneLookup = new SceneLookup();
            foreach (var savableComponent in savableComponents)
            {
                //gather all components on a savable
                var savableLookup = new SavableLookup();
                
                foreach (var componentsContainer in savableComponent.SavableList)
                {
                    //gather all fields on a component
                    var objectLookup = new ObjectLookup(componentsContainer.component);
                    
                    foreach (var fieldInfo in ReflectionUtility.GetFieldInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        objectLookup.StoreElement(fieldInfo.Name, fieldInfo);
                    }

                    foreach (var propertyInfo in ReflectionUtility.GetPropertyInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        objectLookup.StoreElement(propertyInfo.Name, propertyInfo);
                    }
                    
                    savableLookup.AddComponent(componentsContainer.guid, objectLookup);
                }
                
                sceneLookup.AddSavable(savableComponent.SceneGuid, savableLookup);
            }

            return sceneLookup;
        }
        
        private DataContainer CreateSerializeSaveData(SceneLookup sceneLookup, Dictionary<object, GuidPath> referenceLookup)
        {
            var dataContainer = new DataContainer();
            
            foreach (var (savableGuid, savableLookup) in sceneLookup.GetLookup())
            {
                foreach (var (componentGuid, objectLookup) in savableLookup.GetLookup())
                {
                    foreach (var (memberName, memberInfo) in objectLookup.MemberLookupList)
                    {
                        var targetGuidPath = new GuidPath()
                        {
                            savableGuid = savableGuid,
                            componentGuid = componentGuid,
                            memberName = memberName
                        };
                        
                        //save component reference
                        if (typeof(UnityEngine.Object).IsAssignableFrom(memberInfo.ReflectedType))
                        {
                            if (typeof(Component).IsAssignableFrom(memberInfo.ReflectedType))
                            {
                                var reflectedObject = memberInfo switch
                                {
                                    FieldInfo fieldInfo => fieldInfo.GetValue(objectLookup.MemberOwner),
                                    PropertyInfo propertyInfo => propertyInfo.GetValue(objectLookup.MemberOwner),
                                    _ => throw new NotImplementedException($"The type {memberInfo.ReflectedType} of member {memberInfo.Name} on path {targetGuidPath.ToString()} is not supported!")
                                };

                                Debug.Log(reflectedObject.IsUnityNull());

                                if (reflectedObject.IsUnityNull())
                                {
                                    dataContainer.AddNullPath(targetGuidPath);
                                }
                                else if (referenceLookup.TryGetValue(reflectedObject, out GuidPath path))
                                {
                                    dataContainer.AddObject(path, targetGuidPath);
                                    Debug.Log($"Member '{memberInfo.Name}' correctly stored!");
                                }
                                else
                                {
                                    Debug.LogWarning(
                                        $"You need to add a savable component to the origin GameObject of the '{memberInfo.Name}' component. Then you need to apply an " +
                                        $"ID to the type '{memberInfo.ReflectedType}' by adding it into the ReferenceList. This will enable support for component referencing!");
                                }
                            }
                            else
                            {
                                throw new NotImplementedException("Saving Unity Assets is not supported yet!");
                            }
                        }
                        else
                        {
                            switch (memberInfo)
                            {
                                case FieldInfo fieldInfo:
                                    dataContainer.AddObject(fieldInfo.GetValue(objectLookup.MemberOwner), targetGuidPath);
                                    break;
                                case PropertyInfo propertyInfo:
                                    dataContainer.AddObject(propertyInfo.GetValue(objectLookup.MemberOwner), targetGuidPath);
                                    break;
                            }
                        }
                    }
                }
            }

            return dataContainer;
        }

        /// <summary>
        /// buffer.TryGet -> savable is not found (and it is not a prefab that needs to be instantiated) -> for downwards compatibility that mean, the buffer has deprecated data of a previous version.
        /// If there is data on the current savable the buffer does not know -> it suggests there is new data that can be initialized with default values.
        /// </summary>
        /// <param name="sceneLookup"></param>
        /// <param name="referenceLookup"></param>
        /// <param name="deserializedDataContainer"></param>
        /// <param name="onBufferHasExtraData"></param>
        private void ApplyDeserializedSaveData(DataContainer deserializedDataContainer, SceneLookup sceneLookup, Dictionary<GuidPath, object> referenceLookup, Action onBufferHasExtraData = null)
        {
            foreach (var (obj, guidPathList) in deserializedDataContainer.Lookup)
            {
                foreach (var guidPath in guidPathList)
                {
                    Debug.Log(obj.GetType());
                    
                    if (!TryGetMember(sceneLookup, guidPath, out object memberOwner, out MemberInfo memberInfo))
                    {
                        onBufferHasExtraData?.Invoke();
                        continue;
                    }

                    if (obj is GuidPath referenceGuidPath)
                    {
                        //TODO: either new data, or component is missing
                        if (!referenceLookup.TryGetValue(referenceGuidPath, out object reference))
                        {
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
        
        private void ApplyDeserializedNullData(DataContainer deserializedDataContainer, SceneLookup sceneLookup, Action onBufferHasExtraData = null)
        {
            foreach (var guidPath in deserializedDataContainer.NullPathLookup)
            {
                if (!TryGetMember(sceneLookup, guidPath, out object memberOwner, out MemberInfo memberInfo))
                {
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
        
        private bool TryGetMember(SceneLookup sceneLookup, GuidPath guidPath, out object memberOwner, out MemberInfo member)
        {
            member = default;
            memberOwner = default;
            
            if (!sceneLookup.GetLookup().TryGetValue(guidPath.savableGuid, out var savableLookup))
            {
                return false;
            }

            if (!savableLookup.GetLookup().TryGetValue(guidPath.componentGuid, out var objectLookup))
            {
                return false;
            }

            memberOwner = objectLookup.MemberOwner;
            return objectLookup.MemberLookupList.TryGetValue(guidPath.memberName, out member);
        }
    }

    public class SceneLookup
    {
        private readonly Dictionary<string, SavableLookup> _savableLookup = new();

        public Dictionary<string, SavableLookup> GetLookup() => _savableLookup;

        public void AddSavable(string identifier, SavableLookup savableLookup)
        {
            _savableLookup.Add(identifier, savableLookup);
        }
    }

    public class SavableLookup
    {
        private readonly Dictionary<string, ObjectLookup> _componentLookup = new();

        public Dictionary<string, ObjectLookup> GetLookup() => _componentLookup;

        public void AddComponent(string identifier, ObjectLookup objectLookup)
        {
            _componentLookup.Add(identifier, objectLookup);
        }
    }

    public class ObjectLookup
    {
        public Dictionary<string, MemberInfo> MemberLookupList { get; } = new();
        
        public object MemberOwner { get; }

        public ObjectLookup(object memberOwner)
        {
            MemberOwner = memberOwner;
        }

        public void StoreElement(string fieldName, MemberInfo member)
        {
            MemberLookupList.Add(fieldName, member);
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
