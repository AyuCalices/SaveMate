using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SaveLoadSystem.Core.Converter.Collections;

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
            // Handle array types specifically (for any dimension)
            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType();
                var arrayConverterType = typeof(ArrayConverter<>).MakeGenericType(elementType);
                return arrayConverterType;
            }
            
            // Handle all other types
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
}
