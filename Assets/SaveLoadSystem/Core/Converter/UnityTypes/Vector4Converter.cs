using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Vector4Converter : BaseConverter<Vector4>
    {
        protected override void OnSave(Vector4 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", data.x);
            saveDataHandler.Save("y", data.y);
            saveDataHandler.Save("z", data.z);
            saveDataHandler.Save("w", data.w);
        }

        protected override Vector4 OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            return new Vector4();
        }

        protected override void OnLoad(Vector4 data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("x", out data.x);
            loadDataHandler.TryLoad("y", out data.y);
            loadDataHandler.TryLoad("z", out data.z);
            loadDataHandler.TryLoad("w", out data.w);
        }
    }
}
