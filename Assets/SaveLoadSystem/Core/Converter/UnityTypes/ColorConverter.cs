using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class ColorConverter : BaseConverter<Color>
    {
        protected override void OnSave(Color data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("r", data.r);
            saveDataHandler.AddSerializable("g", data.g);
            saveDataHandler.AddSerializable("b", data.b);
            saveDataHandler.AddSerializable("a", data.a);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var r = loadDataHandler.GetSerializable<float>("r");
            var g = loadDataHandler.GetSerializable<float>("g");
            var b = loadDataHandler.GetSerializable<float>("b");
            var a = loadDataHandler.GetSerializable<float>("a");

            loadDataHandler.InitializeInstance(new Color(r, g, b, a));
        }
    }
}
