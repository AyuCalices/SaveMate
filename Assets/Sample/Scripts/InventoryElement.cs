using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.EventHandler;
using UnityEngine;
using UnityEngine.UI;

namespace Sample.Scripts
{
    public class InventoryElement : MonoBehaviour, ISaveMateAfterLoadHandler, ISavable
    {
        [SerializeField] private SaveLoadManager saveLoadManager;
        [SerializeField] private Image image;
        [SerializeField] private Text itemName;
    
        public Item ContainedItem { get; private set; }

        public void Setup(Item item)
        {
            ContainedItem = item;
            image.sprite = item.sprite;
            itemName.text = item.itemName;
        }
    
        public void OnAfterLoad()
        {
            Setup(ContainedItem);
        }

        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("item", ContainedItem);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            if (loadDataHandler.TryLoad("item", out Item item))
            {
                ContainedItem = item;
            }
        }
    }
}
