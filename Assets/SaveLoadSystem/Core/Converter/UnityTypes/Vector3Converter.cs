using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector3Converter : BaseConverter<Vector3>
    {
        protected override void OnSave(Vector3 input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", input.x);
            saveDataHandler.Save("y", input.y);
            saveDataHandler.Save("z", input.z);
        }

        protected override Vector3 OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            return new Vector3();
        }

        protected override void OnLoad(Vector3 input, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("x", out input.x);
            loadDataHandler.TryLoad("y", out input.y);
            loadDataHandler.TryLoad("z", out input.z);
        }
    }
}
