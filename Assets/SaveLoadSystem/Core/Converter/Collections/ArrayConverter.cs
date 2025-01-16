using System;

namespace SaveLoadSystem.Core.Converter.Collections
{
    //TODO: multi-dimensional Arrays
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
            
            var typeString = data.GetType().GetElementType()?.AssemblyQualifiedName;
            saveDataHandler.SaveAsValue("type", typeString);
            
            var index = 0;
            foreach (var obj in (Array)data)
            {
                saveDataHandler.Save(index.ToString(), obj);
                index++;
            }
        }

        public object CreateInstanceForLoad(SimpleLoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoadValue("count", out int count);
            loadDataHandler.TryLoadValue("type", out string typeString);
            
            return Array.CreateInstance(Type.GetType(typeString), count);
        }

        public void OnLoad(object data, LoadDataHandler loadDataHandler)
        {
            var arrayData = (Array)data;
            
            for (int index = 0; index < arrayData.Length; index++)
            {
                var innerScopeIndex = index;
                if (loadDataHandler.TryLoad(arrayData.GetType().GetElementType(), index.ToString(), out var obj))
                {
                    arrayData.SetValue(obj, innerScopeIndex);
                }
            }
        }
    }
}
