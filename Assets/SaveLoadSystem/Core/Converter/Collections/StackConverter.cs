using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class StackConverter : BaseConverter<Stack>
    {
        protected override void OnSave(Stack data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("count", data.Count);
            
            var saveElements = data.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveDataHandler.Save(index.ToString(), saveElements[index]);
            }
            
            var typeString = data.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
            saveDataHandler.Save("type", typeString);
        }

        protected override Stack OnCreateInstanceForLoading(SimpleLoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoadValue("type", out string typeString);
            
            var stackType = typeof(Stack<>).MakeGenericType(Type.GetType(typeString));
            return (Stack)Activator.CreateInstance(stackType);
        }

        protected override void OnLoad(Stack data, LoadDataHandler loadDataHandler)
        {
            loadDataHandler.TryLoadValue("count", out int count);
            
            var type = data.GetType().GetGenericArguments()[0];
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad(type, index.ToString(), out var obj))
                {
                    data.Push(obj);
                }
            }
        }
    }
}
