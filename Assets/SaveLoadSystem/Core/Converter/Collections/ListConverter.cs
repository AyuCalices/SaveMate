using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class ListConverter : BaseConverter<IList>
    {
        protected override void OnSave(IList data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Count);
            
            var typeString = data.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
            saveDataHandler.SaveAsValue("type", typeString);
            
            for (var index = 0; index < data.Count; index++)
            {
                saveDataHandler.Save(index.ToString(), data[index]);
            }
        }

        protected override IList OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoadValue("type", out string typeString);
            
            var listType = typeof(List<>).MakeGenericType(Type.GetType(typeString));
            return (IList)Activator.CreateInstance(listType);
        }

        protected override void OnLoad(IList data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoadValue("count", out int count);

            var type = data.GetType().GetGenericArguments()[0];
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad(type ,index.ToString(), out var obj))
                {
                    data.Add(obj);
                }
            }
        }
    }
}
