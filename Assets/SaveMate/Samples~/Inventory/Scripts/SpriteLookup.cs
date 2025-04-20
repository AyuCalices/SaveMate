using System.Collections.Generic;
using SaveMate.Runtime.Core.StateSnapshot;
using UnityEngine;

namespace SaveMate.Samples.Inventory.Scripts
{
    [CreateAssetMenu(fileName = "SpriteLookup", menuName = "Sample/Inventory/SpriteLookup")]
    public class SpriteLookup : ScriptableObject, ISaveStateHandler
    {
        [SerializeField] private List<Sprite> sprites = new();

        public List<Sprite> Sprites => sprites;
        
        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler) { }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler) { }
    }
}
