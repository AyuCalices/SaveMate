using System;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.SerializableTypes
{
    [Serializable]
    public class SaveMetaData
    {
        public SaveVersion SaveVersion;
        public DateTime ModificationDate;
        public string Checksum;
        public Dictionary<string, object> CustomData = new();
    }
}
