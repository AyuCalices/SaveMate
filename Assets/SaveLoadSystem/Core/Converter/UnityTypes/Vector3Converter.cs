using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Vector3Converter : BaseConverter<Vector3>
    {
        protected override void OnSave(Vector3 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", data.x);
            saveDataHandler.Save("y", data.y);
            saveDataHandler.Save("z", data.z);
        }

        protected override Vector3 OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            return new Vector3();
        }

        protected override void OnLoad(Vector3 data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("x", out data.x);
            loadDataHandler.TryLoad("y", out data.y);
            loadDataHandler.TryLoad("z", out data.z);
        }
    }
}
