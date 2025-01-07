using System.Collections.Generic;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Component.SavableConverter;
using UnityEngine;

namespace Sample.Scripts
{
    public class InventoryView : MonoBehaviour, ISavable
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
            var foundInventoryElement = _instantiatedInventoryElements.Find(x => x.ContainedItem == removedItem);
            _instantiatedInventoryElements.Remove(foundInventoryElement);
            Destroy(foundInventoryElement.gameObject);
        }
    
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("content", _instantiatedInventoryElements);
            saveDataHandler.Save("inventory", inventory);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("content", out _instantiatedInventoryElements);
            loadDataHandler.TryLoad("inventory", out inventory);
        }
    }
}
