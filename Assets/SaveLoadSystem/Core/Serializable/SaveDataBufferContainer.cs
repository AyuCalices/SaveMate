using System;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Serializable
{
    [Serializable]
    public class SaveDataBufferContainer
    {
        public readonly Dictionary<GuidPath, SaveDataBuffer> SaveDataBuffers;
        public readonly List<(string, string)> PrefabList;

        public SaveDataBufferContainer(Dictionary<GuidPath, SaveDataBuffer> saveDataBuffers, List<(string, string)> prefabList)
        {
            SaveDataBuffers = saveDataBuffers;
            PrefabList = prefabList;
        }
    }
}
