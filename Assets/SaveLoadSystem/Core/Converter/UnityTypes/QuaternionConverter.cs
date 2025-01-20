using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class QuaternionConverter : BaseConverter<Quaternion>
    {
        protected override void OnSave(Quaternion input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", input.x);
            saveDataHandler.Save("y", input.y);
            saveDataHandler.Save("z", input.z);
            saveDataHandler.Save("w", input.w);
        }

        protected override Quaternion OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            return new Quaternion();
        }

        protected override void OnLoad(Quaternion input, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("x", out input.x);
            loadDataHandler.TryLoad("y", out input.y);
            loadDataHandler.TryLoad("z", out input.z);
            loadDataHandler.TryLoad("w", out input.w);
        }
    }
}
