using System;
using System.Collections;
using System.Collections.Generic;
using SaveLoadSystem.Core.SerializableTypes;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class QueueConverter : BaseConverter<Queue>
    {
        protected override void OnSave(Queue data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("count", data.Count);
            
            var saveElements = data.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveDataHandler.TryAddReferencable(index.ToString(), saveElements[index]);
            }
            
            var typeString = data.GetType().GetGenericArguments()[0].AssemblyQualifiedName;
            saveDataHandler.AddSerializable("type", typeString);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.GetSerializable<int>("count");
            var loadElements = new List<GuidPath>();
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryGetReferencable(index.ToString(), out var obj))
                {
                    loadElements.Add(obj);
                }
            }
            
            var typeString = loadDataHandler.GetSerializable<string>("type");
            var queueType = typeof(Queue<>).MakeGenericType(Type.GetType(typeString));
            var queue = (Queue)Activator.CreateInstance(queueType);
            
            loadDataHandler.InitializeInstance(queue);
            
            foreach (var saveElement in loadElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, targetObject => queue.Enqueue(targetObject));
            }
        }
    }
}
