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
        [SerializeField] private List<string> searchInFolders = new();
        [SerializeField] private List<Savable> prefabSavables = new();
        [SerializeField] private List<UnityObjectIdentification> scriptableObjectSavables = new();

        public List<string> SearchInFolders => searchInFolders;
        public List<Savable> PrefabSavables => prefabSavables;
        public List<UnityObjectIdentification> ScriptableObjectSavables => scriptableObjectSavables;

        //needed if the user does bad id entries
        private void OnValidate()
        {
            //Prefab
            CleanupSavablePrefabs();
            
            //ScriptableObject
            CleanupSavableScriptableObjects();
            FixMissingScriptableObjectGuid();
            SavableScriptableObjectSetup.UpdateScriptableObjectGuidOnInspectorInput(this);
            
            UnityUtility.SetDirty(this);
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
        
        private void FixMissingScriptableObjectGuid()
        {
            foreach (var unityObjectIdentification in scriptableObjectSavables)
            {
                if (string.IsNullOrEmpty(unityObjectIdentification.guid))
                {
                    unityObjectIdentification.guid = SavableScriptableObjectSetup.ApplyNewUniqueGuid(unityObjectIdentification.unityObject);
                }
            }
        }

        internal void AddSavablePrefab(Savable savable)
        {
            if (prefabSavables.Exists(x => x == savable)) return;
            
            prefabSavables.Add(savable);

            UnityUtility.SetDirty(this);
        }
        
        internal void AddSavableScriptableObject(ScriptableObject scriptableObject, string guid)
        {
            if (scriptableObjectSavables.Exists(x => (ScriptableObject)x.unityObject == scriptableObject)) return;
            
            scriptableObjectSavables.Add(new UnityObjectIdentification(guid, scriptableObject));
            
            UnityUtility.SetDirty(this);
        }
        
        public void UpdateFolderSelect()
        {
            UpdateFolderSelectScriptableObject();
            UpdateFolderSelectPrefabs();
            
            UnityUtility.SetDirty(this);
        }
        
        private void UpdateFolderSelectScriptableObject()
        {
            var newScriptableObjects = SavableScriptableObjectSetup.GetScriptableObjectSavables(searchInFolders.ToArray());

            foreach (var newScriptableObject in newScriptableObjects)
            {
                if (!scriptableObjectSavables.Exists(x => x.unityObject == newScriptableObject))
                {
                    AddSavableScriptableObject(newScriptableObject, SavableScriptableObjectSetup.RequestUniqueGuid(newScriptableObject));
                }
            }

            for (var index = scriptableObjectSavables.Count - 1; index >= 0; index--)
            {
                var currentScriptableObject = scriptableObjectSavables[index];
                if (!newScriptableObjects.Contains(currentScriptableObject.unityObject))
                {
                    scriptableObjectSavables.Remove(currentScriptableObject);
                }
            }
        }

        private void UpdateFolderSelectPrefabs()
        {
            var newPrefabs = SavablePrefabSetup.GetPrefabSavables(searchInFolders.ToArray());

            foreach (var newPrefab in newPrefabs)
            {
                if (!prefabSavables.Contains(newPrefab))
                {
                    SavablePrefabSetup.SetUniquePrefabGuid(newPrefab);
                    AddSavablePrefab(newPrefab);
                }
            }

            for (var index = prefabSavables.Count - 1; index >= 0; index--)
            {
                var currentPrefab = prefabSavables[index];
                if (!newPrefabs.Contains(currentPrefab))
                {
                    prefabSavables.Remove(currentPrefab);
                }
            }
        }
    }
}
