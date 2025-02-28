using System.Collections.Generic;
using System.Linq;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public struct PrefabGuidGroup
    {
        public HashSet<SavablePrefabElement> Guids { get; set; }

        public IEnumerable<SavablePrefabElement> Except(PrefabGuidGroup prefabGuidGroup)
        {
            return Guids.Except(prefabGuidGroup.Guids);
        }
    }
}
