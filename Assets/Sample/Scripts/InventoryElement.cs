using SaveLoadSystem.Core;
using SaveLoadSystem.Core.EventHandler;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using UnityEngine;
using UnityEngine.UI;

namespace Sample.Scripts
{
    public class InventoryElement : MonoBehaviour, ISaveMateAfterLoadHandler, ISavable
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
