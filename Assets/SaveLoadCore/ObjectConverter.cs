using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadCore
{
    public static class ConverterFactoryRegistry
    {
        private static readonly List<IConverterFactory> Factories = new();

        static ConverterFactoryRegistry()
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

    public interface IConvertable
    {
        void OnSave(object data, SerializeReferenceBuilder serializeReferenceBuilder);
        object OnLoad(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder);
    }
    
    public interface IConverterFactory
    {
        bool TryGetConverter(Type type, out IConvertable convertable);
    }

    /// <summary>
    /// Doesn't support references by choice
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseConverter<T> : IConvertable, IConverterFactory
    {
        public virtual bool TryGetConverter(Type type, out IConvertable convertable)
        {
            if (typeof(T).IsAssignableFrom(type) || type is T)
            {
                convertable = this;
                return true;
            }
            
            convertable = default;
            return false;
        }
        
        public void OnSave(object data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            SerializeData((T)data, serializeReferenceBuilder);
        }

        protected abstract void SerializeData(T data, SerializeReferenceBuilder serializeReferenceBuilder);

        public object OnLoad(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            return DeserializeData(loadDataBuffer, deserializeReferenceBuilder);
        }
        
        protected abstract T DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder);
    }
    
    [UsedImplicitly]
    public class StackConverter : BaseConverter<Stack>
    {
        protected override void SerializeData(Stack data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            var listElements = data.ToArray();
            for (var index = 0; index < listElements.Length; index++)
            {
                listElements[index] = serializeReferenceBuilder.ToSavableObject(index.ToString(), listElements[index]);
            }
            serializeReferenceBuilder.AddSerializable("elements", listElements);
            
            var containedType = data.GetType().GetGenericArguments()[0];
            serializeReferenceBuilder.AddSerializable("type", containedType);
        }

        protected override Stack DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var saveElements = (List<object>)loadDataBuffer.SaveElements["elements"];
            var type = (Type)loadDataBuffer.SaveElements["type"];
            
            var listType = typeof(Stack<>).MakeGenericType(type);
            var list = (Stack)Activator.CreateInstance(listType);
            foreach (var saveElement in saveElements)
            {
                deserializeReferenceBuilder.EnqueueAction(saveElement, targetObject => list.Push(targetObject));
            }

            return list;
        }
    }
    
    [UsedImplicitly]
    public class QueueConverter : BaseConverter<Queue>
    {
        protected override void SerializeData(Queue data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            var listElements = data.ToArray();
            for (var index = 0; index < listElements.Length; index++)
            {
                listElements[index] = serializeReferenceBuilder.ToSavableObject(index.ToString(), listElements[index]);
            }
            serializeReferenceBuilder.AddSerializable("elements", listElements);
            
            var containedType = data.GetType().GetGenericArguments()[0];
            serializeReferenceBuilder.AddSerializable("type", containedType);
        }

        protected override Queue DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var saveElements = (List<object>)loadDataBuffer.SaveElements["elements"];
            var type = (Type)loadDataBuffer.SaveElements["type"];
            
            var listType = typeof(Queue<>).MakeGenericType(type);
            var list = (Queue)Activator.CreateInstance(listType);
            foreach (var saveElement in saveElements)
            {
                deserializeReferenceBuilder.EnqueueAction(saveElement, targetObject => list.Enqueue(targetObject));
            }

            return list;
        }
    }
    
    [UsedImplicitly]
    public class ArrayConverter : IConvertable, IConverterFactory
    {
        public bool TryGetConverter(Type type, out IConvertable convertable)
        {
            if (type.IsArray)
            {
                convertable = this;
                return true;
            }
            
            convertable = default;
            return false;
        }
        
        public void OnSave(object data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            var listElements = new List<object>();
            var index = 0;
            foreach (var obj in (Array)data)
            {
                var savable = serializeReferenceBuilder.ToSavableObject(index.ToString(), obj);
                listElements.Add(savable);
                index++;
            }
            serializeReferenceBuilder.AddSerializable("elements", listElements);
            
            var containedType = data.GetType().GetElementType();
            serializeReferenceBuilder.AddSerializable("type", containedType);
        }

        public object OnLoad(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var saveElements = (List<object>)loadDataBuffer.SaveElements["elements"];
            var type = (Type)loadDataBuffer.SaveElements["type"];
            
            var array = Array.CreateInstance(type, saveElements.Count);

            for (var index = 0; index < saveElements.Count; index++)
            {
                var innerScopeIndex = index;
                deserializeReferenceBuilder.EnqueueAction(saveElements[index], targetObject => array.SetValue(targetObject, innerScopeIndex));
            }

            return array;
        }
    }

    [UsedImplicitly]
    public class ListConverter : BaseConverter<IList>
    {
        protected override void SerializeData(IList data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            var listElements = new List<object>();
            for (var index = 0; index < data.Count; index++)
            {
                var savable = serializeReferenceBuilder.ToSavableObject(index.ToString(), data[index]);
                listElements.Add(savable);
            }
            serializeReferenceBuilder.AddSerializable("elements", listElements);
            
            var containedType = data.GetType().GetGenericArguments()[0];
            serializeReferenceBuilder.AddSerializable("type", containedType);
        }

        protected override IList DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var saveElements = (List<object>)loadDataBuffer.SaveElements["elements"];
            var type = (Type)loadDataBuffer.SaveElements["type"];
            
            var listType = typeof(List<>).MakeGenericType(type);
            var list = (IList)Activator.CreateInstance(listType);
            foreach (var saveElement in saveElements)
            {
                deserializeReferenceBuilder.EnqueueAction(saveElement, targetObject => list.Add(targetObject));
            }

            return list;
        }
    }
    
    [UsedImplicitly]
    public class DictionaryConverter : BaseConverter<IDictionary>
    {
        protected override void SerializeData(IDictionary data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            var listElements = new Dictionary<object, object>();

            var index = 0;
            foreach (var dataKey in data.Keys)
            {
                var savableKey = serializeReferenceBuilder.ToSavableObject(index.ToString(), dataKey);
                var savableValue = serializeReferenceBuilder.ToSavableObject(index.ToString(), data[dataKey]);
                listElements.Add(savableKey, savableValue);
                
                index++;
            }
            serializeReferenceBuilder.AddSerializable("elements", listElements);
            
            var keyType = data.GetType().GetGenericArguments()[0];
            serializeReferenceBuilder.AddSerializable("keyType", keyType);
            
            var valueType = data.GetType().GetGenericArguments()[1];
            serializeReferenceBuilder.AddSerializable("valueType", valueType);
        }

        protected override IDictionary DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var saveElements = (Dictionary<object, object>)loadDataBuffer.SaveElements["elements"];
            
            //the activator will always intialize value types with default values
            var keyType = (Type)loadDataBuffer.SaveElements["keyType"];
            var valueType = (Type)loadDataBuffer.SaveElements["valueType"];
            
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);

            foreach (var (key, value) in saveElements)
            {
                var objectGroup = new[] { key, value };
                deserializeReferenceBuilder.EnqueueAction(objectGroup, targetObject =>
                {
                    dictionary.Add(targetObject[0], targetObject[1]);
                });
            }

            return dictionary;
        }
    }
    
    [UsedImplicitly]
    public class Color32Converter : BaseConverter<Color32>
    {
        protected override void SerializeData(Color32 data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            serializeReferenceBuilder.AddSerializable("r", data.r);
            serializeReferenceBuilder.AddSerializable("g", data.g);
            serializeReferenceBuilder.AddSerializable("b", data.b);
            serializeReferenceBuilder.AddSerializable("a", data.a);
        }

        protected override Color32 DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var r = (byte)loadDataBuffer.SaveElements["r"];
            var g = (byte)loadDataBuffer.SaveElements["g"];
            var b = (byte)loadDataBuffer.SaveElements["b"];
            var a = (byte)loadDataBuffer.SaveElements["a"];

            return new Color32(r, g, b, a);
        }
    }
    
    [UsedImplicitly]
    public class ColorConverter : BaseConverter<Color>
    {
        protected override void SerializeData(Color data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            serializeReferenceBuilder.AddSerializable("r", data.r);
            serializeReferenceBuilder.AddSerializable("g", data.g);
            serializeReferenceBuilder.AddSerializable("b", data.b);
            serializeReferenceBuilder.AddSerializable("a", data.a);
        }

        protected override Color DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var r = (float)loadDataBuffer.SaveElements["r"];
            var g = (float)loadDataBuffer.SaveElements["g"];
            var b = (float)loadDataBuffer.SaveElements["b"];
            var a = (float)loadDataBuffer.SaveElements["a"];

            return new Color(r, g, b, a);
        }
    }
    
    [UsedImplicitly]
    public class QuaternionConverter : BaseConverter<Quaternion>
    {
        protected override void SerializeData(Quaternion data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            serializeReferenceBuilder.AddSerializable("x", data.x);
            serializeReferenceBuilder.AddSerializable("y", data.y);
            serializeReferenceBuilder.AddSerializable("z", data.z);
            serializeReferenceBuilder.AddSerializable("w", data.w);
        }

        protected override Quaternion DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var x = (float)loadDataBuffer.SaveElements["x"];
            var y = (float)loadDataBuffer.SaveElements["y"];
            var z = (float)loadDataBuffer.SaveElements["z"];
            var w = (float)loadDataBuffer.SaveElements["w"];

            return new Quaternion(x, y, z, w);
        }
    }
    
    [UsedImplicitly]
    public class Vector4Converter : BaseConverter<Vector4>
    {
        protected override void SerializeData(Vector4 data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            serializeReferenceBuilder.AddSerializable("x", data.x);
            serializeReferenceBuilder.AddSerializable("y", data.y);
            serializeReferenceBuilder.AddSerializable("z", data.z);
            serializeReferenceBuilder.AddSerializable("w", data.w);
        }

        protected override Vector4 DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var x = (float)loadDataBuffer.SaveElements["x"];
            var y = (float)loadDataBuffer.SaveElements["y"];
            var z = (float)loadDataBuffer.SaveElements["z"];
            var w = (float)loadDataBuffer.SaveElements["w"];

            return new Vector4(x, y, z, w);
        }
    }

    [UsedImplicitly]
    public class Vector3Converter : BaseConverter<Vector3>
    {
        protected override void SerializeData(Vector3 data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            serializeReferenceBuilder.AddSerializable("x", data.x);
            serializeReferenceBuilder.AddSerializable("y", data.y);
            serializeReferenceBuilder.AddSerializable("z", data.z);
        }

        protected override Vector3 DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var x = (float)loadDataBuffer.SaveElements["x"];
            var y = (float)loadDataBuffer.SaveElements["y"];
            var z = (float)loadDataBuffer.SaveElements["z"];

            return new Vector3(x, y, z);
        }
    }
    
    [UsedImplicitly]
    public class Vector2Converter : BaseConverter<Vector2>
    {
        protected override void SerializeData(Vector2 data, SerializeReferenceBuilder serializeReferenceBuilder)
        {
            serializeReferenceBuilder.AddSerializable("x", data.x);
            serializeReferenceBuilder.AddSerializable("y", data.y);
        }

        protected override Vector2 DeserializeData(ObjectDataBuffer loadDataBuffer, DeserializeReferenceBuilder deserializeReferenceBuilder)
        {
            var x = (float)loadDataBuffer.SaveElements["x"];
            var y = (float)loadDataBuffer.SaveElements["y"];

            return new Vector2(x, y);
        }
    }
}
