using System;

namespace SaveLoadCore.Core
{
    [Serializable]
    public class ComponentsContainer
    {
        //TODO: change to tuple
        public string guid;
        public UnityEngine.Component component;
    }
}
