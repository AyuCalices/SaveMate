using System.Collections.Generic;
using SaveMate.Core.SaveComponents.GameObjectScope.StateSnapshot;
using SaveMate.Core.StateSnapshot.Interface;
using SaveMate.Core.StateSnapshot.SnapshotHandler;
using UnityEngine;

namespace Sample.Scripts
{
    public class InventoryView : MonoBehaviour, ISaveStateHandler
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private InventoryElement inventoryElementPrefab;
        [SerializeField] private Transform instantiationParent;

        private List<InventoryElement> _instantiatedInventoryElements;

        private void Awake()
        {
            _instantiatedInventoryElements = new List<InventoryElement>();
            inventory.OnItemAdded += InstantiateItem;
            inventory.OnItemRemoved += DestroyItem;
        }

        private void OnDestroy()
        {
            inventory.OnItemAdded -= InstantiateItem;
            inventory.OnItemRemoved -= DestroyItem;
        }

        private void InstantiateItem(Item addedItem)
        {
            var inventoryElement = Instantiate(inventoryElementPrefab, instantiationParent);
            inventoryElement.Setup(addedItem);
            _instantiatedInventoryElements.Add(inventoryElement);
        }
    
        private void DestroyItem(Item removedItem)
        {
            var foundInventoryElement = _instantiatedInventoryElements.Find(x => x.ContainedItem.Equals(removedItem));
            _instantiatedInventoryElements.Remove(foundInventoryElement);
            Destroy(foundInventoryElement.gameObject);
        }
    
        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("content", _instantiatedInventoryElements);
        }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("content", out _instantiatedInventoryElements);
        }
    }
}
