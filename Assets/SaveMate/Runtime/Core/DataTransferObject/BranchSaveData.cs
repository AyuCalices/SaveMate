using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SaveMate.Utility.NewtonsoftJson;

namespace SaveMate.Core.DataTransferObject
{
    public class BranchSaveData
    {
        [UsedImplicitly, JsonConverter(typeof(SaveDataInstanceConverter))] 
        public Dictionary<GuidPath, LeafSaveData> Elements { get; set; }= new();

        public bool TryGetLeafSaveData(GuidPath guidPath, out LeafSaveData leafSaveData)
        {
            return Elements.TryGetValue(guidPath, out leafSaveData);
        }

        public void UpsertLeafSaveData(GuidPath guidPath, LeafSaveData leafSaveData)
        {
            Elements[guidPath] = leafSaveData;
        }

        public void Clear()
        {
            Elements.Clear();
        }
    }
}
