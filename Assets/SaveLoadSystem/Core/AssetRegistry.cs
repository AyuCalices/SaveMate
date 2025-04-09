using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.UnityComponent;
using SaveLoadSystem.Utility;
using SaveLoadSystem.Utility.PreventReset;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class AssetRegistry : ScriptableObject
    {
        [SerializeField] private NonResetableList<Savable> prefabSavables = new();
        [SerializeField] private NonResetableList<UnityObjectIdentification> scriptableObjectSavables = new();

        public List<Savable> PrefabSavables => prefabSavables;
        public List<UnityObjectIdentification> ScriptableObjectSavables => scriptableObjectSavables;

        private void OnEnable()
        {
            Savable.OnValidateSavable += OnValidateSavable;
        }

        private void OnDisable()
        {
            Savable.OnValidateSavable -= OnValidateSavable;
        }

        #region ScriptableObject Handling

        //needed if the user does bad id entries
        private void OnValidate()
        {
            //ScriptableObject
            FixMissingScriptableObjectGuid();
            UpdateScriptableObjectGuidOnInspectorInput();
            
            UnityUtility.SetDirty(this);
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
            
            UnityUtility.SetDirty(this);
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

            UnityUtility.SetDirty(this);
        }

        #endregion
    }
}
