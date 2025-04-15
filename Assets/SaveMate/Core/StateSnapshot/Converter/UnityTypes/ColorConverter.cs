using JetBrains.Annotations;
using SaveMate.Core.StateSnapshot.SnapshotHandler;
using UnityEngine;

namespace SaveMate.Core.StateSnapshot.Converter.UnityTypes
{
    [UsedImplicitly]
    internal class ColorConverter : BaseSaveMateConverter<Color>
    {
        protected override void OnCaptureState(Color input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("r", input.r);
            createSnapshotHandler.Save("g", input.g);
            createSnapshotHandler.Save("b", input.b);
            createSnapshotHandler.Save("a", input.a);
        }

        protected override Color OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Color();
        }

        protected override void OnRestoreState(Color input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("r", out input.r);
            restoreSnapshotHandler.TryLoad("g", out input.g);
            restoreSnapshotHandler.TryLoad("b", out input.b);
            restoreSnapshotHandler.TryLoad("a", out input.a);
        }
    }
}
