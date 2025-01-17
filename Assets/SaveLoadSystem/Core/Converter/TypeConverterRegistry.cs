using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaveLoadSystem.Core.Converter.Collections;
using SaveLoadSystem.Core.Converter.UnityTypes;
using UnityEngine;

namespace SaveLoadSystem.Core.Converter
{
    public static class ConverterServiceProvider
    {
        private static readonly HashSet<(Type Type, IEnumerable<Type> Interfaces)> UsableConverterLookup = new();
        private static readonly Dictionary<Type, object> CreatedConverterLookup = new();

        static ConverterServiceProvider()
        {
            //register all types that inherit from IConverter<>
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in allTypes)
            {
                // Look for classes implementing Converter<T> where T matches targetType
                
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConverter<>));
                
                UsableConverterLookup.Add((type, interfaces));
            }
        }
        
        public static bool ExistsAndCreate<T>()
        {
            var targetType = typeof(T);

            // Check if the converter already exists in the lookup
            if (CreatedConverterLookup.ContainsKey(targetType))
            {
                return true;
            }

            // Discover all types implementing Converter<>
            var converterType = FindConverterType(targetType);
            
            if (converterType == null)
            {
                return false;
            }
            
            // Create the converter dynamically and cache it
            var instance = Activator.CreateInstance(converterType);
            CreatedConverterLookup[targetType] = instance;
            return true;
        }
        
        public static IConverter<T> GetConverter<T>()
        {
            var targetType = typeof(T);

            // Check if the converter already exists in the lookup
            if (CreatedConverterLookup.TryGetValue(targetType, out var converter))
            {
                return (IConverter<T>)converter;
            }

            // Discover all types implementing Converter<>
            var converterType = FindConverterType(targetType);

            if (converterType == null)
            {
                throw new NotSupportedException($"No converter found or supported for type {targetType.FullName}");
            }

            // Create the converter dynamically and cache it
            var instance = Activator.CreateInstance(converterType);
            CreatedConverterLookup[targetType] = instance;
            return (IConverter<T>)instance;
        }

        private static Type FindConverterType(Type targetType)
        {
            foreach (var usableConverter in UsableConverterLookup)
            {
                foreach (var converterInterface in usableConverter.Interfaces)
                {
                    var genericArgument = converterInterface.GetGenericArguments()[0];

                    // Match open generic types
                    //TODO: why was a string called here?
                    if (genericArgument.IsGenericType && targetType.IsGenericType && genericArgument.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition())
                    {
                        // Construct the type with the target's type arguments
                        return usableConverter.Type.MakeGenericType(targetType.GetGenericArguments());
                    }

                    // Match non-generic types
                    if (genericArgument == targetType)
                    {
                        return usableConverter.Type;
                    }
                }
            }
            
            return null;
        }
    }

    public interface IConverter<T>
    {
        void Save(T data, SaveDataHandler saveDataHandler);

        T Load(LoadDataHandler loadDataHandler);
    }

    public class ListConverter<T> : IConverter<List<T>>
    {
        public void Save(List<T> data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.SaveAsValue("count", data.Count);
            
            for (var index = 0; index < data.Count; index++)
            {
                saveDataHandler.Save(index.ToString(), data[index]);
            }
        }

        public List<T> Load(LoadDataHandler loadDataHandler)
        {
            var list = new List<T>();
            
            loadDataHandler.TryLoadValue("count", out int count);
            
            for (var index = 0; index < count; index++)
            {
                if (loadDataHandler.TryLoad<T>(index.ToString(), out var obj))
                {
                    list.Add(obj);
                }
            }

            return list;
        }
    }
    
    public static class TypeConverterRegistry
    {
        private static readonly List<IConvertable> Factories = new();

        static TypeConverterRegistry()
        {
            //collections
            Factories.Add(new ArrayConverter());    //array must be processed before list, due to both inheriting from IList
            Factories.Add(new ListConverter());
            Factories.Add(new DictionaryConverter());
            Factories.Add(new StackConverter());
            Factories.Add(new QueueConverter());
            
            //unity types
            Factories.Add(new Color32Converter());
            Factories.Add(new ColorConverter());
            Factories.Add(new Vector2Converter());
            Factories.Add(new Vector3Converter());
            Factories.Add(new Vector4Converter());
            Factories.Add(new QuaternionConverter());
            
            //add your own converter here
            //Factories.Add(new ItemConverter());   //enable this for Type-Converter save-strategy and disable Attribute- and Component-Saving from item
        }

        public static bool HasConverter(Type type)
        {
            return Factories.Any(factory => factory.CanConvert(type, out _));
        }

        public static IConvertable GetConverter(Type type)
        {
            foreach (var factory in Factories)
            {
                if (factory.CanConvert(type, out IConvertable convertable))
                {
                    return convertable;
                }
            }
            return null;
        }

        public static bool TryGetConverter(Type type, out IConvertable convertable)
        {
            foreach (var factory in Factories)
            {
                if (factory.CanConvert(type, out convertable))
                {
                    return true;
                }
            }
            convertable = default;
            return false;
        }
    }
}
