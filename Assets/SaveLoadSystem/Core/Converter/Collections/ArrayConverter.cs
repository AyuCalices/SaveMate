using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    [UsedImplicitly]
    public class ArrayConverter<T> : IConverter<Array>
    {
        public void Save(Array data, SaveDataHandler saveDataHandler)
        {
            // Save the rank (number of dimensions)
            saveDataHandler.SaveAsValue("rank", data.Rank);
            
            // Save the lengths of each dimension
            for (int i = 0; i < data.Rank; i++)
            {
                saveDataHandler.SaveAsValue($"dimension_{i}", data.GetLength(i));
            }

            // Save each element of the array using its indices as a key
            foreach (var indices in GetIndices(data))
            {
                var key = string.Join(",", indices); // Create a string key from indices
                saveDataHandler.Save(key, data.GetValue(indices));
            }
        }

        public Array Load(LoadDataHandler loadDataHandler)
        {
            // Load the rank and dimensions
            loadDataHandler.TryLoadValue("rank", out int rank);
            var dimensions = new int[rank];
            for (int i = 0; i < rank; i++)
            {
                loadDataHandler.TryLoadValue($"dimension_{i}", out dimensions[i]);
            }

            // Create the array with the loaded dimensions
            var array = Array.CreateInstance(typeof(T), dimensions);

            // Load each element using its indices as the key
            foreach (var indices in GetIndices(array))
            {
                var key = string.Join(",", indices); // Use the same key format
                if (loadDataHandler.TryLoad<T>(key, out var value))
                {
                    array.SetValue(value, indices);
                }
            }

            return array;
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
