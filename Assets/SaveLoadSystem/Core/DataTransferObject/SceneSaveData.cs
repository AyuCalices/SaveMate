using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class SceneSaveData
    {
        [JsonProperty] public readonly List<SavablePrefabElement> SavePrefabs;
        
        [JsonIgnore] public Dictionary<GuidPath, InstanceSaveData> SaveInstancesLookup;
        [JsonProperty] private List<GuidInstanceSaveData> SaveInstances
        {
            get => SaveInstancesLookup
                .Select(kvp => new GuidInstanceSaveData(kvp.Key.TargetGuid)
                {
                    References = kvp.Value.References,
                    Values = kvp.Value.Values
                })
                .ToList();
            set
            {
                SaveInstancesLookup = value?.ToDictionary(x => new GuidPath(x.OriginGuid), x => (InstanceSaveData)x) 
                                         ?? new Dictionary<GuidPath, InstanceSaveData>();;
            }
        }
        
        public SceneSaveData(Dictionary<GuidPath, InstanceSaveData> saveInstancesLookup, List<SavablePrefabElement> savePrefabs)
        {
            SavePrefabs = savePrefabs;
            SaveInstancesLookup = saveInstancesLookup;
        }
    }

    public class SavablePrefabElement
    {
        public readonly string PrefabGuid;
        public readonly string SceneGuid;

        public SavablePrefabElement(string prefabGuid, string sceneGuid)
        {
            PrefabGuid = prefabGuid;
            SceneGuid = sceneGuid;
        }
    }

    public class GuidInstanceSaveData : InstanceSaveData
    {
        public readonly string[] OriginGuid;

        public GuidInstanceSaveData(string[] originGuid)
        {
            OriginGuid = originGuid;
        }
    }
}
