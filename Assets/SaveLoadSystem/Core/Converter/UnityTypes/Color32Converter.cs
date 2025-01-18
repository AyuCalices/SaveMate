using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Color32Converter : IConverter<Color32>
    {
        public void Save(Color32 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("r", data.r);
            saveDataHandler.Save("g", data.g);
            saveDataHandler.Save("b", data.b);
            saveDataHandler.Save("a", data.a);
        }

        public Color32 Load(LoadDataHandler loadDataHandler)
        {
            var color32 = new Color32();
            
            loadDataHandler.TryLoad("r", out color32.r);
            loadDataHandler.TryLoad("g", out color32.g);
            loadDataHandler.TryLoad("b", out color32.b);
            loadDataHandler.TryLoad("a", out color32.a);

            return color32;
        }
    }
}
