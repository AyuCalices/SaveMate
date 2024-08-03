using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.Component;
using UnityEngine;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class AssetRegistry : ScriptableObject
    {
        [SerializeField] private PrefabRegistry prefabLookup;
        [SerializeField] private List<ComponentsContainer> unityObjectList = new();

        public PrefabRegistry PrefabLookup => prefabLookup;

        public IEnumerable<ComponentsContainer> GetCombinedEnumerable()
        {
            return unityObjectList.Concat(prefabLookup.Savables);
        }
    }
}
