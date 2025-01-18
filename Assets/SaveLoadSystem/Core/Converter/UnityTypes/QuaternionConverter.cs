using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class QuaternionConverter : IConverter<Quaternion>
    {
        public void Save(Quaternion data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", data.x);
            saveDataHandler.Save("y", data.y);
            saveDataHandler.Save("z", data.z);
            saveDataHandler.Save("w", data.w);
        }

        public Quaternion Load(LoadDataHandler loadDataHandler)
        {
            var quaternion = new Quaternion();
            
            loadDataHandler.TryLoad("x", out quaternion.x);
            loadDataHandler.TryLoad("y", out quaternion.y);
            loadDataHandler.TryLoad("z", out quaternion.z);
            loadDataHandler.TryLoad("w", out quaternion.w);

            return quaternion;
        }
    }
}
