using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Attributes;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;
using UnityEngine.UI;

public class InventoryElement : MonoBehaviour//, ISavable
{
    [SerializeField] private SaveLoadManager saveLoadManager;
    [SerializeField] private Image image;
    [SerializeField] private Text itemName;
    
    [Savable] public Item ContainedItem { get; private set; }

    private void Awake()
    {
        saveLoadManager.SaveFocus.OnAfterLoad += UpdateContainedItem;
    }

    private void OnDestroy()
    {
        saveLoadManager.SaveFocus.OnAfterLoad -= UpdateContainedItem;
    }

    private void UpdateContainedItem()
    {
        Setup(ContainedItem);
    }

    public void Setup(Item item)
    {
        ContainedItem = item;
        image.sprite = item.sprite;
        itemName.text = item.itemName;
    }

    //Optional Component-Saving implementation approach
    public void OnSave(SaveDataHandler saveDataHandler)
    {
        saveDataHandler.TrySaveAsReferencable("item", ContainedItem);
    }

    //Optional Component-Saving implementation approach
    public void OnLoad(LoadDataHandler loadDataHandler)
    {
        loadDataHandler.TryLoadReferencable("item", out GuidPath guidPath);
        loadDataHandler.EnqueueReferenceBuilding(guidPath, foundObject =>
        {
            ContainedItem = (Item)foundObject;
        });
    }
}
