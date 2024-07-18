using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class ListConverter : BaseConverter<IList>
    {
        protected override void OnSave(IList data, SaveDataHandler saveDataHandler)
        {
            var listElements = new List<object>();
            for (var index = 0; index < data.Count; index++)
            {
                var savable = saveDataHandler.ToReferencableObject(index.ToString(), data[index]);
                listElements.Add(savable);
            }
            saveDataHandler.AddSerializable("elements", listElements);
            
            var containedType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("type", containedType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var saveElements = loadDataHandler.GetSaveElement<List<object>>("elements");
            var type = loadDataHandler.GetSaveElement<Type>("type");
            
            var listType = typeof(List<>).MakeGenericType(type);
            var list = (IList)Activator.CreateInstance(listType);
            
            loadDataHandler.InitializeInstance(list);
            
            foreach (var saveElement in saveElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, foundObject => list.Add(foundObject));
            }
        }
    }
}
