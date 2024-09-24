using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Vector3Converter : BaseConverter<Vector3>
    {
        protected override void OnSave(Vector3 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("x", data.x);
            saveDataHandler.SaveAsValue("y", data.y);
            saveDataHandler.SaveAsValue("z", data.z);
        }

        public override object OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.LoadValue<float>("x");
            var y = loadDataHandler.LoadValue<float>("y");
            var z = loadDataHandler.LoadValue<float>("z");

            return new Vector3(x, y, z);
        }
    }
}
