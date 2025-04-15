using JetBrains.Annotations;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Converter;

namespace Sample.Scripts
{
    [UsedImplicitly]
    public class ItemConverter : BaseConverter<Item>
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
