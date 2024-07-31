using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class DictionaryConverter : BaseConverter<IDictionary>
    {
        protected override void OnSave(IDictionary data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("count", data.Keys.Count);
            
            var index = 0;
            foreach (var dataKey in data.Keys)
            {
                saveDataHandler.TryAddReferencable("key" + index, dataKey);
                saveDataHandler.TryAddReferencable("value" + index, data[dataKey]);
                index++;
            }
            
            var keyType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("keyType", keyType);
            
            var valueType = data.GetType().GetGenericArguments()[1];
            saveDataHandler.AddSerializable("valueType", valueType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.GetSerializable<int>("count");
            var loadElements = new List<(object, object)>();
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryGetReferencable("key" + index, out var key)
                    && loadDataHandler.TryGetReferencable("value" + index, out var value))
                {
                    loadElements.Add((key, value));
                }
            }
            
            var keyType = loadDataHandler.GetSerializable<Type>("keyType");
            var valueType = loadDataHandler.GetSerializable<Type>("valueType");
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);
            
            loadDataHandler.InitializeInstance(dictionary);

            foreach (var (key, value) in loadElements)
            {
                var objectGroup = new[] { key, value };
                loadDataHandler.EnqueueReferenceBuilding(objectGroup, foundObject =>
                {
                    dictionary.Add(foundObject[0], foundObject[1]);
                });
            }
        }
    }
}
