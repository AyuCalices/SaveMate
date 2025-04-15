using System;
using System.Collections.Generic;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
using UnityEngine;

namespace Sample.Scripts
{
    [CreateAssetMenu]
    public class Inventory : ScriptableObject, ISaveStateHandler
    {
        [SerializeField] private List<Item> items;

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

        public void RemoveItem(Item item)
        {
            if (!items.Contains(item))
            {
                return;
            }

            items.Remove(item);
            OnItemRemoved?.Invoke(item);
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
    
        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler)
        {
            items ??= new List<Item>();
            createSnapshotHandler.Save("items", items);
        }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("items", out items);
        }
    }
}

