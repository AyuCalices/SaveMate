using System.Collections.Generic;
using Newtonsoft.Json;
using SaveLoadSystem.Core.DataTransferObject.Converter;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class SaveDataContainer
    {
        [JsonProperty, JsonConverter(typeof(SaveDataInstanceConverter))] 
        private Dictionary<GuidPath, SaveDataInstance> _saveDataInstanceLookup = new();

        public bool TryGetInstanceSaveData(GuidPath guidPath, out SaveDataInstance saveDataInstance)
        {
            return _saveDataInstanceLookup.TryGetValue(guidPath, out saveDataInstance);
        }

        public void AddSaveData(GuidPath guidPath, SaveDataInstance saveDataInstance)
        {
            _saveDataInstanceLookup.Add(guidPath, saveDataInstance);
        }
    }
}
