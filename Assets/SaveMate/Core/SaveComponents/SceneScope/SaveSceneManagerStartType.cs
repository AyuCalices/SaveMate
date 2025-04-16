using UnityEngine;

namespace SaveMate.Core.SaveComponents.SceneScope
{
    public enum SaveSceneManagerStartType
    {
        None,
        RestoreSnapshotSingleScene,
        RestoreSnapshotActiveScenes,
        LoadSingleScene,
        LoadActiveScenes,
    }
}
