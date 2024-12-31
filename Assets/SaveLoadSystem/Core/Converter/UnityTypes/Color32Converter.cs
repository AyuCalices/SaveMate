using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    
    public class Color32Converter : BaseConverter<Color32>
    {
        protected override void OnSave(Color32 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("r", data.r);
            saveDataHandler.Save("g", data.g);
            saveDataHandler.Save("b", data.b);
            saveDataHandler.Save("a", data.a);
        }

        protected override Color32 OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            return new Color32();
        }

        protected override void OnLoad(Color32 data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("r", out data.r);
            loadDataHandler.TryLoad("g", out data.g);
            loadDataHandler.TryLoad("b", out data.b);
            loadDataHandler.TryLoad("a", out data.a);
        }
    }
}
