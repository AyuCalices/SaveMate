using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class ColorConverter : BaseConverter<Color>
    {
        protected override void OnSave(Color data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("r", data.r);
            saveDataHandler.Save("g", data.g);
            saveDataHandler.Save("b", data.b);
            saveDataHandler.Save("a", data.a);
        }

        protected override Color OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            return new Color();
        }

        protected override void OnLoad(Color data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("r", out data.r);
            loadDataHandler.TryLoad("g", out data.g);
            loadDataHandler.TryLoad("b", out data.b);
            loadDataHandler.TryLoad("a", out data.a);
        }
    }
}
