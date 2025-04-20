using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SaveMate.Runtime.Core.DataTransferObject;

namespace SaveMate.Runtime.Utility.NewtonsoftJson
{
    public class SaveDataInstanceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // We are converting a Dictionary<GuidPath, SaveDataInstance>
            return objectType == typeof(Dictionary<GuidPath, LeafSaveData>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Cast the value to Dictionary<GuidPath, SaveDataInstance>
            var saveDataInstanceLookup = (Dictionary<GuidPath, LeafSaveData>)value;

            // Serialize the dictionary as a list of GuidSaveDataInstance objects
            var saveInstances = saveDataInstanceLookup?
                .Select(kvp => new GuidLeafSaveData
                {
                    OriginGuid = kvp.Key,
                    References = kvp.Value.References,
                    Values = kvp.Value.Values
                })
                .ToList() ?? new List<GuidLeafSaveData>();

            // Write the list of GuidSaveDataInstance to the JSON output
            serializer.Serialize(writer, saveInstances);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Deserialize the JSON to a list of GuidSaveDataInstance
            var saveInstances = serializer.Deserialize<List<GuidLeafSaveData>>(reader);

            // Convert the list of GuidSaveDataInstance back into a dictionary
            var saveDataInstanceLookup = saveInstances
                .ToDictionary(x => x.OriginGuid, x => (LeafSaveData)x);

            return saveDataInstanceLookup;
        }
    }
}
