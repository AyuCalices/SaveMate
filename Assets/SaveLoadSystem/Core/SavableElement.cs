using System.Collections.Generic;
using SaveLoadSystem.Core.Component;
using SaveLoadSystem.Core.Serializable;

namespace SaveLoadSystem.Core
{
    public class SavableElement
    {
        public SaveStrategy SaveStrategy;
        public GuidPath CreatorGuidPath;
        public object Obj;
        public Dictionary<string, object> MemberInfoList;
    }
}
