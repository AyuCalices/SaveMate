using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace SaveLoadSystem.Core.SerializableTypes
{
    [Serializable]
    public class SaveDataBuffer
    {
        public Component.SaveStrategy saveStrategy;
        public GuidPath originGuidPath;
        public string savableType;

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

        public SaveDataBuffer(Component.SaveStrategy saveStrategy, GuidPath creatorGuidPath, Type savableType)
        {
            this.saveStrategy = saveStrategy;
            originGuidPath = creatorGuidPath;
            this.savableType = savableType.AssemblyQualifiedName;
        }
    }
}
