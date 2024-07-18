using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadCore.Core.Converter.Collections
{
    public class QueueConverter : BaseConverter<Queue>
    {
        protected override void OnSave(Queue data, SaveDataHandler saveDataHandler)
        {
            var saveElements = data.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveElements[index] = saveDataHandler.ToReferencableObject(index.ToString(), saveElements[index]);
            }
            saveDataHandler.AddSerializable("elements", saveElements);
            
            var containedType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("type", containedType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var loadElements = loadDataHandler.GetSaveElement<List<object>>("elements");
            var type = loadDataHandler.GetSaveElement<Type>("type");
            
            var queueType = typeof(Queue<>).MakeGenericType(type);
            var queue = (Queue)Activator.CreateInstance(queueType);
            
            loadDataHandler.InitializeInstance(queue);
            
            foreach (var saveElement in loadElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, targetObject => queue.Enqueue(targetObject));
            }
        }
    }
}
