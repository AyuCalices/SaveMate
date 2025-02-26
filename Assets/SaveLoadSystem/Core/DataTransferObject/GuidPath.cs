using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public readonly struct GuidPath : IEquatable<GuidPath>
    {
        [JsonProperty] public readonly string[] TargetGuid;
        
        public GuidPath(string guid)
        {
            TargetGuid = new[] {guid};
        }
        
        public GuidPath(string[] parentPath)
        {
            TargetGuid = new string[parentPath.Length];
            Array.Copy(parentPath, TargetGuid, parentPath.Length);
        }
        
        public GuidPath(string guidSuffix, string[] parentPath)
        {
            TargetGuid = new string[parentPath.Length + 1];
            TargetGuid[0] = guidSuffix;
            Array.Copy(parentPath, 0, TargetGuid, 1, parentPath.Length);
        }
        
        public GuidPath(string[] parentPath, string guidPrefix)
        {
            TargetGuid = new string[parentPath.Length + 1];
            Array.Copy(parentPath, TargetGuid, parentPath.Length);
            TargetGuid[^1] = guidPrefix;
        }

        public override string ToString()
        {
            return string.Join(Path.DirectorySeparatorChar.ToString(), TargetGuid);
        }
        
        public bool Equals(GuidPath other)
        {
            return InternalEquals(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != GetType()) return false;
            return InternalEquals((GuidPath)obj);
        }

        public override int GetHashCode()
        {
            if (TargetGuid == null)
                return 0;

            // Use a hash code aggregator
            int hash = 17;
            foreach (var path in TargetGuid)
            {
                hash = hash * 31 + (path != null ? path.GetHashCode() : 0);
            }
            return hash;
        }

        private bool InternalEquals(GuidPath other)
        {
            return TargetGuid.SequenceEqual(other.TargetGuid);
        }
    }
}
