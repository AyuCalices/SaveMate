using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [InitializeOnLoad]
    public class SavableScriptableObjectSetup : AssetPostprocessor
    {
        private static readonly Dictionary<Object, string> _savableScriptableObjectGuidLookup = new();
        
        static SavableScriptableObjectSetup()
        {
            AssetRegistryManager.OnAssetRegistriesInitialized += LoadSavables;
            AssetRegistryManager.OnAssetRegistryAdded += ProcessScriptableObjectsForAssetRegistry;
        }
        
        private static void LoadSavables(List<AssetRegistry> assetRegistries)
        {
            if (assetRegistries is { Count: > 0 })
            {
                ProcessAllScriptableObjects(assetRegistries);
            }
            
            AssetRegistryManager.OnAssetRegistriesInitialized -= LoadSavables;
        }
        
        private static void ProcessAllScriptableObjects(List<AssetRegistry> assetRegistries)
        {
            foreach (var assetRegistry in assetRegistries)
            {
                RegisterAssetRegistryScriptableObjects(assetRegistry);
            }
            
            foreach (var assetRegistry in assetRegistries)
            {
                ProcessScriptableObjectsForAssetRegistry(assetRegistry);
            }
        }
        
        private static void RegisterAssetRegistryScriptableObjects(AssetRegistry assetRegistry)
        {
            foreach (var assetRegistryScriptableObjectSavable in assetRegistry.ScriptableObjectSavables)
            {
                if (_savableScriptableObjectGuidLookup.TryGetValue(assetRegistryScriptableObjectSavable.unityObject, out string guid))
                {
                    if (assetRegistryScriptableObjectSavable.guid != guid)
                    {
                        Debug.LogWarning("[Internal Error] Different guid's for one Savable Scriptable Objects detected!");
                    }
                }
                    
                _savableScriptableObjectGuidLookup.TryAdd(assetRegistryScriptableObjectSavable.unityObject, assetRegistryScriptableObjectSavable.guid);
            }
        }

        private static void ProcessScriptableObjectsForAssetRegistry(AssetRegistry assetRegistry)
        {
            if (assetRegistry == null) return;

            var guids = AssetDatabase.FindAssets("t:ScriptableObject", assetRegistry.SearchInFolders.ToArray());

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is ISavable)
                {
                    assetRegistry.AddSavableScriptableObject(asset, RequestUniqueGuid(asset));
                }
            }
        }
        
        private static string GenerateScriptableObjectID(Object scriptableObject)
        {
            var newGuid = "ScriptableObject_" + scriptableObject.name + "_" + SaveLoadUtility.GenerateId();
            
            while (_savableScriptableObjectGuidLookup.Values.Contains(newGuid))
            {
                newGuid = "ScriptableObject_" + scriptableObject.name + "_" + SaveLoadUtility.GenerateId();
            }

            return newGuid;
        }

        internal static string RequestUniqueGuid(Object scriptableObject)
        {
            if (!_savableScriptableObjectGuidLookup.TryGetValue(scriptableObject, out var guid))
            {
                var newGuid = GenerateScriptableObjectID(scriptableObject);
                _savableScriptableObjectGuidLookup.TryAdd(scriptableObject, newGuid);
                return newGuid;
            }

            return guid;
        }
        
        internal static List<ScriptableObject> GetScriptableObjectSavables(string[] filter)
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

        internal static void UpdateScriptableObjectGuidOnInspectorInput(AssetRegistry updatedAssetRegistry)
        {
            foreach (var updatedObjectGuidContainer in updatedAssetRegistry.ScriptableObjectSavables)
            {
                var duplicatedNames = new List<string>();
                foreach (var keyValuePair in _savableScriptableObjectGuidLookup)
                {
                    if (keyValuePair.Value == updatedObjectGuidContainer.guid)
                    {
                        duplicatedNames.Add($"'{keyValuePair.Key.name}'");
                    }
                }

                var combinedNames = string.Join(" | ", duplicatedNames);
                
                if (duplicatedNames.Count > 1)
                {
                    Debug.LogError($"Duplicate ID '{updatedObjectGuidContainer.guid}' detected in multiple Scriptable Objects within the Asset Registries: {combinedNames}. " +
                                   "Each ScriptableObject reference must have a unique GUID. Please ensure all references are distinct.");
                    continue;
                }
                

                //only change every occurence of the guid for the asset, if there actually is a change
                if (_savableScriptableObjectGuidLookup.TryGetValue(updatedObjectGuidContainer.unityObject, out var storedGuid) &&  storedGuid != updatedObjectGuidContainer.guid)
                {
                    foreach (var cachedAssetRegistry in AssetRegistryManager.CachedAssetRegistries)
                    {
                        //skip the registry with the change
                        if (cachedAssetRegistry == updatedAssetRegistry) continue;
                        
                        foreach (var cachedObjectGuidContainer in cachedAssetRegistry.ScriptableObjectSavables)
                        {
                            if (cachedObjectGuidContainer.unityObject == updatedObjectGuidContainer.unityObject)
                            {
                                cachedObjectGuidContainer.guid = updatedObjectGuidContainer.guid;
                            }
                        }
                    }
                    
                    _savableScriptableObjectGuidLookup[updatedObjectGuidContainer.unityObject] = updatedObjectGuidContainer.guid;
                }
            }
        }
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var assetRegistries = AssetRegistryManager.CachedAssetRegistries;
            if (assetRegistries is { Count: > 0 })
            {
                PostprocessScriptableObjects(assetRegistries, importedAssets);
            }
        }
        
        private static void PostprocessScriptableObjects(List<AssetRegistry> assetRegistries, string[] importedAssets)
        {
            foreach (var assetRegistry in assetRegistries)
            {
                if (assetRegistry.IsUnityNull()) continue;
                
                assetRegistry.CleanupSavableScriptableObjects();
                UnityUtility.SetDirty(assetRegistry);
            }

            foreach (var importedAsset in importedAssets)
            {
                foreach (var assetRegistry in assetRegistries)
                {
                    if (assetRegistry.IsUnityNull()) continue;
                    
                    //TODO: this check must work!
                    if (!assetRegistry.SearchInFolders.Exists(x => importedAsset.StartsWith(x))) continue;
                    
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(importedAsset);
                    if (asset != null && asset is ISavable)
                    {
                        assetRegistry.AddSavableScriptableObject(asset, RequestUniqueGuid(asset));
                    }
                }
            }
        }
    }
}
