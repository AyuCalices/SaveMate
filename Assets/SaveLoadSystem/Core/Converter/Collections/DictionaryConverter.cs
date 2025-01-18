using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class DictionaryConverter<TKey, TValue> : SaveMateBaseConverter<Dictionary<TKey, TValue>>
    {
        protected override void OnSave(Dictionary<TKey, TValue> data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Count);
            
            var index = 0;
            foreach (var dataKey in data.Keys)
            {
                saveDataHandler.Save("key" + index, dataKey);
                saveDataHandler.Save("value" + index, data[dataKey]);
                index++;
            }
        }

        protected override Dictionary<TKey, TValue> OnLoad(LoadDataHandler loadDataHandler)
        {
            var dictionary = new Dictionary<TKey, TValue>();
            
            loadDataHandler.TryLoad<int>("count", out var count);
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad<TKey>("key" + index, out var key)
                    && loadDataHandler.TryLoad<TValue>("value" + index, out var value))
                {
                    dictionary.Add(key, value);
                }
            }

            return dictionary;
        }
    }
}
