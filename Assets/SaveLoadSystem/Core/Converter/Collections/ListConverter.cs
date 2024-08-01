using System;
using System.Collections;
using System.Collections.Generic;
using SaveLoadSystem.Core.SerializableTypes;

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
            
            var typeString = data.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
            saveDataHandler.AddSerializable("type", typeString);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.GetSerializable<int>("count");
            var loadElements = new List<GuidPath>();
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryGetReferencable(index.ToString(), out var obj))
                {
                    loadElements.Add(obj);
                }
            }
            
            var typeString = loadDataHandler.GetSerializable<string>("type");
            var listType = typeof(List<>).MakeGenericType(Type.GetType(typeString));
            var list = (IList)Activator.CreateInstance(listType);
            
            loadDataHandler.InitializeInstance(list);
            
            foreach (var saveElement in loadElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, foundObject => list.Add(foundObject));
            }
        }
    }
}
