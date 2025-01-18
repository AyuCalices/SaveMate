using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector4Converter : IConverter<Vector4>
    {
        public void Save(Vector4 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", data.x);
            saveDataHandler.Save("y", data.y);
            saveDataHandler.Save("z", data.z);
            saveDataHandler.Save("w", data.w);
        }

        public Vector4 Load(LoadDataHandler loadDataHandler)
        {
            var vector4 = new Vector4();
            
            loadDataHandler.TryLoad("x", out vector4.x);
            loadDataHandler.TryLoad("y", out vector4.y);
            loadDataHandler.TryLoad("z", out vector4.z);
            loadDataHandler.TryLoad("w", out vector4.w);

            return vector4;
        }
    }
}
