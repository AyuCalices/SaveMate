using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace SaveMate.Runtime.Core.DataTransferObject
{
    public struct GuidPath : IEquatable<GuidPath>
    {
        public string SceneName { get; set; }
        [UsedImplicitly] public string[] TargetGuid { get; set; }
        
        public GuidPath(string sceneName, string guid)
        {
            SceneName = sceneName;
            TargetGuid = new[] {guid};
        }

        public GuidPath(string sceneName, string[] guidPath)
        {
            SceneName = sceneName;
            TargetGuid = new string[guidPath.Length];
            Array.Copy(guidPath, TargetGuid, guidPath.Length);
        }
        
        public GuidPath(GuidPath parentPath, string guidPrefix)
        {
            SceneName = parentPath.SceneName;
            TargetGuid = new string[parentPath.TargetGuid.Length + 1];
            Array.Copy(parentPath.TargetGuid, TargetGuid, parentPath.TargetGuid.Length);
            TargetGuid[^1] = guidPrefix;
        }

        public override string ToString()
        {
            return SceneName + Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar.ToString(), TargetGuid);
        }
        
        public static GuidPath FromString(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar);
            var scene = parts[0];
            var targetGuid = parts.Skip(1).ToArray();

            return new GuidPath(scene, targetGuid);
        }
        
        public bool Equals(GuidPath other)
        {
            return SceneName == other.SceneName && TargetGuid.SequenceEqual(other.TargetGuid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != GetType()) return false;
            return Equals((GuidPath)obj);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (SceneName != null ? SceneName.GetHashCode() : 0);
        
            if (TargetGuid != null)
            {
                foreach (var path in TargetGuid)
                {
                    hash = hash * 31 + (path != null ? path.GetHashCode() : 0);
                }
            }
        
            return hash;
        }
    }
}
