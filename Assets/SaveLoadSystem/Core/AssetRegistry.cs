using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.Component;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class AssetRegistry : ScriptableObject
    {
        [SerializeField] private List<ComponentsContainer> prefabSavables = new();
        [SerializeField] private List<ComponentsContainer> scriptableObjectSavables = new();
        
        public List<ComponentsContainer> PrefabSavables => prefabSavables;
        public List<ComponentsContainer> ScriptableObjectSavables => scriptableObjectSavables;
        
        
        public IEnumerable<ComponentsContainer> GetSavableAssets()
        {
            return prefabSavables.Concat(scriptableObjectSavables);
        }
        

        internal void AddSavablePrefab(Savable savable, string guid)
        {
            var savableLookup = prefabSavables.Find(x => (Savable)x.unityObject == savable);
            if (savableLookup != null)
            {
                savableLookup.guid = guid;
            }
            else
            {
                prefabSavables.Add(new ComponentsContainer(guid, savable));
            }
            
            savable.SetPrefabPath(guid);
        }
        
        internal void RemoveSavablePrefab(string prefabPath)
        {
            var savableLookup = prefabSavables.Find(x => x.guid == prefabPath);
            if (savableLookup != null)
            {
                ((Savable)savableLookup.unityObject).SetPrefabPath(string.Empty);
                prefabSavables.Remove(savableLookup);
            }
        }
        
        internal void ChangePrefabGuid(string oldGuid, string prefabPath)
        {
            var savableLookup = prefabSavables.Find(x => x.guid == oldGuid);
            if (savableLookup != null)
            {
                ((Savable)savableLookup.unityObject).SetPrefabPath(prefabPath);
                savableLookup.guid = prefabPath;
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
        
        internal void AddSavableScriptableObject(ScriptableObject savable, string guid)
        {
            var savableLookup = scriptableObjectSavables.Find(x => (ScriptableObject)x.unityObject == savable);
            if (savableLookup != null)
            {
                savableLookup.guid = guid;
            }
            else
            {
                scriptableObjectSavables.Add(new ComponentsContainer(guid, savable));
            }
        }
        
        internal void RemoveSavableScriptableObject(string prefabPath)
        {
            var savableLookup = scriptableObjectSavables.Find(x => x.guid == prefabPath);
            if (savableLookup != null)
            {
                scriptableObjectSavables.Remove(savableLookup);
            }
        }
        
        internal void ChangeScriptableObjectGuid(string oldGuid, string prefabPath)
        {
            var savableLookup = scriptableObjectSavables.Find(x => x.guid == oldGuid);
            if (savableLookup != null)
            {
                savableLookup.guid = prefabPath;
            }
        }
    }
}
