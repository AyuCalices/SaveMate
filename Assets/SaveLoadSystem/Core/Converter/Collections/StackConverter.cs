using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class StackConverter : BaseConverter<Stack>
    {
        protected override void OnSave(Stack data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("count", data.Count);
            
            var saveElements = data.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveDataHandler.TryAddReferencable(index.ToString(), saveElements[index]);
            }
            
            var containedType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("type", containedType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var count = loadDataHandler.GetSerializable<int>("count");
            var loadElements = new List<object>();
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryGetReferencable(index.ToString(), out var obj))
                {
                    loadElements.Add(obj);
                }
            }
            
            var type = loadDataHandler.GetSerializable<Type>("type");
            var stackType = typeof(Stack<>).MakeGenericType(type);
            var stack = (Stack)Activator.CreateInstance(stackType);
            
            loadDataHandler.InitializeInstance(stack);
            
            foreach (var loadElement in loadElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(loadElement, foundObject => stack.Push(foundObject));
            }
        }
    }
}
