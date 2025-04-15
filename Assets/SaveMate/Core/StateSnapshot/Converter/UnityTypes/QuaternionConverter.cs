using JetBrains.Annotations;
using SaveMate.Core.StateSnapshot.SnapshotHandler;
using UnityEngine;

namespace SaveMate.Core.StateSnapshot.Converter.UnityTypes
{
    [UsedImplicitly]
    internal class QuaternionConverter : BaseSaveMateConverter<Quaternion>
    {
        protected override void OnCaptureState(Quaternion input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("x", input.x);
            createSnapshotHandler.Save("y", input.y);
            createSnapshotHandler.Save("z", input.z);
            createSnapshotHandler.Save("w", input.w);
        }

        protected override Quaternion OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Quaternion();
        }

        protected override void OnRestoreState(Quaternion input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("x", out input.x);
            restoreSnapshotHandler.TryLoad("y", out input.y);
            restoreSnapshotHandler.TryLoad("z", out input.z);
            restoreSnapshotHandler.TryLoad("w", out input.w);
        }
    }
}
