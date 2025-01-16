using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class QueueConverter : BaseConverter<Queue>
    {
        protected override void OnSave(Queue data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Count);
            
            var typeString = data.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
            saveDataHandler.SaveAsValue("type", typeString);
            
            var saveElements = data.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveDataHandler.Save(index.ToString(), saveElements[index]);
            }
        }

        protected override Queue OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoadValue("type", out string typeString);
            
            var queueType = typeof(Queue<>).MakeGenericType(Type.GetType(typeString));
            return (Queue)Activator.CreateInstance(queueType);
        }

        protected override void OnLoad(Queue data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoadValue("count", out int count);
            
            var type = data.GetType().GetGenericArguments()[0];
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad(type, index.ToString(), out var targetObject))
                {
                    data.Enqueue(targetObject);
                }
            }
        }
    }
}
