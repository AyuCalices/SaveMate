using System.Collections.Generic;
using SaveMate.Core.SaveComponents.GameObjectScope.StateSnapshot;
using SaveMate.Core.StateSnapshot.Interface;
using SaveMate.Core.StateSnapshot.SnapshotHandler;
using UnityEngine;

namespace Sample.Scripts
{
    [CreateAssetMenu]
    public class SpriteLookup : ScriptableObject, ISaveStateHandler
    {
        [SerializeField] private List<Sprite> sprites = new();

        public List<Sprite> Sprites => sprites;
        
        public void OnCaptureState(CreateSnapshotHandler createSnapshotHandler) { }

        public void OnRestoreState(RestoreSnapshotHandler restoreSnapshotHandler) { }
    }
}
