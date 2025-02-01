using System;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using UnityEngine;

namespace Sample.Scripts
{
    [Serializable]
    public class Item : ISavable
    {
        public SpriteLookup spriteLookup;
        public Sprite sprite;
        public string itemName;

        public Item() {}
    
        public Item(SpriteLookup spriteLookup, Sprite sprite, string itemName)
        {
            this.spriteLookup = spriteLookup;
            this.sprite = sprite;
            this.itemName = itemName;
        }

        public void OnSave(SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("spriteLookup", spriteLookup);
            saveDataHandler.Save("sprite", sprite.name);
            saveDataHandler.SaveAsValue("itemName", itemName);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("spriteLookup", out spriteLookup);

            if (loadDataHandler.TryLoad("sprite", out string spriteName))
            {
                sprite = spriteLookup.Sprites.Find(x => x.name == spriteName);
            }
            
            loadDataHandler.TryLoadValue("itemName", out itemName);
        }
    }
}
