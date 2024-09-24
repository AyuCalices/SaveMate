using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Converter;
using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

//Optional Type-Converter implementation approach
public class ItemConverter : BaseConverter<Item>
{
    protected override void OnSave(Item data, SaveDataHandler saveDataHandler)
    {
        saveDataHandler.SaveAsValue("name", data.itemName);
        saveDataHandler.TrySaveAsReferencable("item", data.sprite);
    }

    public override object OnLoad(LoadDataHandler loadDataHandler)
    {
        var name = loadDataHandler.LoadValue<string>("name");
        loadDataHandler.TryLoadReferencable("item", out GuidPath guidPath);

        var item = new Item
        {
            itemName = name
        };
        
        loadDataHandler.EnqueueReferenceBuilding(guidPath, foundObject =>
        {
            item.sprite = (Sprite)foundObject;
        });

        return item;
    }
}
