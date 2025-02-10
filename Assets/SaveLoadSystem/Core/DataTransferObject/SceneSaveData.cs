using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class SceneSaveData
    {
        [JsonProperty] public readonly List<SavablePrefabElement> SavePrefabs;
        
        [JsonIgnore] public Dictionary<GuidPath, InstanceSaveData> InstanceSaveDataLookup;
        [JsonProperty] private List<GuidInstanceSaveData> SaveInstances
        {
            get => InstanceSaveDataLookup
                .Select(kvp => new GuidInstanceSaveData(kvp.Key.TargetGuid)
                {
                    References = kvp.Value.References,
                    Values = kvp.Value.Values
                })
                .ToList();
            set
            {
                InstanceSaveDataLookup = value?.ToDictionary(x => new GuidPath(x.OriginGuid), x => (InstanceSaveData)x) 
                                      ?? new Dictionary<GuidPath, InstanceSaveData>();;
            }
        }
        
        public SceneSaveData(Dictionary<GuidPath, InstanceSaveData> instanceSaveDataLookup, List<SavablePrefabElement> savePrefabs)
        {
            SavePrefabs = savePrefabs;
            InstanceSaveDataLookup = instanceSaveDataLookup;
        }
    }
    
    public struct SavablePrefabElement
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
