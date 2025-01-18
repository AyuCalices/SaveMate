using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class QuaternionConverter : SaveMateBaseConverter<Quaternion>
    {
        protected override void OnSave(Quaternion input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", input.x);
            saveDataHandler.Save("y", input.y);
            saveDataHandler.Save("z", input.z);
            saveDataHandler.Save("w", input.w);
        }

        protected override Quaternion OnLoad(LoadDataHandler loadDataHandler)
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
