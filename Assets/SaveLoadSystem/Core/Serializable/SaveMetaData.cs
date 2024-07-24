using System;

namespace SaveLoadSystem.Core.Serializable
{
    [Serializable]
    public class SaveMetaData
    {
        public SaveVersion SaveVersion;
        public DateTime modificationDate;
        public float playtime;
        public string checksum;
    }
}
