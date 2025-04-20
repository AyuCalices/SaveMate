using System.Collections.Generic;
using System.Linq;
using SaveMate.Core.SaveComponents.GameObjectScope;
using SaveMate.Utility;
using SaveMate.Utility.PreventReset;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaveMate.Core.SaveComponents.AssetScope
{
    [CreateAssetMenu(fileName = "AssetRegistry", menuName = "Save Mate/Asset Registry")]
    public class AssetRegistry : ScriptableObject
    {
        #region Fields & Properties

        
        [SerializeField] private NonResetableList<Savable> prefabSavables = new();
        [SerializeField] private NonResetableList<UnityObjectIdentification> scriptableObjectSavables = new();

        internal List<Savable> PrefabSavables => prefabSavables;
        internal List<UnityObjectIdentification> ScriptableObjectSavables => scriptableObjectSavables;

        
        #endregion
        
        #region Unity Lifecycle

        
        private void OnEnable()
        {
            Savable.OnValidateSavable += OnValidateSavable;
        }

        private void OnDisable()
        {
            Savable.OnValidateSavable -= OnValidateSavable;
        }

        
        #endregion
        
        #region ScriptableObject Handling

        
        private void OnValidate()
        {
            //needed if the user does bad id entries
            FixMissingScriptableObjectGuid();
            UpdateScriptableObjectGuidOnInspectorInput();
            
            SaveLoadUtility.SetDirty(this);
        }
        
        private void FixMissingScriptableObjectGuid()
        {
            foreach (var unityObjectIdentification in scriptableObjectSavables.values)
            {
                if (string.IsNullOrEmpty(unityObjectIdentification.guid))
                {
                    unityObjectIdentification.guid = GenerateUniquePrefabGuid(unityObjectIdentification.unityObject);
                }
            }
        }
        
        private string GenerateUniquePrefabGuid(Object scriptableObject)
        {
            var newGuid = "ScriptableObject_" + scriptableObject.name + "_" + SaveLoadUtility.GenerateId();
            
            while (ScriptableObjectSavables.Exists(x => x.guid == newGuid))
            {
                newGuid = "ScriptableObject_" + scriptableObject.name + "_" + SaveLoadUtility.GenerateId();
            }

            return newGuid;
        }

        private void UpdateScriptableObjectGuidOnInspectorInput()
        {
            SaveLoadUtility.CheckUniqueGuidOnInspectorInput(ScriptableObjectSavables,
                obj => obj.unityObject,
                obj => obj.guid,
                "Duplicate Guid for different 'PrefabGuid' detected!");
        }
        
        internal void AddSavableScriptableObject(ScriptableObject scriptableObject)
        {
            if (ScriptableObjectSavables.Exists(x => x.unityObject == scriptableObject)) return;

            var id = GenerateUniquePrefabGuid(scriptableObject);
            ScriptableObjectSavables.Add(new UnityObjectIdentification(id, scriptableObject));
            
            SaveLoadUtility.SetDirty(this);
        }

        
        #endregion
        
        #region Prefab Handling

        
        private void OnValidateSavable(Savable savable)
        {
            InitializeUniquePrefabGuid(savable);    //if no id -> reaply
            CheckUniquePrefabGuidOnInspectorInput();
        }
        
        private void CheckUniquePrefabGuidOnInspectorInput()
        {
            SaveLoadUtility.CheckUniqueGuidOnInspectorInput(PrefabSavables,
                obj => obj,
                obj => obj.PrefabGuid,
                "Duplicate Guid for different 'PrefabGuid' detected!");
        }
        
        private void InitializeUniquePrefabGuid(Savable savable)
        {
            if (string.IsNullOrEmpty(savable.PrefabGuid))
            {
                savable.PrefabGuid = GenerateUniquePrefabGuid(savable);
            }
        }
        
        private string GenerateUniquePrefabGuid(Savable savable)
        {
            //generate guid
            var guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            
            while (PrefabSavables.Any(x => x.PrefabGuid == guid))
            {
                guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            }

            return guid;
        }
        
        internal void AddSavablePrefab(Savable savable)
        {
            if (PrefabSavables.Exists(x => x == savable)) return;

            InitializeUniquePrefabGuid(savable);
            PrefabSavables.Add(savable);

            SaveLoadUtility.SetDirty(this);
        }

        
        #endregion
    }
}
