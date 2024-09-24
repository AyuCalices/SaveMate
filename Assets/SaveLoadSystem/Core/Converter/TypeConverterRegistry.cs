using System;
using System.Collections.Generic;
using System.Linq;
using SaveLoadSystem.Core.Converter.Collections;
using SaveLoadSystem.Core.Converter.UnityTypes;

namespace SaveLoadSystem.Core.Converter
{
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
