using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SaveLoadCore
{
    //TODO: there might be another case, when the buffer and the serializeField must be triggered!
    public class Savable : MonoBehaviour, ICreateGameObjectHierarchy, IChangeComponentProperties, IChangeGameObjectProperties, IChangeGameObjectStructure, IChangeGameObjectStructureHierarchy
    {
        [SerializeField] private string hierarchyPath;
        [SerializeField] private GameObject prefabSource;
        
        [SerializeField] private string serializeFieldSceneGuid;
        private string _resetBufferSceneGuid;
        
        [SerializeField] private List<ComponentsContainer> serializeFieldCurrentSavableList = new();
        private readonly List<ComponentsContainer> _resetBufferCurrentSavableList = new();
        
        [SerializeField] private List<ComponentsContainer> serializeFieldRemovedSavableList = new();
        private readonly List<ComponentsContainer> _resetBufferRemovedSavableList = new();

        private string ChangingGuidError(string fieldName) => $"The parameter {fieldName} on {hierarchyPath}/{gameObject.name} has changed!";
        
        private void Reset()
        {
            ApplyResetBuffer();
            serializeFieldSceneGuid = _resetBufferSceneGuid;
        }

        private void OnValidate()
        {
            ApplyScriptReloadBuffer();
            
            if (Application.isPlaying) return;
            
            SetupAll(false);
        }
        
        public void OnCreateGameObjectHierarchy()
        {
            SetupAll(true);
        }
        
        public void OnChangeGameObjectStructure()
        {
            SetupAll(false);
        }
        
        public void OnChangeComponentProperties()
        {
            SetupAll(false);
        }

        public void OnChangeGameObjectProperties()
        {
            SetupAll(false);
        }
        
        public void OnChangeGameObjectStructureHierarchy()
        {
            SetupAll(false);
        }
        
        /// <summary>
        /// If a Component get's resetted, all Serialize Field values are lost. This method will reapply the lost values
        /// for the Serialize Fields with the Reset Buffer. This prevents loosing the original guid.
        /// </summary>
        private void ApplyResetBuffer()
        {
            serializeFieldCurrentSavableList.Clear();
            foreach (var savableContainer in _resetBufferCurrentSavableList)
            {
                serializeFieldCurrentSavableList.Add(savableContainer);
            }
            
            
            serializeFieldRemovedSavableList.Clear();
            foreach (var savableContainer in _resetBufferRemovedSavableList)
            {
                serializeFieldRemovedSavableList.Add(savableContainer);
            }
        }

        /// <summary>
        /// Serialize Fields will be serialized through script reloads and application restarts. The Reset Buffer values
        /// will be lost. This method will reapply the lost values for the Reset Buffer with the Serialize Fields. This
        /// prevents loosing the original guid.
        /// </summary>
        private void ApplyScriptReloadBuffer()
        {
            if (serializeFieldCurrentSavableList.Count != _resetBufferCurrentSavableList.Count)
            {
                _resetBufferCurrentSavableList.Clear();
                foreach (var savableContainer in serializeFieldCurrentSavableList)
                {
                    _resetBufferCurrentSavableList.Add(savableContainer);
                }
            }
            
            if (serializeFieldRemovedSavableList.Count != _resetBufferRemovedSavableList.Count)
            {
                _resetBufferRemovedSavableList.Clear();
                foreach (var savableContainer in serializeFieldRemovedSavableList)
                {
                    _resetBufferRemovedSavableList.Add(savableContainer);
                }
            }
        }

        private void SetupAll(bool isCreateCall)
        {
            SetupSceneGuid(isCreateCall);
            prefabSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            
            UpdateScenePath();
            UpdateSavableComponents(isCreateCall);
        }
        
        private void UpdateScenePath()
        {
            string GetHierarchyPath(GameObject obj)
            {
                string path = obj.name;
                Transform current = obj.transform;

                // Traverse up the hierarchy
                while (current.parent != null)
                {
                    current = current.parent;
                    path = current.name + "/" + path;
                }

                return path + "/";
            }
            
            if (gameObject.scene.name != null)
            {
                hierarchyPath = GetHierarchyPath(gameObject);
            }
            else
            {
                hierarchyPath = "";
            }
        }

        private void SetupSceneGuid(bool isCreatCall)
        {
            if (gameObject.scene.name != null)
            {
                if (string.IsNullOrEmpty(serializeFieldSceneGuid))
                {
                    // If both 'serializeField' and 'resetBuffer' are null or empty, and this is not during initialization,
                    // it indicates that a new ID has been assigned. This results in a GUID conflict with the version control system.
                    if (!isCreatCall && string.IsNullOrEmpty(_resetBufferSceneGuid))
                    {
                        Debug.LogError(ChangingGuidError(nameof(serializeFieldSceneGuid))); 
                    }

                    SetSceneGuidGroup(Guid.NewGuid().ToString());
                    SetDirty(this);
                }
            }
            else
            {
                serializeFieldSceneGuid = "";
            }
        }
 
        private void SetSceneGuidGroup(string text)
        {
            serializeFieldSceneGuid = text;
            _resetBufferSceneGuid = text;
        }

        private void AddToCurrentSavableGroup(ComponentsContainer componentsContainer)
        {
            serializeFieldCurrentSavableList.Add(componentsContainer);
            _resetBufferCurrentSavableList.Add(componentsContainer);
        }

        private void RemoveFromCurrentSavableGroup(ComponentsContainer componentsContainer)
        {
            serializeFieldCurrentSavableList.Remove(componentsContainer);
            _resetBufferCurrentSavableList.Remove(componentsContainer);
        }

        private void AddToRemovedSavableGroup(ComponentsContainer componentsContainer)
        {
            serializeFieldRemovedSavableList.Add(componentsContainer);
            _resetBufferRemovedSavableList.Add(componentsContainer);
        }

        private void RemoveFromRemovedSavableGroup(ComponentsContainer componentsContainer)
        {
            serializeFieldRemovedSavableList.Remove(componentsContainer);
            _resetBufferRemovedSavableList.Remove(componentsContainer);
        }
        
        private void UpdateSavableComponents(bool isCreatCall)
        {
            //if setting this dirty, the hierarchy changed event will trigger, resulting in an update behaviour
            List<Component> foundElements = ReflectionUtility.GetComponentsWithTypeCondition(gameObject, 
                ReflectionUtility.ContainsProperty<SavableAttribute>, ReflectionUtility.ContainsField<SavableAttribute>);
            
            //update removed elements and those that are kept 
            for (var index = serializeFieldCurrentSavableList.Count - 1; index >= 0; index--)
            {
                var currentSavableContainer = serializeFieldCurrentSavableList[index];
                
                if (!foundElements.Exists(x => x == currentSavableContainer.component))
                {
                    AddToRemovedSavableGroup(currentSavableContainer);
                    RemoveFromCurrentSavableGroup(currentSavableContainer);
                }
                else
                {
                    if (string.IsNullOrEmpty(currentSavableContainer.identifier))
                    {
                        // If both 'serializeField' and 'resetBuffer' are null or empty, and this is not during initialization,
                        // it indicates that a new ID has been assigned. This results in a GUID conflict with the version control system.
                        if (!isCreatCall && string.IsNullOrEmpty(_resetBufferCurrentSavableList[index].identifier))
                        {
                            Debug.LogError(ChangingGuidError(nameof(currentSavableContainer))); 
                        }
                        
                        currentSavableContainer.identifier = Guid.NewGuid().ToString();
                    }
                    
                    foundElements.Remove(currentSavableContainer.component);
                }
            }

            //add new elements
            foreach (Component foundElement in foundElements) 
            {
                string guid = Guid.NewGuid().ToString();
                
                //make sure a deleted element will have the same id after redo again
                var removedComponent = serializeFieldRemovedSavableList.Find(x => x.component == foundElement);
                if (removedComponent != null)
                {
                    guid = removedComponent.identifier;
                    RemoveFromRemovedSavableGroup(removedComponent);
                }
                
                AddToCurrentSavableGroup(new ComponentsContainer
                {
                    identifier = guid,
                    component = foundElement
                });
            }
        }
        
        //TODO: check set dirty
        private void SetDirty(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }
    }

    [Serializable]
    public class ComponentsContainer
    {
        public string identifier;
        public Component component;
    }
}
