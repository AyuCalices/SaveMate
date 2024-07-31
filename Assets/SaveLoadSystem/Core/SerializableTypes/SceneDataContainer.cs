using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;

namespace SaveLoadSystem.Core.SerializableTypes
{
    [Serializable]
    public class SceneDataContainer
    {
        public readonly List<(string, string)> PrefabList;
        
        [JsonIgnore] public Dictionary<GuidPath, SaveDataBuffer> SaveDataBuffers;
        [JsonProperty] private List<KeyValuePair<GuidPath, SaveDataBuffer>> SerializedLocations
        {
            get => SaveDataBuffers.ToList();
            set
            {
                SaveDataBuffers = value.ToDictionary(x => x.Key, x => x.Value);
            }
        }
        
        public SceneDataContainer(Dictionary<GuidPath, SaveDataBuffer> saveDataBuffers, List<(string, string)> prefabList)
        {
            PrefabList = prefabList;
            SaveDataBuffers = saveDataBuffers;
        }
    }
}
