using System.IO;
using SaveLoadSystem.Core.Component;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [InitializeOnLoad]
    public class PrefabRegistryGenerator : AssetPostprocessor
    {
        private static PrefabRegistry _cachedPrefabRegistry;
        private static string _defaultPath;
        private static bool _subscribed;
        
        static PrefabRegistryGenerator()
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

        private static void ProcessAll(PrefabRegistry prefabRegistry)
        {
            // Get all asset paths
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

            // Iterate through all assets to find prefabs
            foreach (string assetPath in allAssetPaths)
            {
                // Check if the asset is a prefab
                if (assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Load the prefab
                    var savablePrefab = AssetDatabase.LoadAssetAtPath<Savable>(assetPath);
                    if (savablePrefab != null)
                    {
                        prefabRegistry.AddSavablePrefab(savablePrefab, assetPath);
                    }
                }
            }
        }
    
        //help of fishnet code
        private static PrefabRegistry GetDefaultPrefabObjects()
        {
            //If cached is null try to get it.
            if (_cachedPrefabRegistry == null)
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(PrefabRegistry)}");
                
                if (guids.Length > 1)
                {
                    Debug.LogWarning("There are multiple Prefab Registries!");
                }

                if (guids.Length != 0)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _cachedPrefabRegistry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(assetPath);
                }
            }

            if (_cachedPrefabRegistry == null)
            {
                var defaultPrefabsPath = GetPlatformPath(Path.Combine("Assets", "Prefab Registry.asset"));
                var fullPath = Path.GetFullPath(defaultPrefabsPath);
                Debug.Log($"Creating a new DefaultPrefabsObject at {fullPath}.");
                var directory = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(directory))
                { 
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }

                _cachedPrefabRegistry = ScriptableObject.CreateInstance<PrefabRegistry>();
                AssetDatabase.CreateAsset(_cachedPrefabRegistry, defaultPrefabsPath);
                AssetDatabase.SaveAssets();

                ProcessAll(_cachedPrefabRegistry);
            }

            return _cachedPrefabRegistry;
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
                var savablePrefab = AssetDatabase.LoadAssetAtPath<Savable>(importedAsset);
                if (savablePrefab != null)
                {
                    prefabRegistry.AddSavablePrefab(savablePrefab, importedAsset);
                }
            }

            foreach (var deletedAsset in deletedAssets)
            {
                prefabRegistry.RemoveSavablePrefab(deletedAsset);
            }

            for (var i = 0; i < movedAssets.Length; i++)
            {
                prefabRegistry.ChangeGuid(movedFromAssetPaths[i], movedAssets[i]);
            }
        }
    }
}
