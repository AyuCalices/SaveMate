using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveMate.Runtime.Core.StateSnapshot.Converter.Collections
{
    [UsedImplicitly]
    internal class ListConverter<T> : BaseSaveMateConverter<List<T>>
    {
        protected override void OnCaptureState(List<T> data, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("count", data.Count);
            
            for (var index = 0; index < data.Count; index++)
            {
                createSnapshotHandler.Save(index.ToString(), data[index]);
            }
        }

        protected override List<T> OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new List<T>();
        }

        protected override void OnRestoreState(List<T> input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad("count", out int count);
            
            for (var index = 0; index < count; index++)
            {
                if (restoreSnapshotHandler.TryLoad<T>(index.ToString(), out var obj))
                {
                    input.Add(obj);
                }
            }
        }
    }
}
