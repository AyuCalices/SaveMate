using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class SaveDataBuffer
    {
        [JsonProperty] public readonly Component.SaveStrategy SaveStrategy;
        [JsonProperty] public readonly string SavableType;

        //save data should only be initialized, when needed
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
        
        private JObject _serializableSaveData;
        public JObject SerializableSaveData
        {
            get 
            { 
                _serializableSaveData ??= new();
                return _serializableSaveData;
            }
            set => _serializableSaveData = value;
        }
        
        private Dictionary<string, GuidPath> _customGuidPathSaveData;
        public Dictionary<string, GuidPath> CustomGuidPathSaveData
        {
            get 
            { 
                _customGuidPathSaveData ??= new();
                return _customGuidPathSaveData;
            }
            set => _customGuidPathSaveData = value;
        }
        
        private JObject _customSerializableSaveData;
        public JObject CustomSerializableSaveData
        {
            get 
            { 
                _customSerializableSaveData ??= new();
                return _customSerializableSaveData;
            }
            set => _customSerializableSaveData = value;
        }

        public SaveDataBuffer(Component.SaveStrategy saveStrategy, Type savableType)
        {
            SaveStrategy = saveStrategy;
            SavableType = savableType.AssemblyQualifiedName;
        }
    }
}
