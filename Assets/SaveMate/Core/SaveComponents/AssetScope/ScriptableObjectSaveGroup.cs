using System.Collections.Generic;
using SaveMate.Core.DataTransferObject;
using SaveMate.Core.EventHandler;
using SaveMate.Core.SavableGroupInterfaces;
using SaveMate.Core.SaveComponents.ManagingScope;
using SaveMate.Core.SaveComponents.SceneScope;
using SaveMate.Core.StateSnapshot;
using SaveMate.Utility;
using UnityEditor;
using UnityEngine;

namespace SaveMate.Core.SaveComponents.AssetScope
{
    [CreateAssetMenu(fileName = "ScriptableObjectSaveGroup", menuName = "Save Mate/Scriptable Object Save Group")]
    public class ScriptableObjectSaveGroup : ScriptableObject, ISavableGroupHandler, ILoadableGroupHandler
    {
        [SerializeField] private List<string> searchInFolders = new();
        [SerializeField] private List<ScriptableObject> pathBasedScriptableObjects = new();
        [SerializeField] private List<ScriptableObject> customAddedScriptableObjects = new();
        
        public string SceneName => SaveLoadUtility.ScriptableObjectDataName;
        
        private static readonly HashSet<ScriptableObject> _savedScriptableObjectsLookup = new();

        
        #region Editor Behaviour

        
        private void OnValidate()
        {
            UpdateFolderSelectScriptableObject();
            
            SaveLoadUtility.SetDirty(this);
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

                if (asset is ISaveStateHandler)
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
        

        void ISavableGroupHandler.CaptureSnapshot(SaveMateManager saveMateManager, SaveFileContext saveFileContext)
        {
            foreach (var pathBasedScriptableObject in pathBasedScriptableObjects)
            {
                CaptureScriptableObjectSnapshot(saveMateManager, saveFileContext, pathBasedScriptableObject);
            }

            foreach (var customAddedScriptableObject in customAddedScriptableObjects)
            {
                CaptureScriptableObjectSnapshot(saveMateManager, saveFileContext, customAddedScriptableObject);
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

        
        private void CaptureScriptableObjectSnapshot(SaveMateManager saveMateManager, SaveFileContext saveFileContext, ScriptableObject scriptableObject)
        {
            //make sure scriptable objects are only saved once each snapshot
            if (!_savedScriptableObjectsLookup.Add(scriptableObject)) return;

            if (!saveMateManager.ScriptableObjectToGuidLookup.TryGetValue(scriptableObject, out var guidPath))
            {
                //TODO: error handling
                return;
            }
            
            if (scriptableObject is not ISaveStateHandler targetSavable) return;
                
            var leafSaveData = new LeafSaveData();
            var branchSaveData = saveFileContext.RootSaveData.ScriptableObjectSaveData;
            branchSaveData.UpsertLeafSaveData(guidPath, leafSaveData);

            targetSavable.OnCaptureState(new CreateSnapshotHandler(branchSaveData, leafSaveData, guidPath, SaveLoadUtility.ScriptableObjectDataName, 
                saveFileContext, saveMateManager));
                
            saveFileContext.SoftLoadedObjects.Remove(scriptableObject);
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
        
        void ILoadableGroupHandler.OnPrepareSnapshotObjects(SaveMateManager saveMateManager, SaveFileContext saveFileContext, LoadType loadType) { }

        void ILoadableGroupHandler.RestoreSnapshot(SaveMateManager saveMateManager, SaveFileContext saveFileContext, LoadType loadType)
        {
            foreach (var pathBasedScriptableObject in pathBasedScriptableObjects)
            {
                RestoreScriptableObjectSnapshot(saveMateManager, saveFileContext, loadType, pathBasedScriptableObject);
            }

            foreach (var customAddedScriptableObject in customAddedScriptableObjects)
            {
                RestoreScriptableObjectSnapshot(saveMateManager, saveFileContext, loadType, customAddedScriptableObject);
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

        private void RestoreScriptableObjectSnapshot(SaveMateManager saveMateManager, SaveFileContext saveFileContext, 
            LoadType loadType, ScriptableObject scriptableObject)
        {
            //return if it cant be loaded due to soft loading
            if (loadType != LoadType.Hard && saveFileContext.SoftLoadedObjects.Contains(scriptableObject)) return;

            if (!saveMateManager.ScriptableObjectToGuidLookup.TryGetValue(scriptableObject, out var guidPath))
            {
                //TODO: error handling
                return;
            }
            
            var rootSaveData = saveFileContext.RootSaveData;
            
            // Skip the scriptable object, if it contains references to scene's, that arent active
            if (rootSaveData.ScriptableObjectSaveData.TryGetLeafSaveData(guidPath, out var leafSaveData))
            {
                if (!ScenesForGlobalLeafSaveDataAreLoaded(saveMateManager.GetTrackedSaveSceneManagers(), leafSaveData))
                {
                    Debug.LogWarning($"Skipped ScriptableObject '{scriptableObject.name}' for saving, because of a scene requirement. ScriptableObject GUID: '{guidPath.ToString()}'");
                    return;
                }
            }

            //restore snapshot data
            if (!rootSaveData.ScriptableObjectSaveData.TryGetLeafSaveData(guidPath, out var instanceSaveData)) return;
            
            if (scriptableObject is not ISaveStateHandler targetSavable) return;
                    
            var loadDataHandler = new RestoreSnapshotHandler(rootSaveData, instanceSaveData, loadType, guidPath, 
                saveFileContext, saveMateManager);
            
            targetSavable.OnRestoreState(loadDataHandler);
                    
            saveFileContext.SoftLoadedObjects.Add(scriptableObject);
        }
        
        private bool ScenesForGlobalLeafSaveDataAreLoaded(List<SimpleSceneSaveManager> requiredScenes, LeafSaveData leafSaveData)
        {
            foreach (var referenceGuidPath in leafSaveData.References.Values)
            {
                if (!referenceGuidPath.HasValue) return false;
                
                var sceneNameMatchExists = requiredScenes.Exists(x => x.SceneName == referenceGuidPath.Value.SceneName);
                var isNotScriptableObject = referenceGuidPath.Value.SceneName != SaveLoadUtility.ScriptableObjectDataName;
                
                if (!sceneNameMatchExists && isNotScriptableObject) return false;
            }

            return true;
        }

        
        #endregion
    }
}
