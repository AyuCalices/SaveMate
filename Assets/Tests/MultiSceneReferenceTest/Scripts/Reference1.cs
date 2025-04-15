using System.Collections.Generic;
using Sample.Scripts;
using SaveMate.Core.SaveComponents.GameObjectScope.StateSnapshot;
using SaveMate.Core.StateSnapshot;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Tests.MultiSceneReferenceTest.Scripts
{
    public class Reference1 : Singleton<Reference1>, ISaveStateHandler
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
