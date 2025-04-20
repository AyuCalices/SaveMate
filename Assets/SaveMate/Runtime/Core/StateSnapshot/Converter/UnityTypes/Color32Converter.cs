using JetBrains.Annotations;
using UnityEngine;

namespace SaveMate.Runtime.Core.StateSnapshot.Converter.UnityTypes
{
    [UsedImplicitly]
    internal class Color32Converter : BaseSaveMateConverter<Color32>
    {
        protected override void OnCaptureState(Color32 input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("r", input.r);
            createSnapshotHandler.Save("g", input.g);
            createSnapshotHandler.Save("b", input.b);
            createSnapshotHandler.Save("a", input.a);
        }

        protected override Color32 OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Color32();
        }

        protected override void OnRestoreState(Color32 input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("r", out input.r);
            restoreSnapshotHandler.TryLoad("g", out input.g);
            restoreSnapshotHandler.TryLoad("b", out input.b);
            restoreSnapshotHandler.TryLoad("a", out input.a);
        }
    }
}
