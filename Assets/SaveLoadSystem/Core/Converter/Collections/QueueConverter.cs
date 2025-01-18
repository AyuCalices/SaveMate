using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class QueueConverter<T> : IConverter<Queue<T>>
    {
        public void Save(Queue<T> data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Count);
            
            var saveElements = data.ToArray();
            
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveDataHandler.Save(index.ToString(), saveElements[index]);
            }
        }

        public Queue<T> Load(LoadDataHandler loadDataHandler)
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
