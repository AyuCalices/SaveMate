using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class SceneSaveData
    {
        [JsonProperty] public readonly List<(string, string)> SavablePrefabList;
        
        [JsonIgnore] public Dictionary<GuidPath, InstanceSaveData> InstanceSaveDataLookup;
        [JsonProperty] private List<KeyValuePair<GuidPath, InstanceSaveData>> SaveObjectList
        {
            get => InstanceSaveDataLookup.ToList();
            set
            {
                InstanceSaveDataLookup = value.ToDictionary(x => x.Key, x => x.Value);
            }
        }
        
        public SceneSaveData(Dictionary<GuidPath, InstanceSaveData> instanceSaveDataLookup, List<(string, string)> savablePrefabList)
        {
            SavablePrefabList = savablePrefabList;
            InstanceSaveDataLookup = instanceSaveDataLookup;
        }
    }
}
