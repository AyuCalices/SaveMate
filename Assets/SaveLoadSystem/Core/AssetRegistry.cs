using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.UnityComponent;
using SaveLoadSystem.Utility;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class AssetRegistry : ScriptableObject
    {
        [SerializeField] private List<UnityObjectIdentification> prefabSavables = new();
        [SerializeField] private List<UnityObjectIdentification> scriptableObjectSavables = new();
        
        public List<UnityObjectIdentification> PrefabSavables => prefabSavables;
        public List<UnityObjectIdentification> ScriptableObjectSavables => scriptableObjectSavables;
        
        
        public IEnumerable<UnityObjectIdentification> GetSavableAssets()
        {
            return prefabSavables.Concat(scriptableObjectSavables);
        }

        internal void AddSavablePrefab(Savable savable)
        {
            if (prefabSavables.Exists(x => (Savable)x.unityObject == savable)) return;

            var guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            while (prefabSavables.Exists(x => x.guid == guid))
            {
                guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            }
            
            prefabSavables.Add(new UnityObjectIdentification(guid, savable));
            savable.SetPrefabPath(guid);
        }
        
        internal void CleanupSavablePrefabs()
        {
            for (var i = prefabSavables.Count - 1; i >= 0; i--)
            {
                if (prefabSavables[i].unityObject.IsUnityNull())
                {
                    prefabSavables.RemoveAt(i);
                }
            }
        }

        public bool ContainsPrefabGuid(string prefabPath)
        {
            return prefabSavables.Find(x => x.guid == prefabPath) != null;
        }
    
        public bool TryGetPrefab(string guid, out Savable savable)
        {
            var savableLookup = prefabSavables.Find(x => x.guid == guid);
            if (savableLookup != null)
            {
                savable = ((Savable)savableLookup.unityObject);
                return true;
            }

            savable = null;
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
