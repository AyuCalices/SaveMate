using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class QuaternionConverter : BaseConverter<Quaternion>
    {
        protected override void OnSave(Quaternion data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("x", data.x);
            saveDataHandler.AddSerializable("y", data.y);
            saveDataHandler.AddSerializable("z", data.z);
            saveDataHandler.AddSerializable("w", data.w);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.GetSerializable<float>("x");
            var y = loadDataHandler.GetSerializable<float>("y");
            var z = loadDataHandler.GetSerializable<float>("z");
            var w = loadDataHandler.GetSerializable<float>("w");
            
            loadDataHandler.InitializeInstance(new Quaternion(x, y, z, w));
        }
    }
}
