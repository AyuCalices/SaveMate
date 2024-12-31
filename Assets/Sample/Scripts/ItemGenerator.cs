using Sample.Scripts;
using UnityEngine;

[CreateAssetMenu]
public class ItemGenerator : ScriptableObject
{
    public Sprite sprite;
    public string itemName;
    
    public Item GenerateItem()
    {
        return new Item(sprite, itemName);
    }
}
