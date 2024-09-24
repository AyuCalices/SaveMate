using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class SceneDataContainer
    {
        [JsonProperty] public readonly List<(string, string)> PrefabList;
        
        [JsonIgnore] public Dictionary<GuidPath, SaveDataBuffer> SaveObjectLookup;
        [JsonProperty] private List<KeyValuePair<GuidPath, SaveDataBuffer>> SaveObjectList
        {
            get => SaveObjectLookup.ToList();
            set
            {
                SaveObjectLookup = value.ToDictionary(x => x.Key, x => x.Value);
            }
        }
        
        public SceneDataContainer(Dictionary<GuidPath, SaveDataBuffer> saveObjectLookup, List<(string, string)> prefabList)
        {
            PrefabList = prefabList;
            SaveObjectLookup = saveObjectLookup;
        }
    }
}
