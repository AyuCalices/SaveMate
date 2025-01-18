using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Color32Converter : SaveMateBaseConverter<Color32>
    {
        protected override void OnSave(Color32 input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("r", input.r);
            saveDataHandler.Save("g", input.g);
            saveDataHandler.Save("b", input.b);
            saveDataHandler.Save("a", input.a);
        }

        protected override Color32 OnLoad(LoadDataHandler loadDataHandler)
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
