using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class ColorConverter : SaveMateBaseConverter<Color>
    {
        protected override void OnSave(Color input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("r", input.r);
            saveDataHandler.Save("g", input.g);
            saveDataHandler.Save("b", input.b);
            saveDataHandler.Save("a", input.a);
        }

        protected override Color OnLoad(LoadDataHandler loadDataHandler)
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
