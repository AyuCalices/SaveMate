using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector4Converter : BaseConverter<Vector4>
    {
        protected override void OnSave(Vector4 input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", input.x);
            saveDataHandler.Save("y", input.y);
            saveDataHandler.Save("z", input.z);
            saveDataHandler.Save("w", input.w);
        }

        protected override Vector4 OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            return new Vector4();
        }

        protected override void OnLoad(Vector4 input, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("x", out input.x);
            loadDataHandler.TryLoad("y", out input.y);
            loadDataHandler.TryLoad("z", out input.z);
            loadDataHandler.TryLoad("w", out input.w);
        }
    }
}
