using JetBrains.Annotations;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Converter;

namespace Sample.Scripts
{
    [UsedImplicitly]
    public class ItemConverter : BaseConverter<Item>
    {
        protected override void OnSave(Item input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("sprite", input.sprite);
            saveDataHandler.Save("name", input.itemName);
        }

        protected override Item OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            return new Item();
        }

        protected override void OnLoad(Item input, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("sprite", out input.sprite);
            loadDataHandler.TryLoad("name", out input.itemName);
        }
    }
}
