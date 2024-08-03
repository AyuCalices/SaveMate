using System;

namespace SaveLoadSystem.Core
{
    [Serializable]
    public class ComponentsContainer
    {
        public string guid;
        public UnityEngine.Object unityObject;

        public ComponentsContainer(string guid, UnityEngine.Object unityObject)
        {
            this.guid = guid;
            this.unityObject = unityObject;
        }
    }
}
