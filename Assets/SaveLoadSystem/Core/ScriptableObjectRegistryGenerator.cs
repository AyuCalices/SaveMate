using System.IO;
using SaveLoadSystem.Core.Component.SavableConverter;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    
#if UNITY_EDITOR
    
    [InitializeOnLoad]
    public class ScriptableObjectRegistryGenerator : AssetPostprocessor
    {
        private static ScriptableObjectRegistry _cachedScriptableObjectRegistry;
        private static string _defaultPath;
        
        static ScriptableObjectRegistryGenerator()
        {
            EditorApplication.delayCall += LoadSavablePrefab;
        }
        
        private static void LoadSavablePrefab()
        {
            var prefabRegistry = GetDefaultPrefabObjects();
            if (prefabRegistry == null) return;
            
            ProcessAll(prefabRegistry);
            
            EditorApplication.delayCall -= LoadSavablePrefab;
        }
        
        public static void ProcessAll(ScriptableObjectRegistry scriptableObjectRegistry)
        {
            // Get all ScriptableObject asset paths
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (string guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is ISavable)
                {
                    scriptableObjectRegistry.AddSavableScriptableObject(asset, path);
                }
            }
        }
        
        //help of fishnet code
        private static ScriptableObjectRegistry GetDefaultPrefabObjects()
        {
            //If cached is null try to get it.
            if (_cachedScriptableObjectRegistry == null)
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObjectRegistry)}");
                
                if (guids.Length > 1)
                {
                    Debug.LogWarning("There are multiple Prefab Registries!");
                }

                if (guids.Length != 0)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _cachedScriptableObjectRegistry = AssetDatabase.LoadAssetAtPath<ScriptableObjectRegistry>(assetPath);
                }
            }

            if (_cachedScriptableObjectRegistry == null)
            {
                var defaultPrefabsPath = GetPlatformPath(Path.Combine("Assets", "ScriptableObject Registry.asset"));
                var fullPath = Path.GetFullPath(defaultPrefabsPath);
                Debug.Log($"Creating a new DefaultPrefabsObject at {fullPath}.");
                var directory = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(directory))
                { 
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }

                _cachedScriptableObjectRegistry = ScriptableObject.CreateInstance<ScriptableObjectRegistry>();
                AssetDatabase.CreateAsset(_cachedScriptableObjectRegistry, defaultPrefabsPath);
                AssetDatabase.SaveAssets();

                ProcessAll(_cachedScriptableObjectRegistry);
            }

            return _cachedScriptableObjectRegistry;
        }
        
        private static string GetPlatformPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            path = path.Replace(@"\"[0], Path.DirectorySeparatorChar);
            path = path.Replace(@"/"[0], Path.DirectorySeparatorChar);
            return path;
        }
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Application.isPlaying) return;
            
            /* Don't iterate if updating or compiling as that could cause an infinite loop
             * due to the prefabs being generated during an update, which causes the update
             * to start over, which causes the generator to run again, which... you get the idea. */
            if (EditorApplication.isCompiling)
                return;
            
            var prefabRegistry = GetDefaultPrefabObjects();
            if (prefabRegistry == null) return;

            foreach (var importedAsset in importedAssets)
            {
                var savablePrefab = AssetDatabase.LoadAssetAtPath<ScriptableObject>(importedAsset);
                if (savablePrefab != null && savablePrefab is ISavable)
                {
                    prefabRegistry.AddSavableScriptableObject(savablePrefab, importedAsset);
                }
            }

            foreach (var deletedAsset in deletedAssets)
            {
                prefabRegistry.RemoveSavableScriptableObject(deletedAsset);
            }

            for (var i = 0; i < movedAssets.Length; i++)
            {
                prefabRegistry.ChangeGuid(movedFromAssetPaths[i], movedAssets[i]);
            }
        }
    }
    
#endif
    
}
