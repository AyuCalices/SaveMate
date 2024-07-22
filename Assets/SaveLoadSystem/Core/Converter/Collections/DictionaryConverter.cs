using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class DictionaryConverter : BaseConverter<IDictionary>
    {
        protected override void OnSave(IDictionary data, SaveDataHandler saveDataHandler)
        {
            var listElements = new Dictionary<object, object>();

            var index = 0;
            foreach (var dataKey in data.Keys)
            {
                var savableKey = saveDataHandler.ToReferencableObject(index.ToString(), dataKey);
                var savableValue = saveDataHandler.ToReferencableObject(index.ToString(), data[dataKey]);
                listElements.Add(savableKey, savableValue);
                
                index++;
            }
            saveDataHandler.AddSerializable("elements", listElements);
            
            var keyType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("keyType", keyType);
            
            var valueType = data.GetType().GetGenericArguments()[1];
            saveDataHandler.AddSerializable("valueType", valueType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var saveElements = loadDataHandler.GetSerializable<Dictionary<object, object>>("elements");
            
            var keyType = loadDataHandler.GetSerializable<Type>("keyType");
            var valueType = loadDataHandler.GetSerializable<Type>("valueType");
            
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);
            
            loadDataHandler.InitializeInstance(dictionary);

            foreach (var (key, value) in saveElements)
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
