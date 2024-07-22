using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Vector3Converter : BaseConverter<Vector3>
    {
        protected override void OnSave(Vector3 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("x", data.x);
            saveDataHandler.AddSerializable("y", data.y);
            saveDataHandler.AddSerializable("z", data.z);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.GetSerializable<float>("x");
            var y = loadDataHandler.GetSerializable<float>("y");
            var z = loadDataHandler.GetSerializable<float>("z");
            
            loadDataHandler.InitializeInstance(new Vector3(x, y, z));
        }
    }
}
