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
        public Dictionary<string, object> DefinedSaveData;
        public Dictionary<string, object> CustomSaveData;

        public SaveDataBuffer(Component.SaveStrategy saveStrategy, GuidPath creatorGuidPath, Type savableType)
        {
            this.saveStrategy = saveStrategy;
            originGuidPath = creatorGuidPath;
            this.savableType = savableType.AssemblyQualifiedName;
        }

        public void SetDefinedSaveData(Dictionary<string, object> definedSaveData)
        {
            DefinedSaveData = definedSaveData;
        }

        public void SetCustomSaveData(Dictionary<string, object> customSaveData)
        {
            CustomSaveData = customSaveData;
        }
    }
}
