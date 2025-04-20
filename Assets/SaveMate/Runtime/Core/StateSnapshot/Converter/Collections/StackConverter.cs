using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveMate.Runtime.Core.StateSnapshot.Converter.Collections
{
    [UsedImplicitly]
    internal class StackConverter<T> : BaseSaveMateConverter<Stack<T>>
    {
        protected override void OnCaptureState(Stack<T> input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("count", input.Count);
            
            var saveElements = input.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                createSnapshotHandler.Save(index.ToString(), saveElements[index]);
            }
        }

        protected override Stack<T> OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Stack<T>();
        }

        protected override void OnRestoreState(Stack<T> input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("count", out int count);
            
            for (var index = 0; index < count; index++)
            {
                if (restoreSnapshotHandler.TryLoad<T>(index.ToString(), out var obj))
                {
                    input.Push(obj);
                }
            }
        }
    }
}
