using System;
using SaveMate.Core.StateSnapshot;
using UnityEngine;

namespace Sample.Scripts
{
    [Serializable]
    public class Item : ISaveStateHandler
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

        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("spriteLookup", spriteLookup);
            createSnapshotHandler.Save("sprite", sprite.name);
            createSnapshotHandler.Save("itemName", itemName);
        }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("spriteLookup", out spriteLookup);

            if (restoreSnapshotHandler.TryLoad("sprite", out string spriteName))
            {
                sprite = spriteLookup.Sprites.Find(x => x.name == spriteName);
            }
            
            restoreSnapshotHandler.TryLoad("itemName", out itemName);
        }
    }
}
