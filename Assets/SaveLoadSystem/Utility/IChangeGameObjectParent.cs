using UnityEngine;

namespace SaveLoadSystem.Utility
{
    public interface IChangeGameObjectParent
    {
        public void OnChangeGameObjectParent(GameObject newParent, GameObject previousParent);
    }
}
