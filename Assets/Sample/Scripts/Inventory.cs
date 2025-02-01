using System;
using System.Collections.Generic;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using UnityEngine;

namespace Sample.Scripts
{
    [CreateAssetMenu]
    public class Inventory : ScriptableObject, ISavable
    {
        [SerializeField] private List<Item> items;
        [SerializeField] private int test = 5;

        public int ItemCount => items.Count;

        public event Action<Item> OnItemAdded;
        public event Action<Item> OnItemRemoved;

        private void OnEnable()
        {
            //simulate a build game
            items.Clear();
        }

        public void AddItem(Item item)
        {
            if (items.Contains(item))
            {
                Debug.LogWarning("Already added that item!");
                return;
            }

            items.Add(item);
            OnItemAdded?.Invoke(item);
        }

        public void RemoveAtIndex(int index)
        {
            if (index > ItemCount)
            {
                Debug.LogWarning("The item cant be removed from the inventory, because it is not contained!");
                return;
            }

            var itemToRemove = items[index];
            items.RemoveAt(index);
            OnItemRemoved?.Invoke(itemToRemove);
        }
    
        public void OnSave(SaveDataHandler saveDataHandler)
        {
            var test = new List<Item[]>();
            for (var index = 0; index < items.Count; index++)
            {
                var innerTest = new Item[items.Count];
                test.Add(innerTest);
            
                for (var i = 0; i < items.Count; i++)
                {
                    innerTest[i] = items[i];
                }
            }

            saveDataHandler.Save("items", items);
            saveDataHandler.Save("test", test);
            saveDataHandler.Save("test2", this.test);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("items", out items);
        
            loadDataHandler.TryLoad("test", out List<Item[]> test);
        
            loadDataHandler.TryLoad("test3", out this.test);
        }
    }
}

