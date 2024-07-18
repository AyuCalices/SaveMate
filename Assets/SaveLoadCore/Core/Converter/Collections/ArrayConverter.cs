using System;
using System.Collections.Generic;

namespace SaveLoadCore.Core.Converter.Collections
{
    public class ArrayConverter : IConvertable
    {
        public bool TryGetConverter(Type type, out IConvertable convertable)
        {
            if (type.IsArray)
            {
                convertable = this;
                return true;
            }
            
            convertable = default;
            return false;
        }
        
        public void OnSave(object data, SaveDataHandler saveDataHandler)
        {
            var saveElements = new List<object>();
            var index = 0;
            foreach (var obj in (Array)data)
            {
                var savable = saveDataHandler.ToReferencableObject(index.ToString(), obj);
                saveElements.Add(savable);
                index++;
            }
            saveDataHandler.AddSerializable("elements", saveElements);
            
            var containedType = data.GetType().GetElementType();
            saveDataHandler.AddSerializable("type", containedType);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            var loadElements = loadDataHandler.GetSaveElement<List<object>>("elements");
            var type = loadDataHandler.GetSaveElement<Type>("type");
            
            var array = Array.CreateInstance(type, loadElements.Count);
            
            loadDataHandler.InitializeInstance(array);

            for (var index = 0; index < loadElements.Count; index++)
            {
                var innerScopeIndex = index;
                loadDataHandler.EnqueueReferenceBuilding(loadElements[index], targetObject => array.SetValue(targetObject, innerScopeIndex));
            }
        }
    }
}
