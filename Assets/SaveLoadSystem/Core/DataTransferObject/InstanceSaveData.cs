using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class InstanceSaveData
    {
        private Dictionary<string, GuidPath> _guidPathSaveData;
        public Dictionary<string, GuidPath> GuidPathSaveData
        {
            get 
            { 
                _guidPathSaveData ??= new();
                return _guidPathSaveData;
            }
            set => _guidPathSaveData = value;
        }
        
        private JObject _jsonSerializableSaveData;
        public JObject JsonSerializableSaveData
        {
            get 
            { 
                _jsonSerializableSaveData ??= new();
                return _jsonSerializableSaveData;
            }
            set => _jsonSerializableSaveData = value;
        }
    }
}
