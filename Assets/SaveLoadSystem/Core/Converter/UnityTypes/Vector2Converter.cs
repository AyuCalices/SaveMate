using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Vector2Converter : BaseConverter<Vector2>
    {
        protected override void OnSave(Vector2 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("x", data.x);
            saveDataHandler.AddSerializable("y", data.y);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.GetSaveElement<float>("x");
            var y = loadDataHandler.GetSaveElement<float>("y");
            
            loadDataHandler.InitializeInstance(new Vector2(x, y));
        }
    }
}
