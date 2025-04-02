using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class ScriptableObjectSaveGroup : ScriptableObject
    {
        [SerializeField] private List<string> searchInFolders = new();
        [SerializeField] private List<ScriptableObject> pathBasedScriptableObjects = new();
        [SerializeField] private List<ScriptableObject> customAddedScriptableObjects = new();

        public IEnumerable<ScriptableObject> ScriptableObjects => pathBasedScriptableObjects.Concat(customAddedScriptableObjects);

        private void OnValidate()
        {
            UpdateFolderSelectScriptableObject();
            
            UnityUtility.SetDirty(this);
        }

        private void UpdateFolderSelectScriptableObject()
        {
            var newScriptableObjects = GetScriptableObjectSavables(searchInFolders.ToArray());

            foreach (var newScriptableObject in newScriptableObjects)
            {
                if (!pathBasedScriptableObjects.Contains(newScriptableObject))
                {
                    pathBasedScriptableObjects.Add(newScriptableObject);
                }
            }

            for (var index = pathBasedScriptableObjects.Count - 1; index >= 0; index--)
            {
                var currentScriptableObject = pathBasedScriptableObjects[index];
                if (!newScriptableObjects.Contains(currentScriptableObject))
                {
                    pathBasedScriptableObjects.Remove(currentScriptableObject);
                }
            }
        }
        
        private static List<ScriptableObject> GetScriptableObjectSavables(string[] filter)
        {
            List<ScriptableObject> foundObjects = new();
            
            var guids = AssetDatabase.FindAssets("t:ScriptableObject", filter);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is ISavable)
                {
                    foundObjects.Add(asset);
                }
            }

            return foundObjects;
        }
    }
}
