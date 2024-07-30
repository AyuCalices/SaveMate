using System;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Serializable
{
    [Serializable]
    public class GuidPath
    {
        public readonly GuidPath Parent;
        public readonly string Guid;
        
        public GuidPath() {}

        public GuidPath(GuidPath parent, string guid)
        {
            Parent = parent;
            Guid = guid;
        }

        public Stack<string> ToStack()
        {
            var stack = new Stack<string>();

            var currentPath = this;
            while (currentPath != null)
            {
                stack.Push(currentPath.Guid);
                currentPath = currentPath.Parent;
            }

            return stack;
        }

        public override string ToString()
        {
            var pathString = "";
            
            var currentPath = this;
            while (currentPath != null)
            {
                pathString = currentPath.Guid + "/" + pathString;
                currentPath = currentPath.Parent;
            }

            return pathString;
        }
    }
}
