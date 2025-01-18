using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class ColorConverter : IConverter<Color>
    {
        public void Save(Color data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("r", data.r);
            saveDataHandler.Save("g", data.g);
            saveDataHandler.Save("b", data.b);
            saveDataHandler.Save("a", data.a);
        }

        public Color Load(LoadDataHandler loadDataHandler)
        {
            var color = new Color();
            
            loadDataHandler.TryLoad("r", out color.r);
            loadDataHandler.TryLoad("g", out color.g);
            loadDataHandler.TryLoad("b", out color.b);
            loadDataHandler.TryLoad("a", out color.a);

            return color;
        }
    }
}
