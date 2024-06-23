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
            var sceneBuffer = BuildSceneLookup(savableComponents);
            
            var savableData = GetSerializeSaveData(sceneBuffer);
            SaveLoadManager.Save(savableData);
        }

        //TODO: reapply Data -> write tests
        [ContextMenu("Apply")]
        public void ApplySavableComponents()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var sceneBuffer = BuildSceneLookup(savableComponents);
            
            
            var data = SaveLoadManager.Load<DataContainer>();
            ApplyDeserializedSaveData(sceneBuffer, data, () => Debug.LogWarning("fuck"));
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
                    var componentLookup = new ComponentLookup(componentsContainer.component);
                    foreach (var fieldInfo in ReflectionUtility.GetFieldInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        //TODO: duplicate code with property
                        if (typeof(UnityEngine.Object).IsAssignableFrom(fieldInfo.FieldType))
                        {
                            Debug.LogWarning($"The Type {fieldInfo.FieldType} is {typeof(UnityEngine.Object)}!");
                            continue;
                        }
                        
                        if (!SerializationHelper.IsSerializable(fieldInfo.FieldType))
                        {
                            Debug.LogWarning($"Type {fieldInfo.FieldType} is nor market as serializable!");
                            continue;
                        }
                        
                        componentLookup.StoreElement(fieldInfo);
                    }

                    foreach (var propertyInfo in ReflectionUtility.GetPropertyInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        
                        if (typeof(UnityEngine.Object).IsAssignableFrom(propertyInfo.PropertyType))
                        {
                            Debug.LogWarning($"The Type {propertyInfo.PropertyType} is {typeof(UnityEngine.Object)}!");
                            continue;
                        }
                        
                        if (!SerializationHelper.IsSerializable(propertyInfo.PropertyType))
                        {
                            Debug.LogWarning($"Type {propertyInfo.PropertyType} is nor market as serializable!");
                            continue;
                        }
                        
                        componentLookup.StoreElement(propertyInfo);
                    }
                    
                    savableLookup.AddComponent(componentsContainer.guid, componentLookup);
                }
                sceneLookup.AddSavable(savableComponent.SceneGuid, savableLookup);
            }

            return sceneLookup;
        }
        
        private DataContainer GetSerializeSaveData(SceneLookup sceneLookup)
        {
            var dataContainer = new DataContainer();
            
            foreach (var (savableGuid, savableLookup) in sceneLookup.GetLookup())
            {
                foreach (var (componentGuid, componentLookup) in savableLookup.GetLookup())
                {
                    foreach (var (fieldName, fieldInfo) in componentLookup.FieldLookup)
                    {
                        var path = new GuidPath()
                        {
                            savableGuid = savableGuid,
                            componentGuid = componentGuid,
                            memberName = fieldName,
                            memberTypes = fieldInfo.MemberType
                        };
                        
                        dataContainer.AddObject(fieldInfo.GetValue(componentLookup.Component), path);
                    }
                    
                    foreach (var (propertyName, propertyInfo) in componentLookup.PropertyLookup)
                    {
                        var path = new GuidPath()
                        {
                            savableGuid = savableGuid,
                            componentGuid = componentGuid,
                            memberName = propertyName,
                            memberTypes = propertyInfo.MemberType
                        };
                        
                        dataContainer.AddObject(propertyInfo.GetValue(componentLookup.Component), path);
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
        /// <param name="deserializedDataContainer"></param>
        /// <param name="onBufferHasExtraData"></param>
        private void ApplyDeserializedSaveData(SceneLookup sceneLookup, DataContainer deserializedDataContainer, Action onBufferHasExtraData = null)
        {
            foreach (var (obj, guidPathList) in deserializedDataContainer.Lookup)
            {
                foreach (var guidPath in guidPathList)
                {
                    if (!sceneLookup.GetLookup().TryGetValue(guidPath.savableGuid, out var savableLookup))
                    {
                        onBufferHasExtraData?.Invoke();
                        continue;
                    }
                    
                    if (!savableLookup.GetLookup().TryGetValue(guidPath.componentGuid, out var componentLookup))
                    {
                        onBufferHasExtraData?.Invoke();
                        continue;
                    }
                    
                    switch (guidPath.memberTypes)
                    {
                        case MemberTypes.Field:
                            if (!componentLookup.FieldLookup.TryGetValue(guidPath.memberName, out var fieldInfo))
                            {
                                onBufferHasExtraData?.Invoke();
                                continue;
                            }
                            
                            fieldInfo.SetValue(componentLookup.Component, obj);
                            break;
                        case MemberTypes.Property:
                            if (!componentLookup.PropertyLookup.TryGetValue(guidPath.memberName, out var propertyInfo))
                            {
                                onBufferHasExtraData?.Invoke();
                                continue;
                            }
                            
                            propertyInfo.SetValue(componentLookup.Component, obj);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
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

        public bool TryGetSavable(string identifier, out SavableLookup savableLookup)
        {
            return _savableLookup.TryGetValue(identifier, out savableLookup);
        }
    }

    public class SavableLookup
    {
        private readonly Dictionary<string, ComponentLookup> _componentLookup = new();

        public Dictionary<string, ComponentLookup> GetLookup() => _componentLookup;

        public void AddComponent(string identifier, ComponentLookup componentLookup)
        {
            _componentLookup.Add(identifier, componentLookup);
        }
        
        public bool TryGetComponent(string identifier, out ComponentLookup componentLookup)
        {
            return _componentLookup.TryGetValue(identifier, out componentLookup);
        }
    }

    public class ComponentLookup
    {
        public Dictionary<string, FieldInfo> FieldLookup { get; private set; }
        public Dictionary<string, PropertyInfo> PropertyLookup { get; private set; }
        public object Component { get; }

        public ComponentLookup(object component)
        {
            Component = component;
            FieldLookup = new Dictionary<string, FieldInfo>();
            PropertyLookup = new Dictionary<string, PropertyInfo>();
        }

        public void StoreElement(FieldInfo fieldInfo)
        {
            FieldLookup.Add(fieldInfo.Name, fieldInfo);
        }
        
        public void StoreElement(PropertyInfo propertyInfo)
        {
            PropertyLookup.Add(propertyInfo.Name, propertyInfo);
        }
    }
    
    [Serializable]
    public class DataContainer
    {
        public Dictionary<object, List<GuidPath>> Lookup { get; } = new ();

        public void AddObject(object obj, GuidPath guidPath)
        {
            Debug.Log(Lookup.ContainsKey(obj));
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
        public MemberTypes memberTypes;
    }
}
