using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class ArrayConverter<T> : BaseConverter<T>
    {
        protected override void OnSave(T data, SaveDataHandler saveDataHandler)
        {
            if (data is not Array array) throw new ArgumentException("Data must be an array.");
            
            // Save the rank (number of dimensions)
            saveDataHandler.SaveAsValue("rank", array.Rank);
            
            // Save the lengths of each dimension
            for (int i = 0; i < array.Rank; i++)
            {
                saveDataHandler.SaveAsValue($"dimension_{i}", array.GetLength(i));
            }

            // Save each element of the array using its indices as a key
            foreach (var indices in GetIndices(array))
            {
                var key = string.Join(",", indices); // Create a string key from indices
                saveDataHandler.Save(key, array.GetValue(indices));
            }
        }

        protected override T OnCreateInstanceForLoad(LoadDataHandler loadDataHandler)
        {
            var elementType = typeof(T).GetElementType();
            if (elementType == null) throw new ArgumentException("T must be an array type.");
            
            // Load the rank and dimensions
            loadDataHandler.TryLoadValue("rank", out int rank);
            var dimensions = new int[rank];
            for (int i = 0; i < rank; i++)
            {
                loadDataHandler.TryLoadValue($"dimension_{i}", out dimensions[i]);
            }

            // Create the array with the loaded dimensions
            return (T)(object)Array.CreateInstance(elementType, dimensions);
        }

        protected override void OnLoad(T input, LoadDataHandler loadDataHandler)
        {
            var elementType = typeof(T).GetElementType();
            if (elementType == null) throw new ArgumentException("T must be an array type.");
            
            if (input is not Array array) throw new ArgumentException("Data must be an array.");
            
            // Load each element using its indices as the key
            foreach (var indices in GetIndices(array))
            {
                var key = string.Join(",", indices); // Use the same key format
                if (loadDataHandler.TryLoad(elementType, key, out var value))
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
