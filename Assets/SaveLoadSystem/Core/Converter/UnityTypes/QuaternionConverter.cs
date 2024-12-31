using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class QuaternionConverter : BaseConverter<Quaternion>
    {
        protected override void OnSave(Quaternion data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", data.x);
            saveDataHandler.Save("y", data.y);
            saveDataHandler.Save("z", data.z);
            saveDataHandler.Save("w", data.w);
        }

        protected override Quaternion OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            return new Quaternion();
        }

        protected override void OnLoad(Quaternion data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("x", out data.x);
            loadDataHandler.TryLoad("y", out data.y);
            loadDataHandler.TryLoad("z", out data.z);
            loadDataHandler.TryLoad("w", out data.w);
        }
    }
}
