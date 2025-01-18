using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class ListConverter<T> : SaveMateBaseConverter<List<T>>
    {
        protected override void OnSave(List<T> data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Count);
            
            for (var index = 0; index < data.Count; index++)
            {
                saveDataHandler.Save(index.ToString(), data[index]);
            }
        }

        protected override List<T> OnLoad(LoadDataHandler loadDataHandler)
        {
            var list = new List<T>();
            
            loadDataHandler.TryLoadValue("count", out int count);
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad<T>(index.ToString(), out var obj))
                {
                    list.Add(obj);
                }
            }

            return list;
        }
    }
}
