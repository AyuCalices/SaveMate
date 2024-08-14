using UnityEngine;

namespace SaveLoadSystem.Core.Converter.UnityTypes
{
    public class Vector2Converter : BaseConverter<Vector2>
    {
        protected override void OnSave(Vector2 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("x", data.x);
            saveDataHandler.SaveAsValue("y", data.y);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.LoadValue<float>("x");
            var y = loadDataHandler.LoadValue<float>("y");
            
            loadDataHandler.InitializeInstance(new Vector2(x, y));
        }
    }
}
