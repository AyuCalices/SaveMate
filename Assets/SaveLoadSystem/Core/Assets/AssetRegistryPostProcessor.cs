using System;
using System.Collections.Generic;
using SaveLoadSystem.Utility;
using UnityEditor;

namespace SaveLoadSystem.Core
{
#if UNITY_EDITOR
    
    [InitializeOnLoad]
    public class AssetRegistryPostProcessor : AssetPostprocessor
    {
        public static event Action<List<AssetRegistry>> OnAssetRegistriesInitialized;
        public static event Action<AssetRegistry> OnAssetRegistryAdded;
        public static event Action OnAssetRegistryRemoved;
        
        public static List<AssetRegistry> CachedAssetRegistries { get; private set; } = new();
        
        
        static AssetRegistryPostProcessor()
        {
            EditorApplication.delayCall += LoadSavables;
        }

        
        private static void LoadSavables()
        {
            InitializeAssetRegistries();
            OnAssetRegistriesInitialized?.Invoke(CachedAssetRegistries);
            EditorApplication.delayCall -= LoadSavables;
        }

        //help of fishnet code
        private static void InitializeAssetRegistries()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(AssetRegistry)}");

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var assetRegistry = AssetDatabase.LoadAssetAtPath<AssetRegistry>(assetPath);
                
                if (!CachedAssetRegistries.Contains(assetRegistry))
                {
                    CachedAssetRegistries.Add(assetRegistry);
                }
            }
        }
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            
            /* Don't iterate if updating or compiling as that could cause an infinite loop
             * due to the prefabs being generated during an update, which causes the update
             * to start over, which causes the generator to run again, which... you get the idea. */
            if (EditorApplication.isCompiling)
                return;
            
            
            if (RemoveDeletedAssetRegistries(CachedAssetRegistries))
            {
                OnAssetRegistryRemoved?.Invoke();
            }
            
            AddNewAssetRegistries(CachedAssetRegistries, importedAssets);
        }

        private static bool RemoveDeletedAssetRegistries(List<AssetRegistry> assetRegistries)
        {
            if (assetRegistries == null) return false;
            
            var assetRegistryChanged = false;
            
            for (var index = assetRegistries.Count - 1; index >= 0; index--)
            {
                if (assetRegistries[index].IsUnityNull())
                {
                    assetRegistries.RemoveAt(index);
                    assetRegistryChanged = true;
                }
            }

            return assetRegistryChanged;
        }

        private static void AddNewAssetRegistries(List<AssetRegistry> assetRegistries, string[] importedAssets)
        {
            foreach (var importedAsset in importedAssets)
            {
                var newAssetRegistry = AssetDatabase.LoadAssetAtPath<AssetRegistry>(importedAsset);
                
                if (newAssetRegistry != null)
                {
                    assetRegistries.Add(newAssetRegistry);
                    OnAssetRegistryAdded?.Invoke(newAssetRegistry);
                }
            }
        }
    }
    
#endif
}
