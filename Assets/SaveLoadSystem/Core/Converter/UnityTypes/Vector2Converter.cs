using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector2Converter : SaveMateBaseConverter<Vector2>
    {
        protected override void OnSave(Vector2 input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", input.x);
            saveDataHandler.Save("y", input.y);
        }

        protected override Vector2 OnLoad(LoadDataHandler loadDataHandler)
        {
            var vector2 = new Vector2();
            
            loadDataHandler.TryLoad("x", out vector2.x);
            loadDataHandler.TryLoad("y", out vector2.y);

            return vector2;
        }
    }
}
