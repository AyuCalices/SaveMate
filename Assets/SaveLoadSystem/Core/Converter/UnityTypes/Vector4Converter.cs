using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Vector4Converter : BaseConverter<Vector4>
    {
        protected override void OnSave(Vector4 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("x", data.x);
            saveDataHandler.SaveAsValue("y", data.y);
            saveDataHandler.SaveAsValue("z", data.z);
            saveDataHandler.SaveAsValue("w", data.w);
        }

        public override object OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.LoadValue<float>("x");
            var y = loadDataHandler.LoadValue<float>("y");
            var z = loadDataHandler.LoadValue<float>("z");
            var w = loadDataHandler.LoadValue<float>("w");

            return new Vector4(x, y, z, w);
        }
    }
}
