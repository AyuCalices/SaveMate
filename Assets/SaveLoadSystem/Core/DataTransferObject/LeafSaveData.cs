using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class LeafSaveData
    {
        private Dictionary<string, GuidPath> _references;
        public Dictionary<string, GuidPath> References
        {
            get 
            { 
                _references ??= new Dictionary<string, GuidPath>();
                return _references;
            }
            set => _references = value;
        }
        
        private JObject _values;
        public JObject Values
        {
            get 
            { 
                _values ??= new JObject();
                return _values;
            }
            set => _values = value;
        }
    }
}
