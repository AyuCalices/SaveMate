using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class StackConverter<T> : IConverter<Stack<T>>
    {
        public void Save(Stack<T> data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("count", data.Count);
            
            var saveElements = data.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveDataHandler.Save(index.ToString(), saveElements[index]);
            }
        }

        public Stack<T> Load(LoadDataHandler loadDataHandler)
        {
            var stack = new Stack<T>();

            loadDataHandler.TryLoadValue("count", out int count);
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad<T>(index.ToString(), out var obj))
                {
                    stack.Push(obj);
                }
            }

            return stack;
        }
    }
}
