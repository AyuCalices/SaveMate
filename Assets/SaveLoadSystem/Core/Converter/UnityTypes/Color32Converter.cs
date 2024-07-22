using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Color32Converter : BaseConverter<Color32>
    {
        protected override void OnSave(Color32 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("r", data.r);
            saveDataHandler.AddSerializable("g", data.g);
            saveDataHandler.AddSerializable("b", data.b);
            saveDataHandler.AddSerializable("a", data.a);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var r = loadDataHandler.GetSerializable<byte>("r");
            var g = loadDataHandler.GetSerializable<byte>("g");
            var b = loadDataHandler.GetSerializable<byte>("b");
            var a = loadDataHandler.GetSerializable<byte>("a");
            
            loadDataHandler.InitializeInstance(new Color32(r, g, b, a));
        }
    }
}
