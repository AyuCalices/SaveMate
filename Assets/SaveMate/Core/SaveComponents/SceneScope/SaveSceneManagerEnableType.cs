using UnityEngine;

namespace SaveMate.Core.SaveComponents.SceneScope
{
    public enum SaveSceneManagerEnableType
    {
        None,
        RestoreSnapshotSingleScene,
        RestoreSnapshotActiveScenes,
        LoadSingleScene,
        LoadActiveScenes,
    }
}
