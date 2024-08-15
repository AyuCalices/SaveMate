using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class ColorConverter : BaseConverter<Color>
    {
        protected override void OnSave(Color data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("r", data.r);
            saveDataHandler.SaveAsValue("g", data.g);
            saveDataHandler.SaveAsValue("b", data.b);
            saveDataHandler.SaveAsValue("a", data.a);
        }

        public override object OnLoad(LoadDataHandler loadDataHandler)
        {
            var r = loadDataHandler.LoadValue<float>("r");
            var g = loadDataHandler.LoadValue<float>("g");
            var b = loadDataHandler.LoadValue<float>("b");
            var a = loadDataHandler.LoadValue<float>("a");

            return new Color(r, g, b, a);
        }
    }
}
