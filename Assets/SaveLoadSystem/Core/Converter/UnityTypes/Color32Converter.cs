using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Color32Converter : BaseConverter<Color32>
    {
        protected override void OnSave(Color32 input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("r", input.r);
            saveDataHandler.Save("g", input.g);
            saveDataHandler.Save("b", input.b);
            saveDataHandler.Save("a", input.a);
        }

        protected override Color32 OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            return new Color32();
        }

        protected override void OnLoad(Color32 input, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("r", out input.r);
            loadDataHandler.TryLoad("g", out input.g);
            loadDataHandler.TryLoad("b", out input.b);
            loadDataHandler.TryLoad("a", out input.a);
        }
    }
}
