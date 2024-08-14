using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Color32Converter : BaseConverter<Color32>
    {
        protected override void OnSave(Color32 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("r", data.r);
            saveDataHandler.SaveAsValue("g", data.g);
            saveDataHandler.SaveAsValue("b", data.b);
            saveDataHandler.SaveAsValue("a", data.a);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var r = loadDataHandler.LoadValue<byte>("r");
            var g = loadDataHandler.LoadValue<byte>("g");
            var b = loadDataHandler.LoadValue<byte>("b");
            var a = loadDataHandler.LoadValue<byte>("a");
            
            loadDataHandler.InitializeInstance(new Color32(r, g, b, a));
        }
    }
}
