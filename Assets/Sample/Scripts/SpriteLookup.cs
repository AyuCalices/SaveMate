using System.Collections.Generic;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.UnityComponent.SavableConverter;
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
