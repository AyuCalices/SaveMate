using JetBrains.Annotations;
using UnityEngine;

namespace SaveMate.Runtime.Core.StateSnapshot.Converter.UnityTypes
{
    [UsedImplicitly]
    internal class Vector4Converter : BaseSaveMateConverter<Vector4>
    {
        protected override void OnCaptureState(Vector4 input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("x", input.x);
            createSnapshotHandler.Save("y", input.y);
            createSnapshotHandler.Save("z", input.z);
            createSnapshotHandler.Save("w", input.w);
        }

        protected override Vector4 OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Vector4();
        }

        protected override void OnRestoreState(Vector4 input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("x", out input.x);
            restoreSnapshotHandler.TryLoad("y", out input.y);
            restoreSnapshotHandler.TryLoad("z", out input.z);
            restoreSnapshotHandler.TryLoad("w", out input.w);
        }
    }
}
