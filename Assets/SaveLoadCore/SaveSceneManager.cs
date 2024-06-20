using System;
using System.Collections.Generic;
using System.Reflection;
using SaveLoadCore.Utility;
using UnityEngine;

namespace SaveLoadCore
{
    public class SaveSceneManager : MonoBehaviour
    {
        [ContextMenu("Gather")]
        public void GatherSavableComponents()
        {
            var savableComponents = GameObjectExtensions.FindObjectsOfTypeInScene<Savable>(gameObject.scene, true);
            
            SceneBuffer sceneBuffer = new SceneBuffer();
            foreach (Savable savableComponent in savableComponents)
            {
                
                SavableBuffer savableBuffer = new SavableBuffer();
                foreach (ComponentsContainer componentsContainer in savableComponent.SavableComponentList)
                {
                    
                    ComponentBuffer fieldBuffer = new ComponentBuffer();
                    foreach (FieldInfo fieldInfo in ReflectionUtility.GetFieldInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        fieldBuffer.WriteElement(fieldInfo, componentsContainer.component);
                    }

                    foreach (PropertyInfo propertyInfo in ReflectionUtility.GetPropertyInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        fieldBuffer.WriteElement(propertyInfo, componentsContainer.component);
                    }
                    
                    savableBuffer.AddComponent(componentsContainer.identifier, fieldBuffer);
                }
                sceneBuffer.AddSavable(savableComponent.SceneGuid, savableBuffer);
            }
            
            SaveLoadManager.Save(sceneBuffer);
            var data = SaveLoadManager.Load<SceneBuffer>();
            
            //TODO: reapply Data -> write tests
            
            bool failed = false;
            foreach (Savable savableComponent in savableComponents)
            {
                if (!data.TryGetSavable(savableComponent.SceneGuid, out SavableBuffer savableBuffer))
                {
                    failed = true;
                    continue;
                }

                foreach (ComponentsContainer componentsContainer in savableComponent.SavableComponentList)
                {
                    if (!savableBuffer.TryGetComponent(componentsContainer.identifier,
                            out ComponentBuffer componentBuffer))
                    {
                        failed = true;
                        continue;
                    }
                    
                    foreach (FieldInfo fieldInfo in ReflectionUtility.GetFieldInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        if (componentBuffer.TryReadElement(fieldInfo.Name, out object objectBuffer))
                        {
                            fieldInfo.SetValue(componentsContainer.component, objectBuffer);
                        }
                        else
                        {
                            failed = true;
                        }
                    }

                    foreach (PropertyInfo propertyInfo in ReflectionUtility.GetPropertyInfos<SavableAttribute>(componentsContainer.component.GetType()))
                    {
                        if (componentBuffer.TryReadElement(propertyInfo.Name, out object objectBuffer))
                        {
                            propertyInfo.SetValue(componentsContainer.component, objectBuffer);
                        }
                        else
                        {
                            failed = true;
                        }
                    }
                }
            }
            
            Debug.Log(failed);
        }
    }

    [Serializable]
    public class SceneBuffer
    {
        private readonly Dictionary<string, SavableBuffer> _savableLookup = new();

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

        public void WriteElement(FieldInfo fieldInfo, object fieldParent)
        {
            _dataLookup.Add(fieldInfo.Name, fieldInfo.GetValue(fieldParent));
        }
        
        public void WriteElement(PropertyInfo fieldInfo, object fieldParent)
        {
            _dataLookup.Add(fieldInfo.Name, fieldInfo.GetValue(fieldParent));
        }

        public bool TryReadElement(string identifier, out object objectBuffer)
        {
            return _dataLookup.TryGetValue(identifier, out objectBuffer);
        }
    }
}
