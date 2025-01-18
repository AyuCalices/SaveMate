using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector4Converter : SaveMateBaseConverter<Vector4>
    {
        protected override void OnSave(Vector4 input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", input.x);
            saveDataHandler.Save("y", input.y);
            saveDataHandler.Save("z", input.z);
            saveDataHandler.Save("w", input.w);
        }

        protected override Vector4 OnLoad(LoadDataHandler loadDataHandler)
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
