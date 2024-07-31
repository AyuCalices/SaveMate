using System;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.SerializableTypes
{
    [Serializable]
    public class SaveDataBuffer
    {
        public Component.SaveStrategy saveStrategy;
        public GuidPath originGuidPath;
        public string savableType;
        public Dictionary<string, GuidPath> GuidPathSaveData;
        public Dictionary<string, object> SerializableSaveData;
        
        public Dictionary<string, GuidPath> CustomGuidPathSaveData;
        public Dictionary<string, object> CustomSerializableSaveData;

        public SaveDataBuffer(Component.SaveStrategy saveStrategy, GuidPath creatorGuidPath, Type savableType)
        {
            this.saveStrategy = saveStrategy;
            originGuidPath = creatorGuidPath;
            this.savableType = savableType.AssemblyQualifiedName;
            GuidPathSaveData = new();
            SerializableSaveData = new();
            CustomGuidPathSaveData = new();
            CustomSerializableSaveData = new();
        }
    }
}
