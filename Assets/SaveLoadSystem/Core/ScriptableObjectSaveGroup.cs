using System.Collections.Generic;
using SaveLoadSystem.Core.DataTransferObject;
using SaveLoadSystem.Core.EventHandler;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class ScriptableObjectSaveGroupElement : ScriptableObject, ISavableGroupHandler, ILoadableGroupHandler
    {
        [SerializeField] private List<string> searchInFolders = new();
        [SerializeField] private List<ScriptableObject> pathBasedScriptableObjects = new();
        [SerializeField] private List<ScriptableObject> customAddedScriptableObjects = new();
        
        public string SceneName => RootSaveData.ScriptableObjectDataName;
        
        private static readonly HashSet<ScriptableObject> _savedScriptableObjectsLookup = new();

        
        #region Editor Behaviour

        
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

        
        #endregion

        #region Interface Implementation: ISavableGroupHandler
        
        
        void ISavableGroupHandler.OnBeforeCaptureSnapshot()
        {
            foreach (var pathBasedScriptableObject in pathBasedScriptableObjects)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (pathBasedScriptableObject is IBeforeCaptureSnapshotHandler handler)
                {
                    handler.OnBeforeCaptureSnapshot();
                }
            }

            foreach (var customAddedScriptableObject in customAddedScriptableObjects)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (customAddedScriptableObject is IBeforeCaptureSnapshotHandler handler)
                {
                    handler.OnBeforeCaptureSnapshot();
                }
            }
        }
        

        void ISavableGroupHandler.CaptureSnapshot(SaveLoadManager saveLoadManager)
        {
            foreach (var pathBasedScriptableObject in pathBasedScriptableObjects)
            {
                CaptureScriptableObjectSnapshot(saveLoadManager, pathBasedScriptableObject);
            }

            foreach (var customAddedScriptableObject in customAddedScriptableObjects)
            {
                CaptureScriptableObjectSnapshot(saveLoadManager, customAddedScriptableObject);
            }
        }
        
        void ISavableGroupHandler.OnAfterCaptureSnapshot()
        {
            _savedScriptableObjectsLookup.Clear();
            
            foreach (var pathBasedScriptableObject in pathBasedScriptableObjects)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (pathBasedScriptableObject is IAfterCaptureSnapshotHandler handler)
                {
                    handler.OnAfterCaptureSnapshot();
                }
            }

            foreach (var customAddedScriptableObject in customAddedScriptableObjects)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (customAddedScriptableObject is IAfterCaptureSnapshotHandler handler)
                {
                    handler.OnAfterCaptureSnapshot();
                }
            }
        }
        
        
        #endregion

        #region Private: ISavableGroupHandler

        
        private void CaptureScriptableObjectSnapshot(SaveLoadManager saveLoadManager, ScriptableObject scriptableObject)
        {
            //make sure scriptable objects are only saved once each snapshot
            if (!_savedScriptableObjectsLookup.Add(scriptableObject)) return;

            if (!saveLoadManager.ScriptableObjectToGuidLookup.TryGetValue(scriptableObject, out var guidPath))
            {
                //TODO: error handling
                return;
            }
            
            var saveLink = saveLoadManager.CurrentSaveFileContext;
            
            if (!TypeUtility.TryConvertTo(scriptableObject, out ISavable targetSavable)) return;
                
            var leafSaveData = new LeafSaveData();
            var branchSaveData = saveLink.RootSaveData.ScriptableObjectSaveData;
            branchSaveData.UpsertLeafSaveData(guidPath, leafSaveData);

            targetSavable.OnSave(new SaveDataHandler(branchSaveData, leafSaveData, guidPath, RootSaveData.ScriptableObjectDataName, 
                saveLink, saveLoadManager));
                
            saveLink.SoftLoadedObjects.Remove(scriptableObject);
        }

        
        #endregion

        #region #region Interface Implementation: ILoadableGroupHandler

        void ILoadableGroupHandler.OnBeforeRestoreSnapshot()
        {
            foreach (var pathBasedScriptableObject in pathBasedScriptableObjects)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (pathBasedScriptableObject is IBeforeRestoreSnapshotHandler handler)
                {
                    handler.OnBeforeRestoreSnapshot();
                }
            }

            foreach (var customAddedScriptableObject in customAddedScriptableObjects)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (customAddedScriptableObject is IBeforeRestoreSnapshotHandler handler)
                {
                    handler.OnBeforeRestoreSnapshot();
                }
            }
        }
        
        void ILoadableGroupHandler.OnPrepareSnapshotObjects(SaveLoadManager saveLoadManager, LoadType loadType) { }

        void ILoadableGroupHandler.RestoreSnapshot(SaveLoadManager saveLoadManager, LoadType loadType)
        {
            foreach (var pathBasedScriptableObject in pathBasedScriptableObjects)
            {
                RestoreScriptableObjectSnapshot(saveLoadManager, loadType, pathBasedScriptableObject);
            }

            foreach (var customAddedScriptableObject in customAddedScriptableObjects)
            {
                RestoreScriptableObjectSnapshot(saveLoadManager, loadType, customAddedScriptableObject);
            }
        }
        
        void ILoadableGroupHandler.OnAfterRestoreSnapshot()
        {
            foreach (var pathBasedScriptableObject in pathBasedScriptableObjects)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (pathBasedScriptableObject is IAfterRestoreSnapshotHandler handler)
                {
                    handler.OnAfterRestoreSnapshot();
                }
            }

            foreach (var customAddedScriptableObject in customAddedScriptableObjects)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (customAddedScriptableObject is IAfterRestoreSnapshotHandler handler)
                {
                    handler.OnAfterRestoreSnapshot();
                }
            }
        }
        
        
        #endregion
        
        #region Private: ILoadableGroupHandler

        private void RestoreScriptableObjectSnapshot(SaveLoadManager saveLoadManager, LoadType loadType, ScriptableObject scriptableObject)
        {
            var saveContext = saveLoadManager.CurrentSaveFileContext;

            //return if it cant be loaded due to soft loading
            if (loadType != LoadType.Hard && saveContext.SoftLoadedObjects.Contains(scriptableObject)) return;

            if (!saveLoadManager.ScriptableObjectToGuidLookup.TryGetValue(scriptableObject, out var guidPath))
            {
                //TODO: error handling
                return;
            }
            
            var rootSaveData = saveContext.RootSaveData;
            
            // Skip the scriptable object, if it contains references to scene's, that arent active
            if (rootSaveData.ScriptableObjectSaveData.Elements.TryGetValue(guidPath, out var leafSaveData))
            {
                if (!ScenesForGlobalLeafSaveDataAreLoaded(saveLoadManager.GetTrackedSaveSceneManagers(), leafSaveData))
                {
                    Debug.LogWarning($"Skipped ScriptableObject '{scriptableObject.name}' for saving, because of a scene requirement. ScriptableObject GUID: '{guidPath.ToString()}'");
                    return;
                }
            }

            //restore snapshot data
            if (!rootSaveData.ScriptableObjectSaveData.TryGetLeafSaveData(guidPath, out var instanceSaveData)) return;
            
            if (!TypeUtility.TryConvertTo(scriptableObject, out ISavable targetSavable)) return;
                    
            var loadDataHandler = new LoadDataHandler(rootSaveData, instanceSaveData, loadType, RootSaveData.ScriptableObjectDataName, 
                saveContext, saveLoadManager);
            
            targetSavable.OnLoad(loadDataHandler);
                    
            saveContext.SoftLoadedObjects.Add(scriptableObject);
        }
        
        private bool ScenesForGlobalLeafSaveDataAreLoaded(List<SimpleSceneSaveManager> requiredScenes, LeafSaveData leafSaveData)
        {
            foreach (var referenceGuidPath in leafSaveData.References.Values)
            {
                if (!requiredScenes.Exists(x => x.SceneName == referenceGuidPath.SceneName) && 
                    referenceGuidPath.SceneName != RootSaveData.ScriptableObjectDataName) return false;
            }

            return true;
        }

        
        #endregion
    }
}
