using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector3Converter : IConverter<Vector3>
    {
        public void Save(Vector3 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", data.x);
            saveDataHandler.Save("y", data.y);
            saveDataHandler.Save("z", data.z);
        }

        public Vector3 Load(LoadDataHandler loadDataHandler)
        {
            var vector3 = new Vector3();
            
            loadDataHandler.TryLoad("x", out vector3.x);
            loadDataHandler.TryLoad("y", out vector3.y);
            loadDataHandler.TryLoad("z", out vector3.z);

            return vector3;
        }
    }
}
