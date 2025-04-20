using System;
using UnityEngine.Events;

namespace SaveMate.Runtime.Core.SaveComponents.SceneScope
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
