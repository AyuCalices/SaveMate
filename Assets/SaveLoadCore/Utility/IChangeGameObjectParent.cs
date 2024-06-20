using UnityEngine;

namespace SaveLoadCore.Utility
{
    public interface IChangeGameObjectParent
    {
        public void OnChangeGameObjectParent(GameObject newParent, GameObject previousParent);
    }
}
