using System;
using UnityEngine.Events;

namespace SaveLoadSystem.Core.Components.Groups
{
    [Serializable]
    internal class SceneManagerEvents
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
}
