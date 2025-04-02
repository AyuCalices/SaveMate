using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using SaveLoadSystem.Utility.PreventReset;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.UnityComponent
{
    [DisallowMultipleComponent]
    public class Savable : MonoBehaviour, IChangeGameObjectStructure
    {
        [SerializeField] private bool dynamicPrefabSpawningDisabled;
        
        [SerializeField] private NonResetable<string> prefabGuid;
        [SerializeField] private NonResetable<string> sceneGuid;
        
        [SerializeField] private NonResetableList<UnityObjectIdentification> savableLookup = new();
        [SerializeField] private NonResetableList<UnityObjectIdentification> duplicateComponentLookup = new();

        
        public static event Action<Savable> OnValidateSavable;
        
        
        public string SceneGuid
        {
            get => sceneGuid;
            internal set => sceneGuid.value = value;
        }

        public string PrefabGuid
        {
            get => prefabGuid;
            internal set => prefabGuid.value = value;
        }

        
        public bool DynamicPrefabSpawningDisabled => dynamicPrefabSpawningDisabled;
        public List<UnityObjectIdentification> SavableLookup => savableLookup;
        public List<UnityObjectIdentification> DuplicateComponentLookup => duplicateComponentLookup;
        
        
        private Scene _lastScene;
        private SaveSceneManager _saveSceneManager;

        private void Awake()
        {
            _lastScene = gameObject.scene;
            RegisterToSceneManager();
        }

        private void Update()
        {
            if (gameObject.scene != _lastScene)
            {
                UnregisterFromSceneManager();
                RegisterToSceneManager();
                _lastScene = gameObject.scene;
            }
        }

        private void OnDestroy()
        {
            UnregisterFromSceneManager();
        }
        
        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            
            RegisterToSceneManager();
            
            //update prefab guid
            OnValidateSavable?.Invoke(this);
            
            CheckUniqueISavableGuidOnInspectorInput();
            CheckUniqueDuplicateComponentGuidOnInspectorInput();
            
            UnityUtility.SetDirty(this);
        }
        
        /*
         * Currently the system only supports adding savable-components during editor mode.
         * This is by design, to prevent the necessary to save the type of the component.
         * optional todo: find a way to save added savable-components similar to dynamic prefab spawning without saving the type.
         */
        public void OnChangeGameObjectStructure()
        {
            UpdateSavableComponents();
            UpdateSavableReferenceComponents();
            
            UnityUtility.SetDirty(this);
        }
        
        private void RegisterToSceneManager()
        {
            if (!gameObject.scene.IsValid()) return;

            if (gameObject.scene.name == "DontDestroyOnLoad")
            {
                Debug.LogWarning("There is no support for saving elements inside dont destroy on load!");
            }
            else if (AcquireSceneManager())
            {
                _saveSceneManager.RegisterSavable(this);
            }
            else
            {
                SceneGuid = null;
            }
        }
        
        private bool AcquireSceneManager()
        {
            if (_saveSceneManager.IsUnityNull())
            {
                try
                {
                    var sceneManagers = FindObjectsOfType<SaveSceneManager>();
                    _saveSceneManager = sceneManagers.First(x => x.gameObject.scene == gameObject.scene);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SaveMate] Internal Error: " + e);
                    return false;
                }
            }

            return true;
        }
        
        private void UnregisterFromSceneManager()
        {
            if (!_saveSceneManager.IsUnityNull())
            {
                _saveSceneManager.UnregisterSavable(this);
            }
        }

        private void CheckUniqueISavableGuidOnInspectorInput()
        {
            SaveLoadUtility.CheckUniqueGuidOnInspectorInput(savableLookup.values,
                obj => obj.unityObject,
                obj => obj.guid,
                $"Duplicate Guid on GameObject '{gameObject.name}' for different 'ISavable Components' detected!");
        }
        
        private void CheckUniqueDuplicateComponentGuidOnInspectorInput()
        {
            SaveLoadUtility.CheckUniqueGuidOnInspectorInput(duplicateComponentLookup.values,
                obj => obj.unityObject,
                obj => obj.guid,
                $"Duplicate Guid on GameObject '{gameObject.name}' for different 'Duplicated Components' detected!");
        }

        private void UpdateSavableComponents()
        {
            //if setting this dirty, the hierarchy changed event will trigger, resulting in an update behaviour
            var foundElements = TypeUtility.GetComponentsWithTypeCondition(gameObject, TypeUtility.ContainsType<ISavable>);
            
            //update removed elements and those that are kept 
            for (var index = SavableLookup.Count - 1; index >= 0; index--)
            {
                var objectId = SavableLookup[index];
                
                if (!foundElements.Exists(x => x == objectId.unityObject))
                {
                    SavableLookup.Remove(objectId);
                }
                else
                {
                    if (string.IsNullOrEmpty(objectId.guid))
                    {
                        SavableLookup[index].guid = GetUniqueSavableID();
                    }
                    
                    foundElements.Remove((Component)objectId.unityObject);
                }
            }

            //add new elements
            foreach (var foundElement in foundElements) 
            {
                var guid = GetUniqueSavableID();
                
                SavableLookup.Add(new UnityObjectIdentification(guid, foundElement));
            }
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

            for (var index = DuplicateComponentLookup.Count - 1; index >= 0; index--)
            {
                var objectId = DuplicateComponentLookup[index];
                
                if (!duplicates.Exists(x => x == objectId.unityObject))
                {
                    DuplicateComponentLookup.Remove(objectId);
                }
                else
                {
                    
                    if (string.IsNullOrEmpty(objectId.guid))
                    {
                        DuplicateComponentLookup[index].guid = GetUniqueDuplicateID();
                    }
                    
                    duplicates.Remove((Component)objectId.unityObject);
                }
            }
            
            //add new elements
            foreach (var foundElement in duplicates) 
            {
                var guid = GetUniqueSavableID();
                
                DuplicateComponentLookup.Add(new UnityObjectIdentification(guid, foundElement));
            }
        }
        
        private string GetUniqueSavableID()
        {
            var guid = "Component_" + SaveLoadUtility.GenerateId();
            
            while (SavableLookup != null && SavableLookup.Exists(x => x.guid == guid))
            {
                guid = "Component_" + SaveLoadUtility.GenerateId();
            }

            return guid;
        }
        
        private string GetUniqueDuplicateID()
        {
            var guid = "Component_" + SaveLoadUtility.GenerateId();
            
            while (DuplicateComponentLookup != null && DuplicateComponentLookup.Exists(x => x.guid == guid))
            {
                guid = "Component_" + SaveLoadUtility.GenerateId();
            }

            return guid;
        }
    }
}
