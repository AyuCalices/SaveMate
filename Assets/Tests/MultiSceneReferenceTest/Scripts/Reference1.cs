using System;
using System.Collections.Generic;
using Sample.Scripts;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class Reference1 : Singleton<Reference1>, ISavable
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private List<ItemGenerator> spawnableItems;

    public Item storedItem;
    public Item otherItem;
    
    [ContextMenu("Setup")]
    public void Setup()
    {
        storedItem = SpawnRandomItem();
        inventory.AddItem(storedItem);
        Reference2.Instance.otherItem = storedItem;
    }
    
    [ContextMenu("Check Equality")]
    public void CheckEquality()
    {
        bool thisMatch = ReferenceEquals(storedItem, Reference2.Instance.otherItem);
        bool otherMatch = ReferenceEquals(otherItem, Reference2.Instance.storedItem);
        
        Debug.Log("ThisMatch: " + thisMatch + " | OtherMatch " + otherMatch);
    }

    private Item SpawnRandomItem()
    {
        int randomItemGenerator = Random.Range(0, spawnableItems.Count);
        return spawnableItems[randomItemGenerator].GenerateItem();
    }

    public void OnSave(SaveDataHandler saveDataHandler)
    {
        saveDataHandler.Save("StoredItem", storedItem);
        saveDataHandler.Save("OtherItem", otherItem);
    }

    public void OnLoad(LoadDataHandler loadDataHandler)
    {
        if (loadDataHandler.TryLoad("StoredItem", out Item storedItem))
        {
            this.storedItem = storedItem;
        }
        
        if (loadDataHandler.TryLoad("OtherItem", out Item otherItem))
        {
            this.otherItem = otherItem;
        }
    }
}
