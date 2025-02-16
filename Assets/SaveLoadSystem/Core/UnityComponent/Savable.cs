using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core.UnityComponent
{
    [DisallowMultipleComponent]
    public class Savable : MonoBehaviour
    {
        [SerializeField] private string sceneGuid;
        private string _resetBufferSceneGuid;

        [SerializeField] private string prefabPath;
        private string _resetBufferPrefabPath;

        [SerializeField] private bool dynamicPrefabSpawningDisabled;
        
        [SerializeField] private List<UnityObjectIdentification> savableLookup = new();
        private readonly List<UnityObjectIdentification> _resetBufferSavableLookup = new();

        [SerializeField] private List<UnityObjectIdentification> duplicateComponentLookup = new();
        private readonly List<UnityObjectIdentification> _resetBufferDuplicateComponentLookup = new();

        
        public string SceneGuid => sceneGuid;
        public string PrefabGuid => prefabPath;
        public bool DynamicPrefabSpawningDisabled => dynamicPrefabSpawningDisabled;
        public List<UnityObjectIdentification> SavableLookup => savableLookup;
        public List<UnityObjectIdentification> DuplicateComponentLookup => duplicateComponentLookup;

        
        private SaveSceneManager _saveSceneManager;
        
        
        private void Reset()
        {
            ApplySavableListResetBuffer();
            ApplySceneGuidResetBuffer();
            ApplyPrefabPathResetBuffer();
        }

        private void Awake()
        {
            RegisterToSceneManager();
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
            SetupPrefabPathResetBuffer();
            
            SetupAll();
        }

        private bool AcquireSceneManager()
        {
            if (_saveSceneManager.IsUnityNull())
            {
                var sceneManagers = FindObjectsOfType<SaveSceneManager>();
                _saveSceneManager = sceneManagers.First(x => x.gameObject.scene == gameObject.scene);
                
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
            if (gameObject.scene.IsValid() && AcquireSceneManager())
            {
                _saveSceneManager.RegisterSavable(this);
            }
            else
            {
                SetSceneGuidGroup(null);
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
            savableLookup.Clear();
            foreach (var referenceContainer in _resetBufferSavableLookup)
            {
                savableLookup.Add(referenceContainer);
            }
            
            duplicateComponentLookup.Clear();
            foreach (var referenceContainer in _resetBufferDuplicateComponentLookup)
            {
                duplicateComponentLookup.Add(referenceContainer);
            }
        }

        /// <summary>
        /// If a Component is being resetted, all Serialize Field values are lost. This method will reapply the lost values
        /// for the Scene Guid. This prevents loosing the original guid.
        /// </summary>
        private void ApplySceneGuidResetBuffer()
        {
            sceneGuid = _resetBufferSceneGuid;
        }
        
        private void ApplyPrefabPathResetBuffer()
        {
            prefabPath = _resetBufferPrefabPath;
        }

        /// <summary>
        /// Serialize Fields will be serialized through script reloads and application restarts. The Reset Buffer values
        /// will be lost. This method will reapply the lost values for the Reset Buffer with the Serialize Fields. This
        /// prevents loosing the original guid.
        /// </summary>
        private void SetupSavableListResetBuffer()
        {
            if (savableLookup.Count != _resetBufferSavableLookup.Count)
            {
                _resetBufferSavableLookup.Clear();
                foreach (var referenceContainer in savableLookup)
                {
                    _resetBufferSavableLookup.Add(referenceContainer);
                }
            }
            
            if (duplicateComponentLookup.Count != _resetBufferDuplicateComponentLookup.Count)
            {
                _resetBufferDuplicateComponentLookup.Clear();
                foreach (var referenceContainer in duplicateComponentLookup)
                {
                    _resetBufferDuplicateComponentLookup.Add(referenceContainer);
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
            _resetBufferSceneGuid = sceneGuid;
        }
        
        private void SetupPrefabPathResetBuffer()
        {
            _resetBufferPrefabPath = prefabPath;
        }

        private void SetupAll()
        {
            UpdateSavableComponents();
            UpdateSavableReferenceComponents();
            
            SetDirty(this);
        }
 
        internal void SetSceneGuidGroup(string guid)
        {
            sceneGuid = guid;
            _resetBufferSceneGuid = guid;
        }

        internal void SetPrefabPath(string newPrefabPath)
        {
            prefabPath = newPrefabPath;
            _resetBufferPrefabPath = newPrefabPath;
        }
        
        private void SetSavableGuidGroup(int index, string guid)
        {
            savableLookup[index].guid = guid;
            _resetBufferSavableLookup[index].guid = guid;
        }
        
        private void AddToSavableGroup(UnityObjectIdentification unityObjectIdentification)
        {
            savableLookup.Add(unityObjectIdentification);
            _resetBufferSavableLookup.Add(unityObjectIdentification);
        }

        private void RemoveFromSavableGroup(UnityObjectIdentification unityObjectIdentification)
        {
            savableLookup.Remove(unityObjectIdentification);
            _resetBufferSavableLookup.Remove(unityObjectIdentification);
        }

        private void UpdateSavableComponents()
        {
            //if setting this dirty, the hierarchy changed event will trigger, resulting in an update behaviour
            var foundElements = TypeUtility.GetComponentsWithTypeCondition(gameObject, TypeUtility.ContainsType<ISavable>);
            
            //update removed elements and those that are kept 
            for (var index = savableLookup.Count - 1; index >= 0; index--)
            {
                var objectId = savableLookup[index];
                
                if (!foundElements.Exists(x => x == objectId.unityObject))
                {
                    RemoveFromSavableGroup(objectId);
                }
                else
                {
                    if (string.IsNullOrEmpty(objectId.guid))
                    {
                        SetSavableGuidGroup(index, GetUniqueSavableID((Component)objectId.unityObject));
                    }
                    
                    foundElements.Remove((Component)objectId.unityObject);
                }
            }

            //add new elements
            foreach (Component foundElement in foundElements) 
            {
                var guid = Guid.NewGuid().ToString();
                
                AddToSavableGroup(new UnityObjectIdentification(guid, foundElement));
            }
        }

        private string GetUniqueSavableID(Component component)
        {
            var guid = "Component_" + component.name + "_" + SaveLoadUtility.GenerateId();
            
            while (savableLookup != null && savableLookup.Exists(x => x.guid == guid))
            {
                guid = "Component_" + component.name + "_" + SaveLoadUtility.GenerateId();
            }

            return guid;
        }
        
        private void SetDuplicatedComponentGuidGroup(int index, string guid)
        {
            duplicateComponentLookup[index].guid = guid;
            _resetBufferDuplicateComponentLookup[index].guid = guid;
        }
        
        private void AddToDuplicatedComponentGroup(UnityObjectIdentification unityObjectIdentification)
        {
            duplicateComponentLookup.Add(unityObjectIdentification);
            _resetBufferDuplicateComponentLookup.Add(unityObjectIdentification);
        }

        private void RemoveFromDuplicatedComponentGroup(UnityObjectIdentification unityObjectIdentification)
        {
            duplicateComponentLookup.Remove(unityObjectIdentification);
            _resetBufferDuplicateComponentLookup.Remove(unityObjectIdentification);
        }
        
        private void UpdateSavableReferenceComponents()
        {
            var duplicates = UnityUtility.GetDuplicateComponents(gameObject);

            //remove duplicates, that implement the savable component: all savables need an id
            for (var index = duplicates.Count - 1; index >= 0; index--)
            {
                var duplicate = duplicates[index];
                if (duplicate is ISavable)
                {
                    duplicates.Remove(duplicate);
                }
            }

            for (var index = duplicateComponentLookup.Count - 1; index >= 0; index--)
            {
                var objectId = duplicateComponentLookup[index];
                
                if (!duplicates.Exists(x => x == objectId.unityObject))
                {
                    RemoveFromDuplicatedComponentGroup(objectId);
                }
                else
                {
                    if (string.IsNullOrEmpty(objectId.guid))
                    {
                        SetDuplicatedComponentGuidGroup(index, GetUniqueDuplicateID((Component)objectId.unityObject));
                    }
                    
                    duplicates.Remove(objectId.unityObject);
                }
            }
            
            //add new elements
            foreach (UnityEngine.Object foundElement in duplicates) 
            {
                var guid = Guid.NewGuid().ToString();
                
                AddToDuplicatedComponentGroup(new UnityObjectIdentification(guid, foundElement));
            }
        }
        
        private string GetUniqueDuplicateID(Component component)
        {
            var guid = "Component_" + component.name + "_" + SaveLoadUtility.GenerateId();
            
            while (duplicateComponentLookup != null && duplicateComponentLookup.Exists(x => x.guid == guid))
            {
                guid = "Component_" + component.name + "_" + SaveLoadUtility.GenerateId();
            }

            return guid;
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
