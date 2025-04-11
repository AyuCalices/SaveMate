using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.UnityComponent
{
    public class SceneSaveManager : SimpleSceneSaveManager, INestableSaveGroupHandler, INestableLoadGroupHandler
    {
        [SerializeField] private SaveLoadManager saveLoadManager;
        [SerializeField] private LoadType defaultLoadType;
        
        [Header("Save and Load Link")]
        [SerializeField] private ScriptableObjectSaveGroupElement scriptableObjectsToSave;
        [SerializeField] private bool additionallySaveDontDestroyOnLoad;

        [Header("Unity Lifecycle Events")] 
        [SerializeField] private bool loadSceneOnEnable;
        [SerializeField] private SaveSceneManagerDestroyType saveSceneOnDisable;
        [SerializeField] private bool saveActiveScenesOnApplicationQuit;
        
        [Header("Save Events")]
        [SerializeField] private SceneManagerEvents sceneManagerEvents;
        
        //snapshot and loading
        private static bool _hasSavedActiveScenesThisFrame;
        
        
        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            
            saveLoadManager.RegisterSaveSceneManager(this);
        }
        
        private void OnEnable()
        {
            if (loadSceneOnEnable)
            {
                LoadScene();
            }
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            
            saveLoadManager?.RegisterSaveSceneManager(this);
        }
        
        protected override void Update()
        {
            base.Update();
            
            if (_hasSavedActiveScenesThisFrame)
            {
                _hasSavedActiveScenesThisFrame = false;
            }
        }

        private void OnDisable()
        {
            switch (saveSceneOnDisable)
            {
                case SaveSceneManagerDestroyType.SnapshotSingleScene:
                    CaptureSceneSnapshot();
                    break;
                case SaveSceneManagerDestroyType.SnapshotActiveScenes:
                    saveLoadManager.CaptureSnapshotActiveScenes();
                    break;
                case SaveSceneManagerDestroyType.SaveSingleScene:
                    saveLoadManager.Save(this);
                    break;
                case SaveSceneManagerDestroyType.SaveActiveScenes:
                    if (!_hasSavedActiveScenesThisFrame)
                    {
                        saveLoadManager.SaveActiveScenes();
                        _hasSavedActiveScenesThisFrame = true;
                    }
                    break;
                case SaveSceneManagerDestroyType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void OnDestroy()
        {
            saveLoadManager.UnregisterSaveSceneManager(this);
        }

        private void OnApplicationQuit()
        {
            if (saveActiveScenesOnApplicationQuit && !_hasSavedActiveScenesThisFrame)
            {
                saveLoadManager.SaveActiveScenes();
                _hasSavedActiveScenesThisFrame = true;
            }
        }

        #endregion

        #region Interface Implementation: INestableGroupHandler

        
        List<ISavableGroupHandler> INestableSaveGroupHandler.GetSavableGroupHandlers()
        {
            var captureSnapshotGroupElements = new List<ISavableGroupHandler>();
            
            if (scriptableObjectsToSave)
            {
                captureSnapshotGroupElements.Add(scriptableObjectsToSave);
            }
            
            if (additionallySaveDontDestroyOnLoad)
            {
                captureSnapshotGroupElements.Add(SaveLoadManager.GetDontDestroyOnLoadSceneManager());
            }
            
            return captureSnapshotGroupElements;
        }

        List<ILoadableGroupHandler> INestableLoadGroupHandler.GetLoadableGroupHandlers()
        {
            var restoreSnapshotGroupElements = new List<ILoadableGroupHandler>();
            
            if (scriptableObjectsToSave)
            {
                restoreSnapshotGroupElements.Add(scriptableObjectsToSave);
            }
            
            if (additionallySaveDontDestroyOnLoad)
            {
                restoreSnapshotGroupElements.Add(SaveLoadManager.GetDontDestroyOnLoadSceneManager());
            }
            
            return restoreSnapshotGroupElements;
        }
        
        
        #endregion
        
        #region SaveLoad Methods

        
        [ContextMenu("Capture Scene Snapshot")]
        public void CaptureSceneSnapshot()
        {
            saveLoadManager.CaptureSnapshot(this);
        }

        [ContextMenu("Write To Disk")]
        public void WriteToDisk()
        {
            saveLoadManager.WriteToDisk();
        }
        
        [ContextMenu("Save Scene")]
        public void SaveScene()
        {
            saveLoadManager.Save(this);
        }

        [ContextMenu("Restore Scene Snapshot")]
        public void RestoreSceneSnapshot()
        {
            saveLoadManager.RestoreSnapshot(defaultLoadType, this);
        }
        
        public void RestoreSceneSnapshot(LoadType loadType)
        {
            saveLoadManager.RestoreSnapshot(loadType, this);
        }
        
        [ContextMenu("Load Scene")]
        public void LoadScene()
        {
            saveLoadManager.Load(defaultLoadType, this);
        }
        
        public void LoadScene(LoadType loadType)
        {
            saveLoadManager.Load(loadType, this);
        }
        
        [ContextMenu("Delete Scene Snapshot Data")]
        public void DeleteSceneSnapshotData()
        {
            saveLoadManager.DeleteSnapshotData(SceneName);
        }
        
        [ContextMenu("Delete Scene Disk Data")]
        public void DeleteSceneDiskData()
        {
            saveLoadManager.DeleteDiskData(SceneName);
        }

        [ContextMenu("Reload Scene")]
        public void ReloadScene()
        {
            saveLoadManager.ReloadScenes(this);
        }
        
        [ContextMenu("UnloadScene")]
        public void UnloadSceneAsync()
        {
            SceneManager.UnloadSceneAsync(SceneName);
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
        
        #region Private Classes
        
        
        [Serializable]
        private class SceneManagerEvents
        {
            public UnityEvent onBeforeCaptureSnapshot;
            public UnityEvent onAfterCaptureSnapshot;
            
            public UnityEvent onBeforeWriteToDisk;
            public UnityEvent onAfterWriteToDisk;

            public UnityEvent onBeforeReadFromDisk;
            public UnityEvent onAfterReadFromDisk;
            
            public UnityEvent onBeforeRestoreSnapshot;
            public UnityEvent onAfterRestoreSnapshot;
            
            public UnityEvent onBeforeDeleteSnapshotData;
            public UnityEvent onAfterDeleteSnapshotData;
            
            public UnityEvent onBeforeDeleteDiskData;
            public UnityEvent onAfterDeleteDiskData;
        }
        
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
    }
}
