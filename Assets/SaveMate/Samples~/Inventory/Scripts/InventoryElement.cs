using SaveMate.Runtime.Core.EventHandler;
using SaveMate.Runtime.Core.StateSnapshot;
using UnityEngine;
using UnityEngine.UI;

namespace SaveMate.Samples.Inventory.Scripts
{
    public class InventoryElement : MonoBehaviour, IAfterRestoreSnapshotHandler, ISaveStateHandler
    {
        [SerializeField] private Image image;
        [SerializeField] private Text itemName;
    
        public Item ContainedItem { get; private set; }

        public void Setup(Item item)
        {
            ContainedItem = item;
            image.sprite = item.sprite;
            itemName.text = item.itemName;
        }
    
        public void OnAfterRestoreSnapshot()
        {
            if (ContainedItem != null)
            {
                Setup(ContainedItem);
            }
            
        }

        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("item", ContainedItem);
        }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            if (restoreSnapshotHandler.TryLoad("item", out Item item))
            {
                ContainedItem = item;
            }
        }
    }
}
