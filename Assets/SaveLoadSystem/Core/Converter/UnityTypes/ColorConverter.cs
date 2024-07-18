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
            var r = loadDataHandler.GetSaveElement<float>("r");
            var g = loadDataHandler.GetSaveElement<float>("g");
            var b = loadDataHandler.GetSaveElement<float>("b");
            var a = loadDataHandler.GetSaveElement<float>("a");

            loadDataHandler.InitializeInstance(new Color(r, g, b, a));
        }
    }
}
