using System.Collections.Generic;
using JetBrains.Annotations;
using SaveMate.Core.StateSnapshot.SnapshotHandler;

namespace SaveMate.Core.StateSnapshot.Converter.Collections
{
    [UsedImplicitly]
    internal class QueueConverter<T> : BaseSaveMateConverter<Queue<T>>
    {
        protected override void OnCaptureState(Queue<T> input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("count", input.Count);
            
            var saveElements = input.ToArray();
            
            for (var index = 0; index < saveElements.Length; index++)
            {
                createSnapshotHandler.Save(index.ToString(), saveElements[index]);
            }
        }

        protected override Queue<T> OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Queue<T>();
        }

        protected override void OnRestoreState(Queue<T> input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("count", out int count);
            
            for (var index = 0; index < count; index++)
            {
                if (restoreSnapshotHandler.TryLoad<T>( index.ToString(), out var targetObject))
                {
                    input.Enqueue(targetObject);
                }
            }
        }
    }
}
