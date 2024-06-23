using System;
using System.Collections.Generic;
using SaveLoadCore.Utility;
using UnityEditor;
using UnityEngine;

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
        
        [SerializeField] private List<ComponentsContainer> serializeFieldCurrentSavableList = new();
        private readonly List<ComponentsContainer> _resetBufferCurrentSavableList = new();
        
        [SerializeField] private List<ComponentsContainer> serializeFieldRemovedSavableList = new();

        [SerializeField] private List<ComponentsContainer> serializeFieldSavableReferenceList = new();
        private readonly List<ComponentsContainer> _resetBufferSavableReferenceList = new();

        public string HierarchyPath => hierarchyPath;
        public GameObject PrefabSource => prefabSource;
        public string SceneGuid => serializeFieldSceneGuid;
        public List<ComponentsContainer> SavableList => serializeFieldCurrentSavableList;

        private void ChangingGuidWarning(string fieldName) => Debug.LogWarning($"The parameter '{fieldName}' at the path '{hierarchyPath}' has changed when it wasn't created! This may be normal, if you opened e.g. a prefab.");
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
            serializeFieldCurrentSavableList.Clear();
            foreach (var currentSavableContainer in _resetBufferCurrentSavableList)
            {
                serializeFieldCurrentSavableList.Add(currentSavableContainer);
            }
            
            serializeFieldSavableReferenceList.Clear();
            foreach (var savableReferenceContainer in _resetBufferSavableReferenceList)
            {
                serializeFieldSavableReferenceList.Add(savableReferenceContainer);
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
                foreach (var currentSavableContainer in serializeFieldCurrentSavableList)
                {
                    _resetBufferCurrentSavableList.Add(currentSavableContainer);
                }
            }

            if (serializeFieldSavableReferenceList.Count != _resetBufferSavableReferenceList.Count)
            {
                _resetBufferSavableReferenceList.Clear();
                foreach (var componentsReferenceContainer in serializeFieldSavableReferenceList)
                {
                    _resetBufferSavableReferenceList.Add(componentsReferenceContainer);
                }
            }
        }
        
        private void CheckForUnaccountedRemovedSavableElement()
        {
            foreach (var componentsContainer in serializeFieldRemovedSavableList)
            {
                if (componentsContainer.component == null)
                {
                    UnaccountedComponentError(componentsContainer.guid);
                }
            }
        }

        private void SetupAll(bool isCreateCall)
        {
            prefabSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            UpdateSavableComponents();
            UpdateSavableReferenceComponents();

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
            var path = gameObject.name;
            var current = gameObject.transform;

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
            if (isCreatCall)
            {
                SetSceneGuidGroup(Guid.NewGuid().ToString());
            }
            else if (string.IsNullOrEmpty(serializeFieldSceneGuid) && string.IsNullOrEmpty(_resetBufferSceneGuid))
            {
                // If both 'serializeField' and 'resetBuffer' are null or empty, and this is not during initialization,
                // it indicates that a new ID has been assigned. This results in a GUID conflict with the version control system.
                ChangingGuidWarning(nameof(serializeFieldSceneGuid));
                SetSceneGuidGroup(Guid.NewGuid().ToString());
            }
        }

        private void ResetSceneGuid()
        {
            SetSceneGuidGroup("");
        }
 
        private void SetSceneGuidGroup(string guid)
        {
            serializeFieldSceneGuid = guid;
            _resetBufferSceneGuid = guid;
        }

        private void SetSavableReferenceGuidGroup(int index, string guid)
        {
            serializeFieldSavableReferenceList[index].guid = guid;
            _resetBufferSavableReferenceList[index].guid = guid;
        }
        
        private void SetCurrentSavableGuidGroup(int index, string guid)
        {
            serializeFieldSavableReferenceList[index].guid = guid;
            _resetBufferSavableReferenceList[index].guid = guid;
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

        private void UpdateSavableComponents()
        {
            //if setting this dirty, the hierarchy changed event will trigger, resulting in an update behaviour
            var foundElements = ReflectionUtility.GetComponentsWithTypeCondition(gameObject, 
                ReflectionUtility.ContainsProperty<SavableAttribute>, ReflectionUtility.ContainsField<SavableAttribute>);
            
            //update removed elements and those that are kept 
            for (var index = serializeFieldCurrentSavableList.Count - 1; index >= 0; index--)
            {
                var currentSavableContainer = serializeFieldCurrentSavableList[index];
                
                if (!foundElements.Exists(x => x == currentSavableContainer.component))
                {
                    serializeFieldRemovedSavableList.Add(currentSavableContainer);
                    RemoveFromCurrentSavableGroup(currentSavableContainer);
                }
                else
                {
                    // If both 'serializeField' and 'resetBuffer' are null or empty, and this is not during initialization,
                    // it indicates that a new ID has been assigned. This results in a GUID conflict with the version control system.
                    if (string.IsNullOrEmpty(currentSavableContainer.guid) && string.IsNullOrEmpty(_resetBufferCurrentSavableList[index].guid))
                    {
                        ChangingGuidWarning(nameof(currentSavableContainer));
                        SetCurrentSavableGuidGroup(index, Guid.NewGuid().ToString());
                    }
                    
                    foundElements.Remove(currentSavableContainer.component);
                }
            }

            //add new elements
            foreach (Component foundElement in foundElements) 
            {
                var guid = Guid.NewGuid().ToString();
                
                //make sure a deleted element will have the same id after redo again
                var removedComponent = serializeFieldRemovedSavableList.Find(x => x.component == foundElement);
                if (removedComponent != null)
                {
                    guid = removedComponent.guid;
                    serializeFieldRemovedSavableList.Remove(removedComponent);
                }
                
                AddToCurrentSavableGroup(new ComponentsContainer
                {
                    guid = guid,
                    component = foundElement
                });
            }
        }
        
        private void UpdateSavableReferenceComponents()
        {
            if (serializeFieldSavableReferenceList.Count == 0) return;
            
            var savableReferenceContainer = serializeFieldSavableReferenceList[^1];
            var duplicates = serializeFieldSavableReferenceList.FindAll(x => x.component == savableReferenceContainer.component);
            for (var i = 0; i < duplicates.Count - 1; i++)
            {
                var lastElement = serializeFieldSavableReferenceList.FindLast(x => x.component == duplicates[i].component);
                lastElement.component = null;
            }
            
            if (savableReferenceContainer.component == null) return;

            //TODO: will always throw a warning anyway
            if (string.IsNullOrEmpty(savableReferenceContainer.guid) && string.IsNullOrEmpty(_resetBufferSavableReferenceList[^1].guid))
            {
                // If both 'serializeField' and 'resetBuffer' are null or empty, and this is not during initialization,
                // it indicates that a new ID has been assigned. This results in a GUID conflict with the version control system.
                ChangingGuidWarning(nameof(savableReferenceContainer));
                SetSavableReferenceGuidGroup(serializeFieldSavableReferenceList.Count - 1, Guid.NewGuid().ToString());
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
        public string guid;
        public Component component;
    }
}
