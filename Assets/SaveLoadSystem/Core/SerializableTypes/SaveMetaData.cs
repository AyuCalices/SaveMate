using System;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace SaveLoadSystem.Core.SerializableTypes
{
    [Serializable]
    public class SaveMetaData
    {
        public SaveVersion SaveVersion;
        public DateTime ModificationDate;
        public string Checksum;
        public JObject CustomData = new();
    }
}
