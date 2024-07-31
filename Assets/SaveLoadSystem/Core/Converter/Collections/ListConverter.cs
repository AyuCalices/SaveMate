using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class ListConverter : BaseConverter<IList>
    {
        protected override void OnSave(IList data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("count", data.Count);
            
            for (var index = 0; index < data.Count; index++)
            {
                saveDataHandler.TryAddReferencable(index.ToString(), data[index]);
            }
            
            var containedType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("type", containedType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.GetSerializable<int>("count");
            var loadElements = new List<object>();
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryGetReferencable(index.ToString(), out var obj))
                {
                    loadElements.Add(obj);
                }
            }
            
            var type = loadDataHandler.GetSerializable<Type>("type");
            var listType = typeof(List<>).MakeGenericType(type);
            var list = (IList)Activator.CreateInstance(listType);
            
            loadDataHandler.InitializeInstance(list);
            
            foreach (var saveElement in loadElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, foundObject => list.Add(foundObject));
            }
        }
    }
}
