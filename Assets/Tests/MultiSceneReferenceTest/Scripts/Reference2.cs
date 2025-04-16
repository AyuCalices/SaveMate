using System.Collections.Generic;
using Sample.Scripts;
using SaveMate.Core.StateSnapshot;
using UnityEngine;

namespace Tests.MultiSceneReferenceTest.Scripts
{
    public class Reference2 : Singleton<Reference2>, ISaveStateHandler
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private List<ItemGenerator> spawnableItems;

        public Item storedItem;
        public Item otherItem;
    
        [ContextMenu("Setup")]
        public void Setup()
        {
            inventory.RemoveItem(storedItem);
            storedItem = SpawnRandomItem();
            inventory.AddItem(storedItem);
            Reference1.Instance.otherItem = storedItem;
        }
    
        [ContextMenu("Check Equality")]
        public void CheckEquality()
        {
            bool thisMatch = ReferenceEquals(storedItem, Reference1.Instance.otherItem);
            bool otherMatch = ReferenceEquals(otherItem, Reference1.Instance.storedItem);
        
            Debug.Log("ThisMatch: " + thisMatch + " | OtherMatch " + otherMatch);
        }

        private Item SpawnRandomItem()
        {
            int randomItemGenerator = Random.Range(0, spawnableItems.Count);
            return spawnableItems[randomItemGenerator].GenerateItem();
        }

        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("StoredItem", storedItem);
            createSnapshotHandler.Save("OtherItem", otherItem);
        }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            if (restoreSnapshotHandler.TryLoad("StoredItem", out Item storedItem))
            {
                this.storedItem = storedItem;
            }
        
            if (restoreSnapshotHandler.TryLoad("OtherItem", out Item otherItem))
            {
                this.otherItem = otherItem;
            }
        }
    }
}
