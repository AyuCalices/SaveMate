using System.Collections.Generic;
using SaveLoadCore.Core.Component;
using SaveLoadCore.Core.Serializable;

namespace SaveLoadCore.Core
{
    public class SavableElement
    {
        public SaveStrategy SaveStrategy;
        public GuidPath CreatorGuidPath;
        public object Obj;
        public Dictionary<string, object> MemberInfoList;
    }
}
