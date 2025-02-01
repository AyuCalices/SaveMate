using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class InstanceSaveData
    {
        private Dictionary<string, GuidPath> _referenceSaveData;
        public Dictionary<string, GuidPath> ReferenceSaveData
        {
            get 
            { 
                _referenceSaveData ??= new Dictionary<string, GuidPath>();
                return _referenceSaveData;
            }
            set => _referenceSaveData = value;
        }
        
        private JObject _valueSaveData;
        public JObject ValueSaveData
        {
            get 
            { 
                _valueSaveData ??= new JObject();
                return _valueSaveData;
            }
            set => _valueSaveData = value;
        }
    }
}
