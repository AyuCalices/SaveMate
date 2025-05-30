using System;
using System.Collections.Generic;
using System.Linq;
using SaveMate.Runtime.Core.SavableGroupInterfaces;
using SaveMate.Runtime.Core.SaveComponents.AssetScope;
using SaveMate.Runtime.Core.SaveComponents.ManagingScope;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SaveMate.Runtime.Core.SaveComponents.SceneScope
{
    public class SceneSaveManager : SimpleSceneSaveManager, INestableSaveGroupHandler, INestableLoadGroupHandler
    {
        [SerializeField] private SaveMateManager saveMateManager;
        [SerializeField] private LoadType defaultLoadType;
        
        [Header("Save and Load Link")]
        [SerializeField] private ScriptableObjectSaveGroup scriptableObjectsToSnapshot;
        [SerializeField] private bool additionallySnapshotDontDestroyOnLoad;

        [Header("Unity Lifecycle Events")] 
        [SerializeField] private SaveSceneManagerStartType onStartAction;
        [SerializeField] private SaveSceneManagerDestroyType onDestroyAction;
        [SerializeField] private bool saveActiveScenesOnApplicationQuit;
        
        [Header("OnCaptureState Events")]
        [SerializeField] private SceneManagerEvents sceneManagerEvents;
        
        //snapshot and loading
        private static bool _hasSavedActiveScenesThisFrame;
        
        
        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            
            saveMateManager.RegisterSaveSceneManager(this);
        }

        private void Start()
        {
            switch (onStartAction)
            {
                case SaveSceneManagerStartType.None:
                    break;
                case SaveSceneManagerStartType.RestoreSnapshotSingleScene:
                    RestoreSceneSnapshot();
                    break;
                case SaveSceneManagerStartType.RestoreSnapshotActiveScenes:
                    saveMateManager.RestoreSnapshotActiveScenes();
                    break;
                case SaveSceneManagerStartType.LoadSingleScene:
                    LoadScene();
                    break;
                case SaveSceneManagerStartType.LoadActiveScenes:
                    saveMateManager.LoadActiveScenes();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            
            saveMateManager?.RegisterSaveSceneManager(this);
        }
#endif
        
        protected override void Update()
        {
            base.Update();
            
            if (_hasSavedActiveScenesThisFrame)
            {
                _hasSavedActiveScenesThisFrame = false;
            }
        }
        
        private void OnDestroy()
        {
            switch (onDestroyAction)
            {
                case SaveSceneManagerDestroyType.CreateSnapshotSingleScene:
                    CaptureSceneSnapshot();
                    break;
                case SaveSceneManagerDestroyType.CreateSnapshotActiveScenes:
                    saveMateManager.CaptureSnapshotActiveScenes();
                    break;
                case SaveSceneManagerDestroyType.SaveSingleScene:
                    if (!_hasSavedActiveScenesThisFrame)
                    {
                        saveMateManager.Save(this);
                        _hasSavedActiveScenesThisFrame = true;
                    }
                    
                    break;
                case SaveSceneManagerDestroyType.SaveActiveScenes:
                    if (!_hasSavedActiveScenesThisFrame)
                    {
                        saveMateManager.SaveActiveScenes();
                        _hasSavedActiveScenesThisFrame = true;
                    }
                    break;
                case SaveSceneManagerDestroyType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            saveMateManager.UnregisterSaveSceneManager(this);
        }

        private void OnApplicationQuit()
        {
            if (saveActiveScenesOnApplicationQuit && !_hasSavedActiveScenesThisFrame)
            {
                saveMateManager.SaveActiveScenes();
                _hasSavedActiveScenesThisFrame = true;
            }
        }

        #endregion

        #region Interface Implementation: INestableGroupHandler

        
        List<ISavableGroupHandler> INestableSaveGroupHandler.GetSavableGroupHandlers()
        {
            var captureSnapshotGroupElements = new List<ISavableGroupHandler>();
            
            if (scriptableObjectsToSnapshot)
            {
                captureSnapshotGroupElements.Add(scriptableObjectsToSnapshot);
            }
            
            if (additionallySnapshotDontDestroyOnLoad)
            {
                captureSnapshotGroupElements.Add(SaveMateManager.GetDontDestroyOnLoadSceneManager());
            }
            
            return captureSnapshotGroupElements;
        }

        List<ILoadableGroupHandler> INestableLoadGroupHandler.GetLoadableGroupHandlers()
        {
            var restoreSnapshotGroupElements = new List<ILoadableGroupHandler>();
            
            if (scriptableObjectsToSnapshot)
            {
                restoreSnapshotGroupElements.Add(scriptableObjectsToSnapshot);
            }
            
            if (additionallySnapshotDontDestroyOnLoad)
            {
                restoreSnapshotGroupElements.Add(SaveMateManager.GetDontDestroyOnLoadSceneManager());
            }
            
            return restoreSnapshotGroupElements;
        }
        
        
        #endregion
        
        #region SaveLoad Methods

        
        [ContextMenu("Capture Snapshot")]
        public void CaptureSceneSnapshot()
        {
            saveMateManager.CaptureSnapshot(this);
        }

        [ContextMenu("Write To Disk")]
        public void WriteToDisk()
        {
            saveMateManager.WriteToDisk();
        }
        
        [ContextMenu("Save")]
        public void SaveScene()
        {
            saveMateManager.Save(this);
        }

        [ContextMenu("Restore Snapshot")]
        public void RestoreSceneSnapshot()
        {
            saveMateManager.RestoreSnapshot(defaultLoadType, this);
        }
        
        public void RestoreSceneSnapshot(LoadType loadType)
        {
            saveMateManager.RestoreSnapshot(loadType, this);
        }
        
        [ContextMenu("Load Scene")]
        public void LoadScene()
        {
            saveMateManager.Load(defaultLoadType, this);
        }
        
        public void LoadScene(LoadType loadType)
        {
            saveMateManager.Load(loadType, this);
        }
        
        [ContextMenu("Delete Snapshot Data")]
        public void DeleteSnapshotData()
        {
            saveMateManager.DeleteSnapshotData(SceneName);
        }
        
        [ContextMenu("Delete Scene Disk Data")]
        public void DeleteDiskData()
        {
            saveMateManager.DeleteDiskData(SceneName);
        }

        [ContextMenu("Reload Scene")]
        public async void ReloadScene()
        {
            await saveMateManager.ReloadScenes(this);
        }
        
        [ContextMenu("Unload Scene")]
        public void UnloadSceneAsync()
        {
            SceneManager.UnloadSceneAsync(SceneName);
        }
        
        
        #endregion
        
        #region Action Registration

        
        public void RegisterAction(UnityAction action, SceneManagerEventType firstEventType, params SceneManagerEventType[] additionalEventTypes)
        {
            foreach (var selectionViewEventType in additionalEventTypes.Append(firstEventType))
            {
                switch (selectionViewEventType)
                {
                    case SceneManagerEventType.BeforeSnapshot:
                        sceneManagerEvents.onBeforeCaptureSnapshot.AddListener(action);
                        break;
                    case SceneManagerEventType.AfterSnapshot:
                        sceneManagerEvents.onAfterCaptureSnapshot.AddListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeWriteToDisk:
                        sceneManagerEvents.onBeforeWriteToDisk.AddListener(action);
                        break;
                    case SceneManagerEventType.AfterWriteToDisk:
                        sceneManagerEvents.onAfterWriteToDisk.AddListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeReadFromDisk:
                        sceneManagerEvents.onBeforeReadFromDisk.AddListener(action);
                        break;
                    case SceneManagerEventType.AfterReadFromDisk:
                        sceneManagerEvents.onAfterReadFromDisk.AddListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeRestoreSnapshot:
                        sceneManagerEvents.onBeforeRestoreSnapshot.AddListener(action);
                        break;
                    case SceneManagerEventType.AfterRestoreSnpashot:
                        sceneManagerEvents.onAfterRestoreSnapshot.AddListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeDeleteSnapshotData:
                        sceneManagerEvents.onBeforeDeleteSnapshotData.AddListener(action);
                        break;
                    case SceneManagerEventType.AfterDeleteSnapshotData:
                        sceneManagerEvents.onAfterDeleteSnapshotData.AddListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeDeleteDiskData:
                        sceneManagerEvents.onBeforeDeleteDiskData.AddListener(action);
                        break;
                    case SceneManagerEventType.AfterDeleteDiskData:
                        sceneManagerEvents.onAfterDeleteDiskData.AddListener(action);
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        public void UnregisterAction(UnityAction action, SceneManagerEventType firstEventType, params SceneManagerEventType[] additionalEventTypes)
        {
            foreach (var selectionViewEventType in additionalEventTypes.Append(firstEventType))
            {
                switch (selectionViewEventType)
                {
                    case SceneManagerEventType.BeforeSnapshot:
                        sceneManagerEvents.onBeforeCaptureSnapshot.RemoveListener(action);
                        break;
                    case SceneManagerEventType.AfterSnapshot:
                        sceneManagerEvents.onAfterCaptureSnapshot.RemoveListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeWriteToDisk:
                        sceneManagerEvents.onBeforeWriteToDisk.RemoveListener(action);
                        break;
                    case SceneManagerEventType.AfterWriteToDisk:
                        sceneManagerEvents.onAfterWriteToDisk.RemoveListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeReadFromDisk:
                        sceneManagerEvents.onBeforeReadFromDisk.RemoveListener(action);
                        break;
                    case SceneManagerEventType.AfterReadFromDisk:
                        sceneManagerEvents.onAfterReadFromDisk.RemoveListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeRestoreSnapshot:
                        sceneManagerEvents.onBeforeRestoreSnapshot.RemoveListener(action);
                        break;
                    case SceneManagerEventType.AfterRestoreSnpashot:
                        sceneManagerEvents.onAfterRestoreSnapshot.RemoveListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeDeleteSnapshotData:
                        sceneManagerEvents.onBeforeDeleteSnapshotData.RemoveListener(action);
                        break;
                    case SceneManagerEventType.AfterDeleteSnapshotData:
                        sceneManagerEvents.onAfterDeleteSnapshotData.RemoveListener(action);
                        break;
                    
                    case SceneManagerEventType.BeforeDeleteDiskData:
                        sceneManagerEvents.onBeforeDeleteDiskData.RemoveListener(action);
                        break;
                    case SceneManagerEventType.AfterDeleteDiskData:
                        sceneManagerEvents.onAfterDeleteDiskData.RemoveListener(action);
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        

        #endregion
        
        #region Event Inheritance

        
        protected override void InternalOnBeforeCaptureSnapshot()
        {
            sceneManagerEvents.onBeforeCaptureSnapshot.Invoke();
        }
        
        protected override void InternalOnAfterCaptureSnapshot()
        {
            sceneManagerEvents.onAfterCaptureSnapshot.Invoke();
        }
        
        protected override void InternalOnBeforeWriteToDisk()
        {
            sceneManagerEvents.onBeforeWriteToDisk.Invoke();
        }
        
        protected override void InternalOnAfterWriteToDisk()
        {
            sceneManagerEvents.onAfterWriteToDisk.Invoke();
        }
        
        protected override void InternalOnBeforeReadFromDisk()
        {
            sceneManagerEvents.onBeforeReadFromDisk.Invoke();
        }
        
        protected override void InternalOnAfterReadFromDisk()
        {
            sceneManagerEvents.onAfterReadFromDisk.Invoke();
        }
        
        protected override void InternalOnBeforeRestoreSnapshot()
        {
            sceneManagerEvents.onBeforeRestoreSnapshot.Invoke();
        }
        
        protected override void InternalOnAfterRestoreSnapshot()
        {
            sceneManagerEvents.onAfterRestoreSnapshot.Invoke();
        }
        
        protected override void InternalOnBeforeDeleteSnapshotData()
        {
            sceneManagerEvents.onBeforeDeleteSnapshotData.Invoke();
        }
        
        protected override void InternalOnAfterDeleteSnapshotData()
        {
            sceneManagerEvents.onAfterDeleteSnapshotData.Invoke();
        }
        
        protected override void InternalOnBeforeDeleteDiskData()
        {
            sceneManagerEvents.onBeforeDeleteDiskData.Invoke();
        }
        
        protected override void InternalOnAfterDeleteDiskData()
        {
            sceneManagerEvents.onAfterDeleteDiskData.Invoke();
        }

        
        #endregion
    }
}
