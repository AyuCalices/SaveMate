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
        
        private SceneBuffer GatherSavableData(List<Savable> savableComponents)
        {
            SceneBuffer sceneBuffer = new SceneBuffer();
            foreach (Savable savableComponent in savableComponents)
            {
                //gather all components on a savable
                SavableBuffer savableBuffer = new SavableBuffer();
                foreach (ComponentsContainer componentsContainer in savableComponent.SavableComponentList)
                {
                    //gather all fields on a component
                    ComponentBuffer fieldBuffer = new ComponentBuffer();
                    foreach (FieldInfo fieldInfo in ReflectionUtility.GetFieldInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        if (!SerializationHelper.IsSerializable(fieldInfo.FieldType))
                        {
                            Debug.LogWarning("Not Serializable!");
                            continue;
                        }
                        
                        fieldBuffer.StoreElement(fieldInfo, componentsContainer.component);
                    }

                    foreach (PropertyInfo propertyInfo in ReflectionUtility.GetPropertyInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        if (!SerializationHelper.IsSerializable(propertyInfo.PropertyType))
                        {
                            Debug.LogWarning("Not Serializable!");
                            continue;
                        }
                        
                        fieldBuffer.StoreElement(propertyInfo, componentsContainer.component);
                    }
                    
                    savableBuffer.AddComponent(componentsContainer.identifier, fieldBuffer);
                }
                sceneBuffer.AddSavable(savableComponent.SceneGuid, savableBuffer);
            }

            return sceneBuffer;
        }

        /// <summary>
        /// buffer.TryGet -> savable is not found (and it is not a prefab that needs to be instantiated) -> for downwards compatibility that mean, the buffer has deprecated data of a previous version.
        /// If there is data on the current savable the buffer does not know -> it suggests there is new data that can be initialized with default values.
        /// </summary>
        /// <param name="savableComponents"></param>
        /// <param name="deserializedSceneBuffer"></param>
        /// <param name="onBufferHasExtraData"></param>
        private void DistributeSavableData(List<Savable> savableComponents, SceneBuffer deserializedSceneBuffer, Action onBufferHasExtraData = null)
        {
            foreach (var (savableGuid, savableBuffer) in deserializedSceneBuffer.GetLookup())
            {
                var savableMatch = savableComponents.Find(x => x.SceneGuid == savableGuid);
                if (savableMatch == null)
                {
                    onBufferHasExtraData?.Invoke();
                    continue;
                }
                
                foreach (var (componentGuid, componentBuffer) in savableBuffer.GetLookup())
                {
                    var componentMatch = savableMatch.SavableComponentList.Find(x => x.identifier == componentGuid);
                    if (componentMatch == null)
                    {
                        onBufferHasExtraData?.Invoke();
                        continue;
                    }
                    
                    foreach (var (elementName, deserializedObj) in componentBuffer.GetLookup())
                    {
                        bool matchFound = false;
                        
                        var fieldMatch = ReflectionUtility.GetFieldInfos<SavableAttribute>(componentMatch.component.GetType()).Find(x => x.Name == elementName);
                        if (fieldMatch != null)
                        {
                            fieldMatch.SetValue(componentMatch.component, deserializedObj);
                            matchFound = true;
                        }

                        var propertyMatch = ReflectionUtility.GetPropertyInfos<SavableAttribute>(componentMatch.component.GetType()).Find(x => x.Name == elementName);
                        if (propertyMatch != null)
                        {
                            propertyMatch.SetValue(componentMatch.component, deserializedObj);
                            matchFound = true;
                        }

                        if (!matchFound)
                        {
                            onBufferHasExtraData?.Invoke();
                        }
                    }
                }
            }
        }
        
        //TODO: implement integrity check
        [ContextMenu("Gather")]
        public void GatherSavableComponents()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var sceneBuffer = GatherSavableData(savableComponents);
            SaveLoadManager.Save(sceneBuffer);
        }

        //TODO: reapply Data -> write tests
        [ContextMenu("Apply")]
        public void ApplySavableComponents()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            var data = SaveLoadManager.Load<SceneBuffer>();
            DistributeSavableData(savableComponents, data, () => Debug.LogWarning("Save file contains data, that could not be assigned to savables!"));
        }
    }

    [Serializable]
    public class SceneBuffer
    {
        private readonly Dictionary<string, SavableBuffer> _savableLookup = new();

        public Dictionary<string, SavableBuffer> GetLookup() => _savableLookup;

        public void AddSavable(string identifier, SavableBuffer savableBuffer)
        {
            _savableLookup.Add(identifier, savableBuffer);
        }

        public bool TryGetSavable(string identifier, out SavableBuffer savableBuffer)
        {
            return _savableLookup.TryGetValue(identifier, out savableBuffer);
        }
    }

    [Serializable]
    public class SavableBuffer
    {
        private readonly Dictionary<string, ComponentBuffer> _componentLookup = new();

        public Dictionary<string, ComponentBuffer> GetLookup() => _componentLookup;

        public void AddComponent(string identifier, ComponentBuffer componentBuffer)
        {
            _componentLookup.Add(identifier, componentBuffer);
        }
        
        public bool TryGetComponent(string identifier, out ComponentBuffer componentBuffer)
        {
            return _componentLookup.TryGetValue(identifier, out componentBuffer);
        }
    }

    [Serializable]
    public class ComponentBuffer
    {
        private readonly Dictionary<string, object> _dataLookup = new();

        public Dictionary<string, object> GetLookup() => _dataLookup;
        
        public void StoreElement(FieldInfo fieldInfo, object fieldParent)
        {
            _dataLookup.Add(fieldInfo.Name, fieldInfo.GetValue(fieldParent));
        }
        
        public void StoreElement(PropertyInfo fieldInfo, object fieldParent)
        {
            _dataLookup.Add(fieldInfo.Name, fieldInfo.GetValue(fieldParent));
        }

        public bool TryReadElement(string identifier, out object objectBuffer)
        {
            return _dataLookup.TryGetValue(identifier, out objectBuffer);
        }
    }
}
