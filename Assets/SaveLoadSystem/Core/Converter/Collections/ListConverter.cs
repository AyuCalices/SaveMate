using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class ListConverter<T> : BaseConverter<List<T>>
    {
        protected override void OnSave(List<T> data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Count);
            
            for (var index = 0; index < data.Count; index++)
            {
                saveDataHandler.Save(index.ToString(), data[index]);
            }
        }

        protected override List<T> OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            return new List<T>();
        }

        protected override void OnLoad(List<T> input, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoadValue("count", out int count);
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad<T>(index.ToString(), out var obj))
                {
                    input.Add(obj);
                }
            }
        }
    }
}
