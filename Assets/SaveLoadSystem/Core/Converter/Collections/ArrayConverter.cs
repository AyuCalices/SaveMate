using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace SaveLoadSystem.Core.Converter.Collections
{
    /*
    public class ArrayConverter<T> : IConverter<T[]>
    {
        public void Save(T[] data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("length", data.Length);

            for (int i = 0; i < data.Length; i++)
            {
                saveDataHandler.Save(i.ToString(), data[i]);
            }
        }

        public T[] Load(LoadDataHandler loadDataHandler)
        {
            if (!loadDataHandler.TryLoadValue("length", out int length))
            {
                return new T[0];
            }

            var array = new T[length];
            for (int i = 0; i < length; i++)
            {
                if (loadDataHandler.TryLoad<T>(i.ToString(), out var element))
                {
                    array[i] = element;
                }
            }

            return array;
        }
    }*/
    
    
    [UsedImplicitly]
    public class ArrayConverter<T> : SaveMateBaseConverter<T>
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

        protected override T OnLoad(LoadDataHandler loadDataHandler)
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
            var array = Array.CreateInstance(elementType, dimensions);

            // Load each element using its indices as the key
            foreach (var indices in GetIndices(array))
            {
                var key = string.Join(",", indices); // Use the same key format
                if (loadDataHandler.TryLoad(elementType, key, out var value))
                {
                    array.SetValue(value, indices);
                }
            }

            return (T)(object)array;
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
