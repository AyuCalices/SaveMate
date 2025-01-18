using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class StackConverter<T> : SaveMateBaseConverter<Stack<T>>
    {
        protected override void OnSave(Stack<T> input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.Save("count", input.Count);
            
            var saveElements = input.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveDataHandler.Save(index.ToString(), saveElements[index]);
            }
        }

        protected override Stack<T> OnLoad(LoadDataHandler loadDataHandler)
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
