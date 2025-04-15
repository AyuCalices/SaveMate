using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace SaveMate.Core.StateSnapshot.Converter.Collections
{
    [UsedImplicitly]
    internal class ArrayConverter<T> : BaseSaveMateConverter<T>
    {
        protected override void OnCaptureState(T data, CreateSnapshotHandler createSnapshotHandler)
        {
            if (data is not Array array) throw new ArgumentException("Data must be an array.");
            
            // OnCaptureState the rank (number of dimensions)
            createSnapshotHandler.Save("rank", array.Rank);
            
            // OnCaptureState the lengths of each dimension
            for (int i = 0; i < array.Rank; i++)
            {
                createSnapshotHandler.Save($"dimension_{i}", array.GetLength(i));
            }

            // OnCaptureState each element of the array using its indices as a key
            foreach (var indices in GetIndices(array))
            {
                var key = string.Join(",", indices); // Create a string key from indices
                createSnapshotHandler.Save(key, array.GetValue(indices));
            }
        }

        protected override T OnCreateStateObject(RestoreSnapshotHandler restoreSnapshotHandler)
        {
            var elementType = typeof(T).GetElementType();
            if (elementType == null) throw new ArgumentException("T must be an array type.");
            
            // OnRestoreState the rank and dimensions
            restoreSnapshotHandler.TryLoad("rank", out int rank);
            var dimensions = new int[rank];
            for (int i = 0; i < rank; i++)
            {
                restoreSnapshotHandler.TryLoad($"dimension_{i}", out dimensions[i]);
            }

            // Create the array with the loaded dimensions
            return (T)(object)Array.CreateInstance(elementType, dimensions);
        }

        protected override void OnRestoreState(T input, RestoreSnapshotHandler restoreSnapshotHandler)
        {
            var elementType = typeof(T).GetElementType();
            if (elementType == null) throw new ArgumentException("T must be an array type.");
            
            if (input is not Array array) throw new ArgumentException("Data must be an array.");
            
            // OnRestoreState each element using its indices as the key
            foreach (var indices in GetIndices(array))
            {
                var key = string.Join(",", indices); // Use the same key format
                if (restoreSnapshotHandler.TryLoad(elementType, key, out var value))
                {
                    array.SetValue(value, indices);
                }
            }
        }

        private IEnumerable<int[]> GetIndices(Array array)
        {
            var dimensions = Enumerable.Range(0, array.Rank).Select(array.GetLength).ToArray();
            var indices = new int[array.Rank];
            while (true)
            {
                yield return (int[])indices.Clone();
                for (int dim = array.Rank - 1; dim >= 0; dim--)
                {
                    if (++indices[dim] < dimensions[dim]) break;
                    indices[dim] = 0;
                    if (dim == 0) yield break;
                }
            }
        }
    }
}
