using System.Collections.Generic;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    public class ScriptableObjectRegistry : ScriptableObject
    {
        [SerializeField] private List<ComponentsContainer> savables = new();

        public List<ComponentsContainer> Savables => savables;

        internal void AddSavableScriptableObject(ScriptableObject savable, string guid)
        {
            var savableLookup = savables.Find(x => (ScriptableObject)x.unityObject == savable);
            if (savableLookup != null)
            {
                savableLookup.guid = guid;
            }
            else
            {
                savables.Add(new ComponentsContainer(guid, savable));
            }
        }
        
        internal void RemoveSavableScriptableObject(string prefabPath)
        {
            var savableLookup = savables.Find(x => x.guid == prefabPath);
            if (savableLookup != null)
            {
                savables.Remove(savableLookup);
            }
        }
        
        internal void ChangeGuid(string oldGuid, string prefabPath)
        {
            var savableLookup = savables.Find(x => x.guid == oldGuid);
            if (savableLookup != null)
            {
                savableLookup.guid = prefabPath;
            }
        }

        public bool ContainsPrefabGuid(string prefabPath)
        {
            return savables.Find(x => x.guid == prefabPath) != null;
        }
    
        public bool TryGetScriptableObject(string guid, out ScriptableObject savable)
        {
            var savableLookup = savables.Find(x => x.guid == guid);
            if (savableLookup != null)
            {
                savable = (ScriptableObject)savableLookup.unityObject;
                return true;
            }

            savable = null;
            return false;
        }
    }
}
