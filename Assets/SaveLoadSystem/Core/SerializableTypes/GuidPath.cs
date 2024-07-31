using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveLoadSystem.Core.SerializableTypes
{
    [Serializable]
    public class GuidPath : IEquatable<GuidPath>
    {
        public string[] fullPath;
        
        public GuidPath() {}
        
        public GuidPath(string guid)
        {
            fullPath = new[] {guid};
        }
        
        public GuidPath(string[] parentPath, string guid)
        {
            fullPath = new string[parentPath.Length + 1];
            Array.Copy(parentPath, fullPath, parentPath.Length);
            fullPath[^1] = guid;
        }
        
        public Stack<string> ToStack()
        {
            var stack = new Stack<string>(fullPath.Reverse());
            return stack;
        }

        public override string ToString()
        {
            var pathString = "";
            
            foreach (var path in fullPath)
            {
                pathString += path + "/";
            }

            return pathString;
        }
        
        public bool Equals(GuidPath other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return InternalEquals(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return InternalEquals((GuidPath)obj);
        }

        public override int GetHashCode()
        {
            return (fullPath != null ? fullPath.GetHashCode() : 0);
        }

        private bool InternalEquals(GuidPath other)
        {
            return fullPath.SequenceEqual(other.fullPath);
        }
    }
}
