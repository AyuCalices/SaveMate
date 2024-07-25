using System;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Serializable
{
    [Serializable]
    public class SaveMetaData
    {
        public SaveVersion SaveVersion;
        public DateTime ModificationDate;
        public Dictionary<string, object> CustomData = new();
        
        private string _checksum;

        public void SetChecksum(string newChecksum)
        {
            _checksum = newChecksum;
        }

        public string GetChecksum()
        {
            return _checksum;
        }
    }
}
