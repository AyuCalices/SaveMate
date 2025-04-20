using JetBrains.Annotations;
using SaveMate.Runtime.Core.StateSnapshot;

namespace SaveMate.Samples.Inventory.Scripts
{
    [UsedImplicitly]
    public class ItemConverter : BaseSaveMateConverter<Item>
    {
        protected override void OnCaptureState(Item input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("sprite", input.sprite);
            createSnapshotHandler.Save("name", input.itemName);
        }

        protected override Item OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Item();
        }

        protected override void OnRestoreState(Item input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("sprite", out input.sprite);
            restoreSnapshotHandler.TryLoad("name", out input.itemName);
        }
    }
}
