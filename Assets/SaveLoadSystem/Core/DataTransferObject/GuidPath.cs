using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class GuidPath : IEquatable<GuidPath>
    {
        [JsonProperty] public readonly string[] FullPath;
        
        public GuidPath() {}
        
        public GuidPath(string guid)
        {
            FullPath = new[] {guid};
        }
        
        public GuidPath(string[] parentPath, string guid)
        {
            FullPath = new string[parentPath.Length + 1];
            Array.Copy(parentPath, FullPath, parentPath.Length);
            FullPath[^1] = guid;
        }
        
        public Stack<string> ToStack()
        {
            var stack = new Stack<string>(FullPath.Reverse());
            return stack;
        }

        public override string ToString()
        {
            return string.Join(Path.DirectorySeparatorChar.ToString(), FullPath);
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
            if (FullPath == null)
                return 0;

            // Use a hash code aggregator
            int hash = 17;
            foreach (var path in FullPath)
            {
                hash = hash * 31 + (path != null ? path.GetHashCode() : 0);
            }
            return hash;
        }

        private bool InternalEquals(GuidPath other)
        {
            return FullPath.SequenceEqual(other.FullPath);
        }
    }
}
