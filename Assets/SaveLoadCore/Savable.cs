using System;
using System.Collections.Generic;
using SaveLoadCore.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveLoadCore
{
    [DisallowMultipleComponent]
    public class Savable : MonoBehaviour, ICreateGameObjectHierarchy, IChangeComponentProperties, IChangeGameObjectProperties, IChangeGameObjectStructure, IChangeGameObjectStructureHierarchy
    {
        [SerializeField] private string hierarchyPath;
        [SerializeField] private GameObject prefabSource;
        
        [SerializeField] private string serializeFieldSceneGuid;
        private string _resetBufferSceneGuid;
        
        [SerializeField] private List<ComponentsContainer> serializeFieldSavableList = new();
        private readonly List<ComponentsContainer> _resetBufferSavableList = new();

        [SerializeField] private List<ComponentsContainer> serializeFieldSavableReferenceList = new();
        private readonly List<ComponentsContainer> _resetBufferSavableReferenceList = new();

        public string HierarchyPath => hierarchyPath;
        public GameObject PrefabSource => prefabSource;
        public string SceneGuid => serializeFieldSceneGuid;
        public List<ComponentsContainer> SavableList => serializeFieldSavableList;
        public List<ComponentsContainer> ReferenceList => serializeFieldSavableReferenceList;

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
            serializeFieldSavableList.Clear();
            foreach (var savableContainer in _resetBufferSavableList)
            {
                serializeFieldSavableList.Add(savableContainer);
            }
            
            serializeFieldSavableReferenceList.Clear();
            foreach (var referenceContainer in _resetBufferSavableReferenceList)
            {
                serializeFieldSavableReferenceList.Add(referenceContainer);
            }
        }

        /// <summary>
        /// Serialize Fields will be serialized through script reloads and application restarts. The Reset Buffer values
        /// will be lost. This method will reapply the lost values for the Reset Buffer with the Serialize Fields. This
        /// prevents loosing the original guid.
        /// </summary>
        private void ApplyScriptReloadBuffer()
        {
            if (serializeFieldSavableList.Count != _resetBufferSavableList.Count)
            {
                _resetBufferSavableList.Clear();
                foreach (var savableContainer in serializeFieldSavableList)
                {
                    _resetBufferSavableList.Add(savableContainer);
                }
            }

            if (serializeFieldSavableReferenceList.Count != _resetBufferSavableReferenceList.Count)
            {
                _resetBufferSavableReferenceList.Clear();
                foreach (var referenceContainer in serializeFieldSavableReferenceList)
                {
                    _resetBufferSavableReferenceList.Add(referenceContainer);
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
            hierarchyPath = gameObject.GetScenePath() + "/";
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
        
        private void SetSavableGuidGroup(int index, string guid)
        {
            serializeFieldSavableReferenceList[index].guid = guid;
            _resetBufferSavableReferenceList[index].guid = guid;
        }

        private void AddToSavableGroup(ComponentsContainer componentsContainer)
        {
            serializeFieldSavableList.Add(componentsContainer);
            _resetBufferSavableList.Add(componentsContainer);
        }

        private void RemoveFromSavableGroup(ComponentsContainer componentsContainer)
        {
            serializeFieldSavableList.Remove(componentsContainer);
            _resetBufferSavableList.Remove(componentsContainer);
        }

        private void UpdateSavableComponents()
        {
            //if setting this dirty, the hierarchy changed event will trigger, resulting in an update behaviour
            var foundElements = ReflectionUtility.GetComponentsWithTypeCondition(gameObject, 
                ReflectionUtility.ClassHasAttribute<SavableAttribute>,
                ReflectionUtility.ContainsProperty<SavableMemberAttribute>, 
                ReflectionUtility.ContainsField<SavableMemberAttribute>,
                ReflectionUtility.ContainsInterface<ISavable>);
            
            //update removed elements and those that are kept 
            for (var index = serializeFieldSavableList.Count - 1; index >= 0; index--)
            {
                var savableContainer = serializeFieldSavableList[index];
                
                if (!foundElements.Exists(x => x == savableContainer.component))
                {
                    RemoveFromSavableGroup(savableContainer);
                }
                else
                {
                    // If both 'serializeField' and 'resetBuffer' are null or empty, and this is not during initialization,
                    // it indicates that a new ID has been assigned. This results in a GUID conflict with the version control system.
                    if (string.IsNullOrEmpty(savableContainer.guid) && string.IsNullOrEmpty(_resetBufferSavableList[index].guid))
                    {
                        ChangingGuidWarning(nameof(savableContainer));
                        SetSavableGuidGroup(index, Guid.NewGuid().ToString());
                    }
                    
                    foundElements.Remove(savableContainer.component);
                }
            }

            //add new elements
            foreach (Component foundElement in foundElements) 
            {
                var guid = Guid.NewGuid().ToString();
                
                AddToSavableGroup(new ComponentsContainer
                {
                    guid = guid,
                    component = foundElement
                });
            }
        }
        
        private void UpdateSavableReferenceComponents()
        {
            if (serializeFieldSavableReferenceList.Count == 0) return;
            
            var referenceContainer = serializeFieldSavableReferenceList[^1];
            var duplicates = serializeFieldSavableReferenceList.FindAll(x => x.component == referenceContainer.component);
            for (var i = 0; i < duplicates.Count - 1; i++)
            {
                var lastElement = serializeFieldSavableReferenceList.FindLast(x => x.component == duplicates[i].component);
                lastElement.component = null;
            }
            
            if (referenceContainer.component == null) return;

            if (string.IsNullOrEmpty(referenceContainer.guid) && string.IsNullOrEmpty(_resetBufferSavableReferenceList[^1].guid))
            {
                // If both 'serializeField' and 'resetBuffer' are null or empty, and this is not during initialization,
                // it indicates that a new ID has been assigned. This results in a GUID conflict with the version control system.
                ChangingGuidWarning(nameof(referenceContainer));
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
