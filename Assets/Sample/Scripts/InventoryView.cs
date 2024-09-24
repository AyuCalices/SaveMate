using System.Collections.Generic;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Attributes;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

public class InventoryView : MonoBehaviour//, ISavable
{
    [SerializeField, Savable] private Inventory inventory;
    [SerializeField] private InventoryElement inventoryElementPrefab;
    [SerializeField] private Transform instantiationParent;

    [Savable] private List<InventoryElement> _instantiatedInventoryElements;

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
    
    //Optional Component-Saving implementation approach
    public void OnSave(SaveDataHandler saveDataHandler)
    {
        saveDataHandler.TrySaveAsReferencable("content", _instantiatedInventoryElements);
        saveDataHandler.TrySaveAsReferencable("inventory", inventory);
    }

    //Optional Component-Saving implementation approach
    public void OnLoad(LoadDataHandler loadDataHandler)
    {
        loadDataHandler.TryLoadReferencable("content", out GuidPath contentGuidPath);
        loadDataHandler.TryLoadReferencable("inventory", out GuidPath inventoryGuidPath);
        
        loadDataHandler.EnqueueReferenceBuilding(new[]{contentGuidPath, inventoryGuidPath}, foundObject =>
        {
            _instantiatedInventoryElements = (List<InventoryElement>)foundObject[0];
            inventory = (Inventory)foundObject[1];
        });
    }
}
