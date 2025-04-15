using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector2Converter : BaseConverter<Vector2>
    {
        protected override void OnCaptureState(Vector2 input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("x", input.x);
            createSnapshotHandler.Save("y", input.y);
        }

        protected override Vector2 OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Vector2();
        }

        protected override void OnRestoreState(Vector2 input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("x", out input.x);
            restoreSnapshotHandler.TryLoad("y", out input.y);
        }
    }
}
