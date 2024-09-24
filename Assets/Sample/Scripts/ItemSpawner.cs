using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField] private InputField inputField;
    [SerializeField] private Inventory inventory;
    [SerializeField] private List<ItemGenerator> spawnableItems;

    public void SpawnInput()
    {
        for (int i = 0; i < int.Parse(inputField.text); i++)
        {
            SpawnRandomItem();
        }
    }

    public void DestroyAll()
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    public void SpawnRandomItem()
    {
        int randomItemGenerator = Random.Range(0, spawnableItems.Count);
        inventory.AddItem(spawnableItems[randomItemGenerator].GenerateItem());
    }

    public void DestroyRandomItemFromInventory()
    {
        if (inventory.ItemCount == 0) return;
        
        int randomInventoryIndex = Random.Range(0, inventory.ItemCount);
        inventory.RemoveAtIndex(randomInventoryIndex);
    }
}
