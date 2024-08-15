using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.DataTransferObject;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class ArrayConverter : IConvertable
    {
        public bool CanConvert(Type type, out IConvertable convertable)
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
            saveDataHandler.SaveAsValue("count", ((Array)data).Length);
            var index = 0;
            foreach (var obj in (Array)data)
            {
                saveDataHandler.TrySaveAsReferencable(index.ToString(), obj);
                index++;
            }
            
            var typeString = data.GetType().GetElementType()?.AssemblyQualifiedName;
            saveDataHandler.SaveAsValue("type", typeString);
        }

        public object OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.LoadValue<int>("count");
            var loadElements = new List<GuidPath>();
            for (int index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoadReferencable(index.ToString(), out var obj))
                {
                    loadElements.Add(obj);
                }
            }
            
            var typeString = loadDataHandler.LoadValue<string>("type");
            var array = Array.CreateInstance(Type.GetType(typeString), loadElements.Count);

            for (var index = 0; index < loadElements.Count; index++)
            {
                var innerScopeIndex = index;
                loadDataHandler.EnqueueReferenceBuilding(loadElements[index], targetObject => array.SetValue(targetObject, innerScopeIndex));
            }

            return array;
        }
    }
}
