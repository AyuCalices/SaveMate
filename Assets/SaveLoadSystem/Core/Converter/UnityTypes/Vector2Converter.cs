using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    [UsedImplicitly]
    public class Vector2Converter : IConverter<Vector2>
    {
        public void Save(Vector2 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("x", data.x);
            saveDataHandler.Save("y", data.y);
        }

        public Vector2 Load(LoadDataHandler loadDataHandler)
        {
            var vector2 = new Vector2();
            
            loadDataHandler.TryLoad("x", out vector2.x);
            loadDataHandler.TryLoad("y", out vector2.y);

            return vector2;
        }
    }
}
