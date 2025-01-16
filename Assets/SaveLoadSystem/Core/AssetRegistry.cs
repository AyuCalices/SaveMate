using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.Component;
using UnityEngine;
using UnityEngine.Serialization;

namespace SaveLoadSystem.Core
{
    [CreateAssetMenu]
    public class AssetRegistry : ScriptableObject
    {
        [SerializeField] private PrefabRegistry prefabRegistry;
        [SerializeField] private ScriptableObjectRegistry scriptableObjectRegistry;
        [SerializeField] private List<ComponentsContainer> unityObjectList = new();

        public PrefabRegistry PrefabRegistry => prefabRegistry;
        public ScriptableObjectRegistry ScriptableObjectRegistry => scriptableObjectRegistry;

        public IEnumerable<ComponentsContainer> GetCombinedEnumerable()
        {
            return unityObjectList.Concat(prefabRegistry.Savables).Concat(scriptableObjectRegistry.Savables);
        }
    }
}
