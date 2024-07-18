using System;
using System.Collections.Generic;
using SaveLoadCore.Core.Component;

namespace SaveLoadCore.Core.Serializable
{
    [Serializable]
    public class DataBuffer
    {
        public SaveStrategy saveStrategy;
        public GuidPath originGuidPath;
        public Type SavableType;
        public Dictionary<string, object> DefinedSaveData;
        public Dictionary<string, object> CustomSaveData;

        public DataBuffer(SaveStrategy saveStrategy, GuidPath creatorGuidPath, Type savableType)
        {
            this.saveStrategy = saveStrategy;
            originGuidPath = creatorGuidPath;
            SavableType = savableType;
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
