using System;
using System.Collections;
using System.Collections.Generic;
using SaveLoadSystem.Core.DataTransferObject;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class ListConverter : BaseConverter<IList>
    {
        protected override void OnSave(IList data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Count);
            
            for (var index = 0; index < data.Count; index++)
            {
                saveDataHandler.TrySaveAsReferencable(index.ToString(), data[index]);
            }
            
            var typeString = data.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
            saveDataHandler.SaveAsValue("type", typeString);
        }

        public override object OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.LoadValue<int>("count");
            var loadElements = new List<GuidPath>();
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoadReferencable(index.ToString(), out var obj))
                {
                    loadElements.Add(obj);
                }
            }
            
            var typeString = loadDataHandler.LoadValue<string>("type");
            var listType = typeof(List<>).MakeGenericType(Type.GetType(typeString));
            var list = (IList)Activator.CreateInstance(listType);
            
            foreach (var saveElement in loadElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, foundObject => list.Add(foundObject));
            }

            return list;
        }
    }
}
