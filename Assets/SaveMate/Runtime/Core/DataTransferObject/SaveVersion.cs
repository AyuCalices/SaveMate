using System;
using Newtonsoft.Json;

namespace SaveMate.Core.DataTransferObject
{
    public readonly struct SaveVersion : IComparable<SaveVersion>
    {
        [JsonProperty] public readonly int Major;
        [JsonProperty] public readonly int Minor;
        [JsonProperty] public readonly int Patch;

        public SaveVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public int CompareTo(SaveVersion other)
        {
            if (Major != other.Major)
            {
                return Major.CompareTo(other.Major);
            }
            if (Minor != other.Minor)
            {
                return Minor.CompareTo(other.Minor);
            }
            return Patch.CompareTo(other.Patch);
        }

        public override bool Equals(object obj)
        {
            if (obj is SaveVersion version)
            {
                return CompareTo(version) == 0;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch);
        }

        public static bool operator >(SaveVersion left, SaveVersion right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <(SaveVersion left, SaveVersion right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator ==(SaveVersion left, SaveVersion right)
        {
            return left.CompareTo(right) == 0;
        }

        public static bool operator !=(SaveVersion left, SaveVersion right)
        {
            return !(left == right);
        }

        public static bool operator >=(SaveVersion left, SaveVersion right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(SaveVersion left, SaveVersion right)
        {
            return left.CompareTo(right) <= 0;
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }
    }
}
