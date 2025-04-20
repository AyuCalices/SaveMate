using JetBrains.Annotations;
using UnityEngine;

namespace SaveMate.Runtime.Core.StateSnapshot.Converter.UnityTypes
{
    [UsedImplicitly]
    internal class Vector3Converter : BaseSaveMateConverter<Vector3>
    {
        protected override void OnCaptureState(Vector3 input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("x", input.x);
            createSnapshotHandler.Save("y", input.y);
            createSnapshotHandler.Save("z", input.z);
        }

        protected override Vector3 OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Vector3();
        }

        protected override void OnRestoreState(Vector3 input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("x", out input.x);
            restoreSnapshotHandler.TryLoad("y", out input.y);
            restoreSnapshotHandler.TryLoad("z", out input.z);
        }
    }
}
