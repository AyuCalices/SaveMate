using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadCore.Core.Converter.Collections;
using SaveLoadCore.Core.Converter.UnityTypes;

namespace SaveLoadCore.Core.Converter
{
    public static class ConverterRegistry
    {
        private static readonly List<IConvertable> Factories = new();

        static ConverterRegistry()
        {
            //collections
            Factories.Add(new ArrayConverter());    //array must be processed before list, because an array inherits from IList but needs an own converter
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
        }

        public static bool HasConverter(Type type)
        {
            return Factories.Any(factory => factory.TryGetConverter(type, out _));
        }

        public static IConvertable GetConverter(Type type)
        {
            foreach (var factory in Factories)
            {
                if (factory.TryGetConverter(type, out IConvertable convertable))
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
                if (factory.TryGetConverter(type, out convertable))
                {
                    return true;
                }
            }
            convertable = default;
            return false;
        }
    }
}
