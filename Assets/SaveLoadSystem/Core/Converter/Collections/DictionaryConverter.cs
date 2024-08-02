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
            saveDataHandler.AddSerializable("count", data.Keys.Count);
            
            var index = 0;
            foreach (var dataKey in data.Keys)
            {
                saveDataHandler.TryAddReferencable("key" + index, dataKey);
                saveDataHandler.TryAddReferencable("value" + index, data[dataKey]);
                index++;
            }
            
            var keyTypeString = data.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
            saveDataHandler.AddSerializable("keyType", keyTypeString);
            
            var valueTypeString = data.GetType().GetGenericArguments()[1].AssemblyQualifiedName;
            saveDataHandler.AddSerializable("valueType", valueTypeString);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.GetSerializable<int>("count");
            var loadElements = new List<(GuidPath, GuidPath)>();
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryGetReferencable("key" + index, out var key)
                    && loadDataHandler.TryGetReferencable("value" + index, out var value))
                {
                    loadElements.Add((key, value));
                }
            }
            
            var keyTypeString = loadDataHandler.GetSerializable<string>("keyType");
            var valueTypeString = loadDataHandler.GetSerializable<string>("valueType");
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
