using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.UnityComponent;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [InitializeOnLoad]
    public class SavablePrefabSetup : AssetPostprocessor
    {
        private static readonly HashSet<Savable> SavablePrefabsGuidLookup = new();
        
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
                RegisterAssetRegistrySavables(registry);
            }
            
            foreach (var registry in assetRegistries)
            {
                ProcessPrefabsForAssetRegistry(registry); 
            }
        }
        
        private static void RegisterAssetRegistrySavables(AssetRegistry assetRegistry)
        {
            foreach (var prefabSavable in assetRegistry.PrefabSavables)
            {
                SavablePrefabsGuidLookup.Add(prefabSavable);
            }
        }

        private static void ProcessPrefabsForAssetRegistry(AssetRegistry assetRegistry)
        {
            if (assetRegistry == null) return;

            var guids = AssetDatabase.FindAssets("t:Prefab", assetRegistry.SearchInFolders.ToArray());

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var savablePrefab = AssetDatabase.LoadAssetAtPath<Savable>(assetPath);

                if (savablePrefab != null)
                {
                    SetUniquePrefabGuid(savablePrefab);
                    assetRegistry.AddSavablePrefab(savablePrefab);
                }
            }
        }

        private static string GenerateUniquePrefabGuid(Savable savable)
        {
            //generate guid
            var guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            
            while (SavablePrefabsGuidLookup.Any(x => x.PrefabGuid == guid))
            {
                guid = "Prefab_" + savable.gameObject.name + "_" + SaveLoadUtility.GenerateId();
            }

            return guid;
        }
        
        internal static void SetUniquePrefabGuid(Savable savable)
        {
            SavablePrefabsGuidLookup.Add(savable);
            
            if (string.IsNullOrEmpty(savable.PrefabGuid))
            {
                ApplyUniquePrefabGuid(savable);
            }
        }

        internal static void ApplyUniquePrefabGuid(Savable savable)
        {
            savable.PrefabGuid = GenerateUniquePrefabGuid(savable);
        }
        
        internal static List<Savable> GetPrefabSavables(string[] filter)
        {
            List<Savable> foundObjects = new();
            
            var guids = AssetDatabase.FindAssets("t:Prefab", filter);

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var savablePrefab = AssetDatabase.LoadAssetAtPath<Savable>(assetPath);

                if (savablePrefab != null)
                {
                    foundObjects.Add(savablePrefab);
                }
            }

            return foundObjects;
        }

        internal static void CheckUniquePrefabGuidOnInspectorInput()
        {
            var lookup = new Dictionary<string, List<string>>();
            
            foreach (var savable in SavablePrefabsGuidLookup)
            {
                if (savable.IsUnityNull() || string.IsNullOrEmpty(savable.PrefabGuid)) continue;
                
                if (!lookup.TryGetValue(savable.PrefabGuid, out var prefabNames))
                {
                    lookup.Add(savable.PrefabGuid, new List<string>{ $"'{savable.name}'" });
                }
                else
                {
                    prefabNames.Add($"'{savable.name}'");
                }
            }
            
            foreach (var (guid, prefabNames) in lookup)
            {
                if (prefabNames.Count > 1)
                {
                    var combinedNames = string.Join(" | ", prefabNames);
                    
                    Debug.LogError(
                        $"Duplicate ID '{guid}' detected in multiple Prefabs: {combinedNames}. " +
                        "Each Prefab must have a unique PrefabGuid. Please ensure all guids are distinct.");
                }
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var assetRegistries = AssetRegistryManager.CachedAssetRegistries;
            if (assetRegistries is { Count: > 0 })
            {
                PostprocessPrefabs(assetRegistries, importedAssets);
            }
        }
        
        private static void PostprocessPrefabs(List<AssetRegistry> assetRegistries, string[] importedAssets)
        {
            foreach (var assetRegistry in assetRegistries)
            {
                if (assetRegistry.IsUnityNull()) continue;
                
                assetRegistry.CleanupSavablePrefabs();
                UnityUtility.SetDirty(assetRegistry);
            }
            
            foreach (var importedAsset in importedAssets)
            {
                foreach (var assetRegistry in assetRegistries)
                {
                    if (assetRegistry.IsUnityNull()) continue;
                    
                    //TODO: this check must work!
                    if (!assetRegistry.SearchInFolders.Exists(x => importedAsset.StartsWith(x))) continue;
                    
                    var savablePrefab = AssetDatabase.LoadAssetAtPath<Savable>(importedAsset);
                    if (savablePrefab)
                    {
                        SetUniquePrefabGuid(savablePrefab);
                        assetRegistry.AddSavablePrefab(savablePrefab);
                    }
                }
            }
        }
    }
}
