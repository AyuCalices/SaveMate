using System.Collections.Generic;
using SaveLoadSystem.Core.UnityComponent;
using SaveLoadSystem.Utility;
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

        internal void AddSavablePrefab(Savable savable)
        {
            if (string.IsNullOrEmpty(savable.PrefabGuid))
            {
                var guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
                while (prefabSavables.Exists(x => x.PrefabGuid == guid))
                {
                    guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
                }
                
                savable.PrefabGuid = guid;
            }

            if (!prefabSavables.Exists(x => x == savable))
            {
                prefabSavables.Add(savable);
            }
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
        }

        public bool ContainsPrefabGuid(string prefabPath)
        {
            return prefabSavables.Find(x => x.PrefabGuid == prefabPath) != null;
        }
    
        public bool TryGetPrefab(string guid, out Savable match)
        {
            var savable = prefabSavables.Find(x => x.PrefabGuid == guid);
            if (savable != null)
            {
                match = savable;
                return true;
            }

            match = null;
            return false;
        }
        
        internal void AddSavableScriptableObject(ScriptableObject scriptableObject)
        {
            if (scriptableObjectSavables.Exists(x => (ScriptableObject)x.unityObject == scriptableObject)) return;

            var guid = "ScriptableObject_" + scriptableObject.name + "_" + SaveLoadUtility.GenerateId();
            while (scriptableObjectSavables.Exists(x => x.guid == guid))
            {
                guid = "ScriptableObject_" + scriptableObject.name + "_" + SaveLoadUtility.GenerateId();
            }
            
            scriptableObjectSavables.Add(new UnityObjectIdentification(guid, scriptableObject));
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
        }
    }
}
