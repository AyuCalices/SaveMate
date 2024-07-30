using System;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.SerializableTypes
{
    [Serializable]
    public class GuidPath
    {
        public GuidPath parent;
        public string guid;

        public GuidPath(GuidPath parent, string guid)
        {
            this.parent = parent;
            this.guid = guid;
        }

        public Stack<string> ToStack()
        {
            var stack = new Stack<string>();

            var currentPath = this;
            while (currentPath != null)
            {
                stack.Push(currentPath.guid);
                currentPath = currentPath.parent;
            }

            return stack;
        }

        public override string ToString()
        {
            var pathString = "";
            
            var currentPath = this;
            while (currentPath != null)
            {
                pathString = currentPath.guid + "/" + pathString;
                currentPath = currentPath.parent;
            }

            return pathString;
        }
    }
}
