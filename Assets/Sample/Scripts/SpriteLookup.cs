using System.Collections.Generic;
using SaveLoadSystem.Core;
using SaveLoadSystem.Core.Component.SavableConverter;
using UnityEngine;

namespace Sample.Scripts
{
    [CreateAssetMenu]
    public class SpriteLookup : ScriptableObject, ISavable
    {
        [SerializeField] private List<Sprite> sprites = new();

        public List<Sprite> Sprites => sprites;
        
        public void OnSave(SaveDataHandler saveDataHandler) { }

        public void OnLoad(LoadDataHandler loadDataHandler) { }
    }
}
