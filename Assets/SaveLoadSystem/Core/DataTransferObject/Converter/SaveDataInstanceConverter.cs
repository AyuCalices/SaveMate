using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SaveLoadSystem.Core.DataTransferObject.Converter
{
    public class SaveDataInstanceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // We are converting a Dictionary<GuidPath, SaveDataInstance>
            return objectType == typeof(Dictionary<GuidPath, SaveDataInstance>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Cast the value to Dictionary<GuidPath, SaveDataInstance>
            var saveDataInstanceLookup = (Dictionary<GuidPath, SaveDataInstance>)value;

            // Serialize the dictionary as a list of GuidSaveDataInstance objects
            var saveInstances = saveDataInstanceLookup?
                .Select(kvp => new GuidSaveDataInstance
                {
                    OriginGuid = kvp.Key.TargetGuid,
                    References = kvp.Value.References,
                    Values = kvp.Value.Values
                })
                .ToList() ?? new List<GuidSaveDataInstance>();

            // Write the list of GuidSaveDataInstance to the JSON output
            serializer.Serialize(writer, saveInstances);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Deserialize the JSON to a list of GuidSaveDataInstance
            var saveInstances = serializer.Deserialize<List<GuidSaveDataInstance>>(reader);

            // Convert the list of GuidSaveDataInstance back into a dictionary
            var saveDataInstanceLookup = saveInstances
                .ToDictionary(x => new GuidPath(x.OriginGuid), x => (SaveDataInstance)x);

            return saveDataInstanceLookup;
        }
    }
}
