using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class QueueConverter<T> : SaveMateBaseConverter<Queue<T>>
    {
        protected override void OnSave(Queue<T> input, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", input.Count);
            
            var saveElements = input.ToArray();
            
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveDataHandler.Save(index.ToString(), saveElements[index]);
            }
        }

        protected override Queue<T> OnLoad(LoadDataHandler loadDataHandler)
        {
            var queue = new Queue<T>();
            
            loadDataHandler.TryLoadValue("count", out int count);
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad<T>( index.ToString(), out var targetObject))
                {
                    queue.Enqueue(targetObject);
                }
            }

            return queue;
        }
    }
}
