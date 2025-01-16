using System;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Component.SavableConverter;
using UnityEngine;

namespace Sample.Scripts
{
    [Serializable]
    public class Item : ISavable
    {
        public Sprite sprite;
        public string itemName;

        public Item() {}
    
        public Item(Sprite sprite, string itemName)
        {
            this.sprite = sprite;
            this.itemName = itemName;
        }

        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("sprite", sprite);
            saveDataHandler.SaveAsValue("itemName", itemName);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("sprite", out sprite);
            loadDataHandler.TryLoadValue("itemName", out itemName);
        }
    }
}
