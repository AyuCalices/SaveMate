using System;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class SaveMetaData
    {
        public SaveVersion SaveVersion { get; set; }
        public DateTime ModificationDate { get; set; }
        public string Checksum { get; set; }
        public JObject CustomData { get; set; } = new();
    }
}
