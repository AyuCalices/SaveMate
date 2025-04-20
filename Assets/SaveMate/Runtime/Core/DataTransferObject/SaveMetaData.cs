using System;
using Newtonsoft.Json.Linq;

namespace SaveMate.Runtime.Core.DataTransferObject
{
    public class SaveMetaData
    {
        public SaveVersion SaveVersion { get; set; }
        public DateTime ModificationDate { get; set; }
        public JObject CustomData { get; set; } = new();
    }
}
