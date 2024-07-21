using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveLoadSystem.Core.Component
{
    public class PrefabRegistry : ScriptableObject
    {
        [SerializeField] private List<SavableRegistryLookup> savables = new();

        public void AddSavablePrefab(Savable savable, string guid)
        {
            var savableLookup = savables.Find(x => x.savablePrefab == savable);
            if (savableLookup != null)
            {
                savableLookup.guid = guid;
            }
            else
            {
                savables.Add(new SavableRegistryLookup(savable, guid));
            }
            
            savable.SetPrefabPath(guid);
        }
        
        public void RemoveSavablePrefab(string prefabPath)
        {
            var savableLookup = savables.Find(x => x.guid == prefabPath);
            if (savableLookup != null)
            {
                savableLookup.savablePrefab.SetPrefabPath(string.Empty);
                savables.Remove(savableLookup);
            }
        }
        
        public void ChangeGuid(string oldGuid, string prefabPath)
        {
            var savableLookup = savables.Find(x => x.guid == oldGuid);
            if (savableLookup != null)
            {
                savableLookup.savablePrefab.SetPrefabPath(prefabPath);
                savableLookup.guid = prefabPath;
            }
        }

        public bool ContainsGuid(string prefabPath)
        {
            return savables.Find(x => x.guid == prefabPath) != null;
        }
    
        public bool TryGetSavable(string guid, out Savable savable)
        {
            var savableLookup = savables.Find(x => x.guid == guid);
            if (savableLookup != null)
            {
                savable = savableLookup.savablePrefab;
                return true;
            }

            savable = null;
            return false;
        }
    }

    [Serializable]
    public class SavableRegistryLookup
    {
        public Savable savablePrefab;
        public string guid;

        public SavableRegistryLookup(Savable savablePrefab, string guid)
        {
            this.savablePrefab = savablePrefab;
            this.guid = guid;
        }
    }
}
