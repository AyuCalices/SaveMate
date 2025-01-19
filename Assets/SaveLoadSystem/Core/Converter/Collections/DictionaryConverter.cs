using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class DictionaryConverter<TKey, TValue> : BaseConverter<Dictionary<TKey, TValue>>
    {
        protected override void OnSave(Dictionary<TKey, TValue> input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", input.Count);
            
            var index = 0;
            foreach (var dataKey in input.Keys)
            {
                saveDataHandler.Save("key" + index, dataKey);
                saveDataHandler.Save("value" + index, input[dataKey]);
                index++;
            }
        }

        protected override Dictionary<TKey, TValue> OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            return new Dictionary<TKey, TValue>();
        }

        protected override void OnLoad(Dictionary<TKey, TValue> input, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad<int>("count", out var count);
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad<TKey>("key" + index, out var key)
                    && loadDataHandler.TryLoad<TValue>("value" + index, out var value))
                {
                    input.Add(key, value);
                }
            }
        }
    }
}
