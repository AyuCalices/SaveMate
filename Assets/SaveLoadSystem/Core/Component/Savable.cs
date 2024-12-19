using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.Attributes;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core.Component
{
    [DisallowMultipleComponent]
    public class Savable : MonoBehaviour, ICreateGameObjectHierarchy, IChangeComponentProperties, IChangeGameObjectProperties, IChangeGameObjectStructure, IChangeGameObjectStructureHierarchy
    {
        [SerializeField] private string serializeFieldSceneGuid;
        private string _resetBufferSceneGuid;

        [SerializeField] private string prefabPath;

        [SerializeField] private bool dynamicPrefabSpawningDisabled;
        
        [SerializeField] private List<ComponentsContainer> serializeFieldSavableList = new();
        private readonly List<ComponentsContainer> _resetBufferSavableList = new();

        [SerializeField] private List<ComponentsContainer> serializeFieldSavableReferenceList = new();
        private readonly List<ComponentsContainer> _resetBufferSavableReferenceList = new();

        
        public string SceneGuid => serializeFieldSceneGuid;
        public string PrefabGuid => prefabPath;
        public bool DynamicPrefabSpawningDisabled => dynamicPrefabSpawningDisabled;
        public List<ComponentsContainer> SavableList => serializeFieldSavableList;
        public List<ComponentsContainer> ReferenceList => serializeFieldSavableReferenceList;

        
        private SaveSceneManager _saveSceneManager;
        
        
        private void Reset()
        {
            ApplySavableListResetBuffer();
            ApplySceneGuidResetBuffer();
            
        }

        private void Awake()
        {
            RegisterToSceneManager();
            
            SetupSceneGuid();
        }

        private void OnDestroy()
        {
            UnregisterFromSceneManager();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            
            RegisterToSceneManager();
            
            SetupSavableListResetBuffer();
            SetupSceneGuidResetBuffer();
            SetupAll();
        }
        
        public void OnCreateGameObjectHierarchy()
        {
            if (Application.isPlaying) return;
            
            SetupAll();
        }
        
        public void OnChangeGameObjectStructure()
        {
            if (Application.isPlaying) return;
            
            SetupAll();
        }
        
        public void OnChangeComponentProperties()
        {
            if (Application.isPlaying) return;
            
            SetupAll();
        }

        public void OnChangeGameObjectProperties()
        {
            if (Application.isPlaying) return;
            
            SetupAll();
        }
        
        public void OnChangeGameObjectStructureHierarchy()
        {
            if (Application.isPlaying) return;
            
            SetupAll();
        }

        private bool AcquireSceneManager()
        {
            if (_saveSceneManager.IsUnityNull())
            {
                _saveSceneManager = FindObjectOfType<SaveSceneManager>();
                
                if (_saveSceneManager.IsUnityNull())
                {
                    Debug.LogWarning("[SaveMate] SaveSceneManager is missing in the current scene. Please ensure a SaveSceneManager is present to enable proper functionality.");
                    return false;
                }
            }

            return true;
        }
        
        private void RegisterToSceneManager()
        {
            if (AcquireSceneManager() && gameObject.scene.IsValid())
            {
                _saveSceneManager.RegisterSavable(this);
            }
        }
        
        private void UnregisterFromSceneManager()
        {
            if (!_saveSceneManager.IsUnityNull() && gameObject.scene.IsValid())
            {
                _saveSceneManager.UnregisterSavable(this);
            }
        }
        
        /// <summary>
        /// If the Component is being resetted, all Serialize Field values are lost. This method will reapply the lost values
        /// for the Serialize Fields with the Reset Buffer. This prevents loosing the original guid.
        /// </summary>
        private void ApplySavableListResetBuffer()
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
        /// If a Component is being resetted, all Serialize Field values are lost. This method will reapply the lost values
        /// for the Scene Guid. This prevents loosing the original guid.
        /// </summary>
        private void ApplySceneGuidResetBuffer()
        {
            serializeFieldSceneGuid = _resetBufferSceneGuid;
        }

        /// <summary>
        /// Serialize Fields will be serialized through script reloads and application restarts. The Reset Buffer values
        /// will be lost. This method will reapply the lost values for the Reset Buffer with the Serialize Fields. This
        /// prevents loosing the original guid.
        /// </summary>
        private void SetupSavableListResetBuffer()
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
        
        /// <summary>
        /// Serialize Fields will be serialized through script reloads and application restarts. The Reset Buffer values
        /// will be lost. This method will reapply the lost values for the Reset Buffer with the Serialize Fields. This
        /// prevents loosing the original guid.
        /// </summary>
        private void SetupSceneGuidResetBuffer()
        {
            _resetBufferSceneGuid = serializeFieldSceneGuid;
        }

        private void SetupAll()
        {
            UpdateSavableComponents();
            SetupDefaultSavableReferenceComponents();
            UpdateSavableReferenceComponents();

            if (gameObject.scene.name != null)
            {
                SetupSceneGuid();
            }
            else
            {
                ResetSceneGuid();
            }
            
            SetDirty(this);
        }
        
        private void SetupSceneGuid()
        {
            if (string.IsNullOrEmpty(serializeFieldSceneGuid) && string.IsNullOrEmpty(_resetBufferSceneGuid))
            {
                SetSceneGuidGroup(Guid.NewGuid().ToString());
            }
        }

        private void ResetSceneGuid()
        {
            SetSceneGuidGroup("");
        }
 
        public void SetSceneGuidGroup(string guid)
        {
            serializeFieldSceneGuid = guid;
            _resetBufferSceneGuid = guid;
        }

        public void SetPrefabPath(string newPrefabPath)
        {
            prefabPath = newPrefabPath;
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
            var foundElements = TypeUtility.GetComponentsWithTypeCondition(gameObject, 
                TypeUtility.ClassHasAttribute<SavableObjectAttribute>,
                TypeUtility.ContainsProperty<SavableAttribute>, 
                TypeUtility.ContainsField<SavableAttribute>,
                TypeUtility.ContainsInterface<ISavable>);
            
            //update removed elements and those that are kept 
            for (var index = serializeFieldSavableList.Count - 1; index >= 0; index--)
            {
                var savableContainer = serializeFieldSavableList[index];
                
                if (!foundElements.Exists(x => x == savableContainer.unityObject))
                {
                    RemoveFromSavableGroup(savableContainer);
                }
                else
                {
                    if (string.IsNullOrEmpty(savableContainer.guid) && string.IsNullOrEmpty(_resetBufferSavableList[index].guid))
                    {
                        SetSavableGuidGroup(index, Guid.NewGuid().ToString());
                    }
                    
                    foundElements.Remove(savableContainer.unityObject);
                }
            }

            //add new elements
            foreach (UnityEngine.Object foundElement in foundElements) 
            {
                var guid = Guid.NewGuid().ToString();
                
                AddToSavableGroup(new ComponentsContainer(guid, foundElement));
            }
        }

        private void SetupDefaultSavableReferenceComponents()
        {
            if (!serializeFieldSavableReferenceList.Exists(x => x.unityObject == transform))
            {
                serializeFieldSavableReferenceList.Add(new ComponentsContainer(Guid.NewGuid().ToString(), transform));
            }
            
            if (!serializeFieldSavableReferenceList.Exists(x => x.unityObject == gameObject))
            {
                serializeFieldSavableReferenceList.Add(new ComponentsContainer(Guid.NewGuid().ToString(), gameObject));
            }
        }
        
        private void UpdateSavableReferenceComponents()
        {
            if (serializeFieldSavableReferenceList.Count == 0) return;
            
            var referenceContainer = serializeFieldSavableReferenceList[^1];
            var duplicates = serializeFieldSavableReferenceList.FindAll(x => x.unityObject == referenceContainer.unityObject);
            for (var i = 0; i < duplicates.Count - 1; i++)
            {
                var lastElement = serializeFieldSavableReferenceList.FindLast(x => x.unityObject == duplicates[i].unityObject);
                lastElement.unityObject = null;
            }
            
            if (referenceContainer.unityObject == null) return;

            if (string.IsNullOrEmpty(referenceContainer.guid) && string.IsNullOrEmpty(_resetBufferSavableReferenceList[^1].guid))
            {
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
}
