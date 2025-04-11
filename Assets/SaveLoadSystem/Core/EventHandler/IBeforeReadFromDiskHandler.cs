using UnityEngine;

namespace SaveLoadSystem.Core.EventHandler
{
    public interface IBeforeReadFromDiskHandler
    {
        void OnBeforeReadFromDisk();
    }
}
