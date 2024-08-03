using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveLoadSystem.Core.Component
{
    public class PrefabRegistry : ScriptableObject
    {
        [SerializeField] private List<ComponentsContainer> savables = new();

        public List<ComponentsContainer> Savables => savables;

        internal void AddSavablePrefab(Savable savable, string guid)
        {
            var savableLookup = savables.Find(x => (Savable)x.unityObject == savable);
            if (savableLookup != null)
            {
                savableLookup.guid = guid;
            }
            else
            {
                savables.Add(new ComponentsContainer(guid, savable));
            }
            
            savable.SetPrefabPath(guid);
        }
        
        internal void RemoveSavablePrefab(string prefabPath)
        {
            var savableLookup = savables.Find(x => x.guid == prefabPath);
            if (savableLookup != null)
            {
                ((Savable)savableLookup.unityObject).SetPrefabPath(string.Empty);
                savables.Remove(savableLookup);
            }
        }
        
        internal void ChangeGuid(string oldGuid, string prefabPath)
        {
            var savableLookup = savables.Find(x => x.guid == oldGuid);
            if (savableLookup != null)
            {
                ((Savable)savableLookup.unityObject).SetPrefabPath(prefabPath);
                savableLookup.guid = prefabPath;
            }
        }

        public bool ContainsPrefabGuid(string prefabPath)
        {
            return savables.Find(x => x.guid == prefabPath) != null;
        }
    
        public bool TryGetPrefab(string guid, out Savable savable)
        {
            var savableLookup = savables.Find(x => x.guid == guid);
            if (savableLookup != null)
            {
                savable = ((Savable)savableLookup.unityObject);
                return true;
            }

            savable = null;
            return false;
        }
    }
}
