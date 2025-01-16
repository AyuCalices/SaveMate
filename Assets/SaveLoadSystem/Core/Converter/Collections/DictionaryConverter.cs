using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class DictionaryConverter : BaseConverter<IDictionary>
    {
        protected override void OnSave(IDictionary data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Keys.Count);
            
            var keyTypeString = data.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
            saveDataHandler.SaveAsValue("keyType", keyTypeString);
            
            var valueTypeString = data.GetType().GetGenericArguments()[1].AssemblyQualifiedName;
            saveDataHandler.SaveAsValue("valueType", valueTypeString);
            
            var index = 0;
            foreach (var dataKey in data.Keys)
            {
                saveDataHandler.Save("key" + index, dataKey);
                saveDataHandler.Save("value" + index, data[dataKey]);
                index++;
            }
        }

        protected override IDictionary OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoadValue("keyType", out string keyTypeString);
            loadDataHandler.TryLoadValue("valueType", out string valueTypeString);
            
            var keyType = Type.GetType(keyTypeString);
            var valueType = Type.GetType(valueTypeString);
            
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            return (IDictionary)Activator.CreateInstance(dictionaryType);
        }

        protected override void OnLoad(IDictionary data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoad<int>("count", out var count);
            
            var keyType = data.GetType().GetGenericArguments()[0];
            var valueType = data.GetType().GetGenericArguments()[1];
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad(keyType, "key" + index, out var key)
                    && loadDataHandler.TryLoad(valueType, "value" + index, out var value))
                {
                    data.Add(key, value);
                }
            }
        }
    }
}
