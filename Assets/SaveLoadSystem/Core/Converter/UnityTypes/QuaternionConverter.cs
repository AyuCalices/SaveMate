using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class QuaternionConverter : BaseConverter<Quaternion>
    {
        protected override void OnSave(Quaternion data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("x", data.x);
            saveDataHandler.SaveAsValue("y", data.y);
            saveDataHandler.SaveAsValue("z", data.z);
            saveDataHandler.SaveAsValue("w", data.w);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.LoadValue<float>("x");
            var y = loadDataHandler.LoadValue<float>("y");
            var z = loadDataHandler.LoadValue<float>("z");
            var w = loadDataHandler.LoadValue<float>("w");
            
            loadDataHandler.InitializeInstance(new Quaternion(x, y, z, w));
        }
    }
}
