using System;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Attributes;
using SaveLoadSystem.Core.Component.SavableConverter;
using SaveLoadSystem.Core.DataTransferObject;
using UnityEngine;

[Serializable, SavableObject]
public class Item// : ISavable
{
    public Sprite sprite;
    public string itemName;

    public Item() {}
    
    public Item(Sprite sprite, string itemName)
    {
        this.sprite = sprite;
        this.itemName = itemName;
    }

    //Optional Component-Saving implementation approach
    public void OnSave(SaveDataHandler saveDataHandler)
    {
        saveDataHandler.TrySaveAsReferencable("sprite", sprite);
        saveDataHandler.SaveAsValue("itemName", itemName);
    }

    //Optional Component-Saving implementation approach
    public void OnLoad(LoadDataHandler loadDataHandler)
    {
        if (!loadDataHandler.TryLoadReferencable("sprite", out GuidPath guidPath)) return;

        itemName = loadDataHandler.LoadValue<string>("itemName");
        loadDataHandler.EnqueueReferenceBuilding(guidPath, foundObject =>
        {
            sprite = (Sprite)foundObject;
        });
    }
}
