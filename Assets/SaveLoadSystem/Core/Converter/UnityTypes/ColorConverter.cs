using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class ColorConverter : BaseConverter<Color>
    {
        protected override void OnSave(Color input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("r", input.r);
            saveDataHandler.Save("g", input.g);
            saveDataHandler.Save("b", input.b);
            saveDataHandler.Save("a", input.a);
        }

        protected override Color OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            return new Color();
        }

        protected override void OnLoad(Color input, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("r", out input.r);
            loadDataHandler.TryLoad("g", out input.g);
            loadDataHandler.TryLoad("b", out input.b);
            loadDataHandler.TryLoad("a", out input.a);
        }
    }
}
