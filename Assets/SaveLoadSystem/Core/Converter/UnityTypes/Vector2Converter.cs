using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector2Converter : BaseConverter<Vector2>
    {
        protected override void OnSave(Vector2 input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", input.x);
            saveDataHandler.Save("y", input.y);
        }

        protected override Vector2 OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            return new Vector2();
        }

        protected override void OnLoad(Vector2 input, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad("x", out input.x);
            loadDataHandler.TryLoad("y", out input.y);
        }
    }
}
