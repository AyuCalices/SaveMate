using System.Collections.Generic;
using SaveLoadSystem.Core.UnityComponent;
using SaveLoadSystem.Utility;
using UnityEditor;

namespace SaveLoadSystem.Core
{
    [InitializeOnLoad]
    public class SavablePrefabSetup : AssetPostprocessor
    {
        static SavablePrefabSetup()
        {
            AssetRegistryManager.OnAssetRegistriesInitialized += LoadSavables;
            AssetRegistryManager.OnAssetRegistryAdded += ProcessPrefabsForAssetRegistry;
        }
        
        private static void LoadSavables(List<AssetRegistry> assetRegistries)
        {
            if (assetRegistries is { Count: > 0 })
            {
                ProcessAllPrefabs(assetRegistries);
            }
            
            AssetRegistryManager.OnAssetRegistriesInitialized -= LoadSavables;
        }
        
        private static void ProcessAllPrefabs(List<AssetRegistry> assetRegistries)
        {
            foreach (var registry in assetRegistries)
            {
                ProcessPrefabsForAssetRegistry(registry); 
            }
        }

        private static void ProcessPrefabsForAssetRegistry(AssetRegistry assetRegistry)
        {
            if (assetRegistry == null) return;

            var guids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var savablePrefab = AssetDatabase.LoadAssetAtPath<Savable>(assetPath);

                if (savablePrefab != null)
                {
                    assetRegistry.AddSavablePrefab(savablePrefab);
                }
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var assetRegistries = AssetRegistryManager.CachedAssetRegistries;
            if (assetRegistries is { Count: > 0 })
            {
                CleanupSavablePrefabs(assetRegistries);
                PostprocessPrefabs(assetRegistries, importedAssets);
                assetRegistries.ForEach(UnityUtility.SetDirty);
            }
        }
        
        private static void CleanupSavablePrefabs(List<AssetRegistry> assetRegistries)
        {
            foreach (var assetRegistry in assetRegistries)
            {
                if (assetRegistry.IsUnityNull()) continue;
                
                for (var i = assetRegistry.PrefabSavables.Count - 1; i >= 0; i--)
                {
                    if (assetRegistry.PrefabSavables[i].IsUnityNull())
                    {
                        assetRegistry.PrefabSavables.RemoveAt(i);
                    }
                }
            }
        }
        
        private static void PostprocessPrefabs(List<AssetRegistry> assetRegistries, string[] importedAssets)
        {
            foreach (var importedAsset in importedAssets)
            {
                foreach (var assetRegistry in assetRegistries)
                {
                    if (assetRegistry.IsUnityNull()) continue;
                    
                    var savablePrefab = AssetDatabase.LoadAssetAtPath<Savable>(importedAsset);
                    if (savablePrefab)
                    {
                        assetRegistry.AddSavablePrefab(savablePrefab);
                    }
                }
            }
        }
    }
}
