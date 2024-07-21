using System;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Serializable
{
    [Serializable]
    public class DataBufferContainer
    {
        public readonly Dictionary<GuidPath, DataBuffer> DataBuffers;
        public readonly List<(string, string)> PrefabList;

        public DataBufferContainer(Dictionary<GuidPath, DataBuffer> dataBuffers, List<(string, string)> prefabList)
        {
            DataBuffers = dataBuffers;
            PrefabList = prefabList;
        }
    }
}
