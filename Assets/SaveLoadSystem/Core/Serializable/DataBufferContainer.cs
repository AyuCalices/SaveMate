using System;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Serializable
{
    [Serializable]
    public class DataBufferContainer
    {
        public readonly Dictionary<GuidPath, DataBuffer> DataBuffers = new();
    }
}
