using Sample.Scripts;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Converter;

//Optional Type-Converter implementation approach
public class ItemConverter : BaseConverter<Item>
{
    protected override void OnSave(Item data, SaveDataHandler saveDataHandler)
    {
        saveDataHandler.Save("name", data.itemName);
        saveDataHandler.Save("item", data.sprite);
    }

    protected override Item OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
    {
        return new Item();
    }

    protected override void OnLoad(Item data, LoadDataHandler loadDataHandler)
    {
        loadDataHandler.TryLoad("name", out data.itemName);
        loadDataHandler.TryLoad("item", out data.sprite);
    }
}
