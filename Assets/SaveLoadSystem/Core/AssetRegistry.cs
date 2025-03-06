using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.UnityComponent;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class AssetRegistry : ScriptableObject
    {
        [SerializeField] private List<Savable> prefabSavables = new();
        [SerializeField] private List<UnityObjectIdentification> scriptableObjectSavables = new();

        public List<Savable> PrefabSavables => prefabSavables;
        public List<UnityObjectIdentification> ScriptableObjectSavables => scriptableObjectSavables;

        private void OnValidate()
        {
            FixMissingPrefabID();
            FixMissingScriptableObjectID();

            DetectDuplicateScriptableObjectIDs();
            
            SetDirty();
        }

        private void DetectDuplicateScriptableObjectIDs()
        {
            var assetRegistries = UnityUtility.FindAllScriptableObjects<AssetRegistry>();
            foreach (var unityObjectIdentification in scriptableObjectSavables)
            {
                int count = 0;
                
                foreach (var assetRegistry in assetRegistries)
                {
                    count += assetRegistry.ScriptableObjectSavables.Count(x => x.guid == unityObjectIdentification.guid);
                }
                
                if (count >= 2)
                {
                    Debug.LogError($"Duplicate ID '{unityObjectIdentification.guid}' detected! ScriptableObject IDs must be unique across all " +
                                   $"Asset Registries. Please change your latest edit to ensure uniqueness.");
                }
            }
        }

        internal void AddSavablePrefab(Savable savable)
        {
            if (string.IsNullOrEmpty(savable.PrefabGuid))
            {
                savable.PrefabGuid = GeneratePrefabID(savable);
            }

            if (!prefabSavables.Exists(x => x == savable))
            {
                prefabSavables.Add(savable);
            }

            SetDirty();
        }

        private void FixMissingPrefabID()
        {
            foreach (var prefabSavable in prefabSavables)
            {
                if (string.IsNullOrEmpty(prefabSavable.PrefabGuid))
                {
                    prefabSavable.PrefabGuid = GeneratePrefabID(prefabSavable);
                }
            }
        }

        private string GeneratePrefabID(Savable savable)
        {
            var guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            
            while (prefabSavables.Exists(x => x.PrefabGuid == guid))
            {
                guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            }

            return guid;
        }
        
        internal void CleanupSavablePrefabs()
        {
            for (var i = prefabSavables.Count - 1; i >= 0; i--)
            {
                if (prefabSavables[i].IsUnityNull())
                {
                    prefabSavables.RemoveAt(i);
                }
            }
            
            SetDirty();
        }
        
        internal void AddSavableScriptableObject(ScriptableObject scriptableObject)
        {
            if (scriptableObjectSavables.Exists(x => (ScriptableObject)x.unityObject == scriptableObject)) return;

            var guid = GenerateScriptableObjectID(scriptableObject);
            
            scriptableObjectSavables.Add(new UnityObjectIdentification(guid, scriptableObject));
            
            SetDirty();
        }
        
        private void FixMissingScriptableObjectID()
        {
            foreach (var unityObjectIdentification in scriptableObjectSavables)
            {
                if (string.IsNullOrEmpty(unityObjectIdentification.guid))
                {
                    unityObjectIdentification.guid = GenerateScriptableObjectID((ScriptableObject)unityObjectIdentification.unityObject);
                }
            }
        }
        
        private string GenerateScriptableObjectID(ScriptableObject scriptableObject)
        {
            var guid = "ScriptableObject_" + scriptableObject.name + "_" + SaveLoadUtility.GenerateId();
            
            while (GuidExists(guid, UnityUtility.FindAllScriptableObjects<AssetRegistry>()))
            {
                guid = "ScriptableObject_" + scriptableObject.name + "_" + SaveLoadUtility.GenerateId();
            }

            return guid;
        }

        private bool GuidExists(string guid, AssetRegistry[] assetRegistries)
        {
            foreach (var assetRegistry in assetRegistries)
            {
                if (assetRegistry.ScriptableObjectSavables.Exists(x => x.guid == guid))
                {
                    return true;
                }
            }

            return false;
        }

        internal void CleanupSavableScriptableObjects()
        {
            for (var i = scriptableObjectSavables.Count - 1; i >= 0; i--)
            {
                if (scriptableObjectSavables[i].unityObject.IsUnityNull())
                {
                    scriptableObjectSavables.RemoveAt(i);
                }
            }
            
            SetDirty();
        }

        /// <summary>
        /// Changes made to serialized fields via script in Edit mode are not automatically saved.
        /// Unity only persists modifications made through the Inspector unless explicitly marked as dirty.
        /// </summary>
        private new void SetDirty()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }
    }
}
