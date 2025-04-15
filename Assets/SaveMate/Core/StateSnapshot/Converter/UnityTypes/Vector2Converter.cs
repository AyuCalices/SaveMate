using JetBrains.Annotations;
using UnityEngine;

namespace SaveMate.Core.StateSnapshot.Converter.UnityTypes
{
    [UsedImplicitly]
    internal class Vector2Converter : BaseSaveMateConverter<Vector2>
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
