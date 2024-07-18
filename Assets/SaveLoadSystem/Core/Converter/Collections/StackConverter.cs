using System;
using System.Collections;
using System.Collections.Generic;

namespace SaveLoadSystem.Core.Converter.Collections
{
    public class StackConverter : BaseConverter<Stack>
    {
        protected override void OnSave(Stack data, SaveDataHandler saveDataHandler)
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
            
            var stackType = typeof(Stack<>).MakeGenericType(type);
            var stack = (Stack)Activator.CreateInstance(stackType);
            
            loadDataHandler.InitializeInstance(stack);
            
            foreach (var saveElement in loadElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, foundObject => stack.Push(foundObject));
            }
        }
    }
}
