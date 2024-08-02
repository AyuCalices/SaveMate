using System;
using System.Collections.Generic;
using SaveLoadSystem.Core.DataTransferObject;

namespace SaveLoadSystem.Core.Converter.Collections
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
            saveDataHandler.AddSerializable("count", ((Array)data).Length);
            var index = 0;
            foreach (var obj in (Array)data)
            {
                saveDataHandler.TryAddReferencable(index.ToString(), obj);
                index++;
            }
            
            var typeString = data.GetType().GetElementType()?.AssemblyQualifiedName;
            saveDataHandler.AddSerializable("type", typeString);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.GetSerializable<int>("count");
            var loadElements = new List<GuidPath>();
            for (int index = 0; index < count; index++)
            {
                if (loadDataHandler.TryGetReferencable(index.ToString(), out var obj))
                {
                    loadElements.Add(obj);
                }
            }
            
            var typeString = loadDataHandler.GetSerializable<string>("type");
            var array = Array.CreateInstance(Type.GetType(typeString), loadElements.Count);
            
            loadDataHandler.InitializeInstance(array);

            for (var index = 0; index < loadElements.Count; index++)
            {
                var innerScopeIndex = index;
                loadDataHandler.EnqueueReferenceBuilding(loadElements[index], targetObject => array.SetValue(targetObject, innerScopeIndex));
            }
        }
    }
}
