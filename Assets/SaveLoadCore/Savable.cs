using System;
using System.Collections.Generic;
using SaveLoadCore.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace SaveLoadCore
{
    //TODO: there might be another case, when the buffer and the serializeField must be triggered!
    //TODO: prefab: delete element -> restart editor -> reapply element will result in changed id!
    public class Savable : MonoBehaviour, ICreateGameObjectHierarchy, IChangeComponentProperties, IChangeGameObjectProperties, IChangeGameObjectStructure, IChangeGameObjectStructureHierarchy
    {
        [SerializeField] private string hierarchyPath;
        [SerializeField] private GameObject prefabSource;
        
        [SerializeField] private string serializeFieldSceneGuid;
        private string _resetBufferSceneGuid;
        
        [FormerlySerializedAs("serializeFieldCurrentSavableList")] [SerializeField] private List<ComponentsContainer> serializeFieldCurrentSavableComponentList = new();
        private readonly List<ComponentsContainer> _resetBufferCurrentSavableList = new();
        
        [SerializeField] private List<ComponentsContainer> serializeFieldRemovedSavableList = new();

        //TODO: decide on naming: Guid or Identifier
        public string HierarchyPath => hierarchyPath;
        public GameObject PrefabSource => prefabSource;
        public string SceneGuid => serializeFieldSceneGuid;
        public List<ComponentsContainer> SavableComponentList => serializeFieldCurrentSavableComponentList;

        private void ChangingGuidWarning(string fieldName) => Debug.LogWarning($"The parameter {fieldName} on {hierarchyPath}/{gameObject.name} has changed! This may be normal, if you opened e.g. a prefab.");
        private void UnaccountedComponentError(string guid) => Debug.LogError($"There is an unaccounted guid `{guid}` registered. Maybe you removed a component and then restarted the scene/editor?");
        
        private void Reset()
        {
            ApplyResetBuffer();
            serializeFieldSceneGuid = _resetBufferSceneGuid;
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            
            ApplyScriptReloadBuffer();
            _resetBufferSceneGuid = serializeFieldSceneGuid;
            CheckForUnaccountedRemovedSavableElement();
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
            serializeFieldCurrentSavableComponentList.Clear();
            foreach (var savableContainer in _resetBufferCurrentSavableList)
            {
                serializeFieldCurrentSavableComponentList.Add(savableContainer);
            }
        }

        /// <summary>
        /// Serialize Fields will be serialized through script reloads and application restarts. The Reset Buffer values
        /// will be lost. This method will reapply the lost values for the Reset Buffer with the Serialize Fields. This
        /// prevents loosing the original guid.
        /// </summary>
        private void ApplyScriptReloadBuffer()
        {
            if (serializeFieldCurrentSavableComponentList.Count != _resetBufferCurrentSavableList.Count)
            {
                _resetBufferCurrentSavableList.Clear();
                foreach (var savableContainer in serializeFieldCurrentSavableComponentList)
                {
                    _resetBufferCurrentSavableList.Add(savableContainer);
                }
            }
        }
        
        private void CheckForUnaccountedRemovedSavableElement()
        {
            foreach (var componentsContainer in serializeFieldRemovedSavableList)
            {
                if (componentsContainer.component == null)
                {
                    UnaccountedComponentError(componentsContainer.identifier);
                }
            }
        }

        private void SetupAll(bool isCreateCall)
        {
            prefabSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            
            UpdateSavableComponents(isCreateCall);

            if (gameObject.scene.name != null)
            {
                SetupSceneGuid(isCreateCall);
                SetupScenePath();
            }
            else
            {
                ResetSceneGuid();
                ResetScenePath();
            }
            
            SetDirty(this);
        }
        
        private void SetupScenePath()
        {
            string path = gameObject.name;
            Transform current = gameObject.transform;

            // Traverse up the hierarchy
            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
                
            hierarchyPath = path + "/";
        }

        private void ResetScenePath()
        {
            hierarchyPath = "";
        }

        private void SetupSceneGuid(bool isCreatCall)
        {
            if (isCreatCall || string.IsNullOrEmpty(serializeFieldSceneGuid))
            {
                // If both 'serializeField' and 'resetBuffer' are null or empty, and this is not during initialization,
                // it indicates that a new ID has been assigned. This results in a GUID conflict with the version control system.
                if (!isCreatCall && string.IsNullOrEmpty(_resetBufferSceneGuid))
                {
                    ChangingGuidWarning(nameof(serializeFieldSceneGuid)); 
                }

                SetSceneGuidGroup(Guid.NewGuid().ToString());
            }
        }

        private void ResetSceneGuid()
        {
            SetSceneGuidGroup("");
        }
 
        private void SetSceneGuidGroup(string text)
        {
            serializeFieldSceneGuid = text;
            _resetBufferSceneGuid = text;
        }

        private void AddToCurrentSavableGroup(ComponentsContainer componentsContainer)
        {
            serializeFieldCurrentSavableComponentList.Add(componentsContainer);
            _resetBufferCurrentSavableList.Add(componentsContainer);
        }

        private void RemoveFromCurrentSavableGroup(ComponentsContainer componentsContainer)
        {
            serializeFieldCurrentSavableComponentList.Remove(componentsContainer);
            _resetBufferCurrentSavableList.Remove(componentsContainer);
        }

        private void UpdateSavableComponents(bool isCreatCall)
        {
            //if setting this dirty, the hierarchy changed event will trigger, resulting in an update behaviour
            List<Component> foundElements = ReflectionUtility.GetComponentsWithTypeCondition(gameObject, 
                ReflectionUtility.ContainsProperty<SavableAttribute>, ReflectionUtility.ContainsField<SavableAttribute>);
            
            //update removed elements and those that are kept 
            for (var index = serializeFieldCurrentSavableComponentList.Count - 1; index >= 0; index--)
            {
                var currentSavableContainer = serializeFieldCurrentSavableComponentList[index];
                
                if (!foundElements.Exists(x => x == currentSavableContainer.component))
                {
                    serializeFieldRemovedSavableList.Add(currentSavableContainer);
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
                            ChangingGuidWarning(nameof(currentSavableContainer));
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
                    serializeFieldRemovedSavableList.Remove(removedComponent);
                }
                
                AddToCurrentSavableGroup(new ComponentsContainer
                {
                    identifier = guid,
                    component = foundElement
                });
            }
        }
        
        private void SetDirty(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(obj);
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
