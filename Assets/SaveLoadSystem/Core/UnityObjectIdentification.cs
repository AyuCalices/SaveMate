using System;
using Object = UnityEngine.Object;

namespace SaveLoadSystem.Core
{
    [Serializable]
    public class UnityObjectIdentification
    {
        public string guid;
        public Object unityObject;

        public UnityObjectIdentification(string guid, Object unityObject)
        {
            this.guid = guid;
            this.unityObject = unityObject;
        }
    }
}
