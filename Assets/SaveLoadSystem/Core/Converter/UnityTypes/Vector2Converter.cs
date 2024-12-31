using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Vector2Converter : BaseConverter<Vector2>
    {
        protected override void OnSave(Vector2 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", data.x);
            saveDataHandler.Save("y", data.y);
        }

        protected override Vector2 OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            return new Vector2();
        }

        protected override void OnLoad(Vector2 data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("x", out data.x);
            loadDataHandler.TryLoad("y", out data.y);
        }
    }
}
