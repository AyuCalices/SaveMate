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

        [SerializeField] private bool customSpawning;
        
        [SerializeField] private List<ComponentsContainer> serializeFieldSavableList = new();
        private readonly List<ComponentsContainer> _resetBufferSavableList = new();

        [SerializeField] private List<ComponentsContainer> serializeFieldSavableReferenceList = new();
        private readonly List<ComponentsContainer> _resetBufferSavableReferenceList = new();

        public string SceneGuid => serializeFieldSceneGuid;
        public string PrefabGuid => prefabPath;
        public bool CustomSpawning => customSpawning;   //TODO: implement
        public List<ComponentsContainer> SavableList => serializeFieldSavableList;
        public List<ComponentsContainer> ReferenceList => serializeFieldSavableReferenceList;
        
        private void Reset()
        {
            ApplyResetBuffer();
        }

        private void Awake()
        {
            SetupSceneGuid();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            
            ApplyScriptReloadBuffer();
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
            List<UnityEngine.Object> foundElements = ReflectionUtility.GetComponentsWithTypeCondition(gameObject, 
                ReflectionUtility.ClassHasAttribute<SavableSchemaAttribute>,
                ReflectionUtility.ContainsProperty<SavableAttribute>, 
                ReflectionUtility.ContainsField<SavableAttribute>,
                ReflectionUtility.ContainsInterface<ISavable>);
            
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
                
                AddToSavableGroup(new ComponentsContainer
                {
                    guid = guid,
                    unityObject = foundElement
                });
            }
        }

        private void SetupDefaultSavableReferenceComponents()
        {
            if (!serializeFieldSavableReferenceList.Exists(x => x.unityObject == transform))
            {
                serializeFieldSavableReferenceList.Add(new ComponentsContainer{unityObject = transform, guid = Guid.NewGuid().ToString()});
            }
            
            if (!serializeFieldSavableReferenceList.Exists(x => x.unityObject == gameObject))
            {
                serializeFieldSavableReferenceList.Add(new ComponentsContainer{unityObject = gameObject, guid = Guid.NewGuid().ToString()});
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
