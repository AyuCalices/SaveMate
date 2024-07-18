using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Vector4Converter : BaseConverter<Vector4>
    {
        protected override void OnSave(Vector4 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("x", data.x);
            saveDataHandler.AddSerializable("y", data.y);
            saveDataHandler.AddSerializable("z", data.z);
            saveDataHandler.AddSerializable("w", data.w);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.GetSaveElement<float>("x");
            var y = loadDataHandler.GetSaveElement<float>("y");
            var z = loadDataHandler.GetSaveElement<float>("z");
            var w = loadDataHandler.GetSaveElement<float>("w");
            
            loadDataHandler.InitializeInstance(new Vector4(x, y, z, w));
        }
    }
}
