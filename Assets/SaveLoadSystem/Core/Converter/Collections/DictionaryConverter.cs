using System;
using System.Collections;
using System.Collections.Generic;
using SaveLoadSystem.Core.DataTransferObject;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class DictionaryConverter : BaseConverter<IDictionary>
    {
        protected override void OnSave(IDictionary data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Keys.Count);
            
            var index = 0;
            foreach (var dataKey in data.Keys)
            {
                saveDataHandler.TrySaveAsReferencable("key" + index, dataKey);
                saveDataHandler.TrySaveAsReferencable("value" + index, data[dataKey]);
                index++;
            }
            
            var keyTypeString = data.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
            saveDataHandler.SaveAsValue("keyType", keyTypeString);
            
            var valueTypeString = data.GetType().GetGenericArguments()[1].AssemblyQualifiedName;
            saveDataHandler.SaveAsValue("valueType", valueTypeString);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.LoadValue<int>("count");
            var loadElements = new List<(GuidPath, GuidPath)>();
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoadReferencable("key" + index, out var key)
                    && loadDataHandler.TryLoadReferencable("value" + index, out var value))
                {
                    loadElements.Add((key, value));
                }
            }
            
            var keyTypeString = loadDataHandler.LoadValue<string>("keyType");
            var valueTypeString = loadDataHandler.LoadValue<string>("valueType");
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(Type.GetType(keyTypeString), Type.GetType(valueTypeString));
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
