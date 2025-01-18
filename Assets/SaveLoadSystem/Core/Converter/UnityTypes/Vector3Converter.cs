using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector3Converter : SaveMateBaseConverter<Vector3>
    {
        protected override void OnSave(Vector3 input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", input.x);
            saveDataHandler.Save("y", input.y);
            saveDataHandler.Save("z", input.z);
        }

        protected override Vector3 OnLoad(LoadDataHandler loadDataHandler)
        {
            var vector3 = new Vector3();
            
            loadDataHandler.TryLoad("x", out vector3.x);
            loadDataHandler.TryLoad("y", out vector3.y);
            loadDataHandler.TryLoad("z", out vector3.z);

            return vector3;
        }
    }
}
