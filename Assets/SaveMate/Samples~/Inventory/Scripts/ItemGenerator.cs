using UnityEngine;

namespace SaveMate.Samples.Inventory.Scripts
{
    [CreateAssetMenu]
    public class ItemGenerator : ScriptableObject
    {
        public SpriteLookup spriteLookup;
        public Sprite sprite;
        public string itemName;
    
        public Item GenerateItem()
        {
            return new Item(spriteLookup, sprite, itemName);
        }
    }
}
