using System.Collections.Generic;
using JetBrains.Annotations;

namespace SaveMate.Runtime.Core.StateSnapshot.Converter.Collections
{
    [UsedImplicitly]
    internal class DictionaryConverter<TKey, TValue> : BaseSaveMateConverter<Dictionary<TKey, TValue>>
    {
        protected override void OnCaptureState(Dictionary<TKey, TValue> input, CreateSnapshotHandler createSnapshotHandler)
        {
            createSnapshotHandler.Save("count", input.Count);
            
            var index = 0;
            foreach (var dataKey in input.Keys)
            {
                createSnapshotHandler.Save("key" + index, dataKey);
                createSnapshotHandler.Save("value" + index, input[dataKey]);
                index++;
            }
        }

        protected override Dictionary<TKey, TValue> OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            return new Dictionary<TKey, TValue>();
        }

        protected override void OnRestoreState(Dictionary<TKey, TValue> input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            restoreSnapshotHandler.TryLoad<int>("count", out var count);
            
            for (var index = 0; index < count; index++)
            {
                if (restoreSnapshotHandler.TryLoad<TKey>("key" + index, out var key)
                    && restoreSnapshotHandler.TryLoad<TValue>("value" + index, out var value))
                {
                    input.Add(key, value);
                }
            }
        }
    }
}
