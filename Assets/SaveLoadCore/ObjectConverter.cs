using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadCore
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

    public interface IConvertable
    {
        bool TryGetConverter(Type type, out IConvertable convertable);
        void OnSave(object data, SaveDataHandler saveDataHandler);
        void OnLoad(LoadDataHandler loadDataHandler);
    }
    
    public interface ISavable
    {
        void OnSave(SaveDataHandler saveDataHandler);
        void OnLoad(LoadDataHandler loadDataHandler);
    }

    /// <summary>
    /// Doesn't support references by choice
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseConverter<T> : IConvertable
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
        
        public void OnSave(object data, SaveDataHandler saveDataHandler)
        {
            OnSave((T)data, saveDataHandler);
        }

        protected abstract void OnSave(T data, SaveDataHandler saveDataHandler);

        public abstract void OnLoad(LoadDataHandler loadDataHandler);
    }
    
    [UsedImplicitly]
    public class StackConverter : BaseConverter<Stack>
    {
        protected override void OnSave(Stack data, SaveDataHandler saveDataHandler)
        {
            var saveElements = data.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveElements[index] = saveDataHandler.ToReferencableObject(index.ToString(), saveElements[index]);
            }
            saveDataHandler.AddSerializable("elements", saveElements);
            
            var containedType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("type", containedType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var loadElements = loadDataHandler.GetSaveElement<List<object>>("elements");
            var type = loadDataHandler.GetSaveElement<Type>("type");
            
            var stackType = typeof(Stack<>).MakeGenericType(type);
            var stack = (Stack)Activator.CreateInstance(stackType);
            
            loadDataHandler.InitializeInstance(stack);
            
            foreach (var saveElement in loadElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, foundObject => stack.Push(foundObject));
            }
        }
    }
    
    [UsedImplicitly]
    public class QueueConverter : BaseConverter<Queue>
    {
        protected override void OnSave(Queue data, SaveDataHandler saveDataHandler)
        {
            var saveElements = data.ToArray();
            for (var index = 0; index < saveElements.Length; index++)
            {
                saveElements[index] = saveDataHandler.ToReferencableObject(index.ToString(), saveElements[index]);
            }
            saveDataHandler.AddSerializable("elements", saveElements);
            
            var containedType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("type", containedType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var loadElements = loadDataHandler.GetSaveElement<List<object>>("elements");
            var type = loadDataHandler.GetSaveElement<Type>("type");
            
            var queueType = typeof(Queue<>).MakeGenericType(type);
            var queue = (Queue)Activator.CreateInstance(queueType);
            
            loadDataHandler.InitializeInstance(queue);
            
            foreach (var saveElement in loadElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, targetObject => queue.Enqueue(targetObject));
            }
        }
    }
    
    [UsedImplicitly]
    public class ArrayConverter : IConvertable
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
        
        public void OnSave(object data, SaveDataHandler saveDataHandler)
        {
            var saveElements = new List<object>();
            var index = 0;
            foreach (var obj in (Array)data)
            {
                var savable = saveDataHandler.ToReferencableObject(index.ToString(), obj);
                saveElements.Add(savable);
                index++;
            }
            saveDataHandler.AddSerializable("elements", saveElements);
            
            var containedType = data.GetType().GetElementType();
            saveDataHandler.AddSerializable("type", containedType);
        }

        public void OnLoad(LoadDataHandler loadDataHandler)
        {
            var loadElements = loadDataHandler.GetSaveElement<List<object>>("elements");
            var type = loadDataHandler.GetSaveElement<Type>("type");
            
            var array = Array.CreateInstance(type, loadElements.Count);
            
            loadDataHandler.InitializeInstance(array);

            for (var index = 0; index < loadElements.Count; index++)
            {
                var innerScopeIndex = index;
                loadDataHandler.EnqueueReferenceBuilding(loadElements[index], targetObject => array.SetValue(targetObject, innerScopeIndex));
            }
        }
    }

    [UsedImplicitly]
    public class ListConverter : BaseConverter<IList>
    {
        protected override void OnSave(IList data, SaveDataHandler saveDataHandler)
        {
            var listElements = new List<object>();
            for (var index = 0; index < data.Count; index++)
            {
                var savable = saveDataHandler.ToReferencableObject(index.ToString(), data[index]);
                listElements.Add(savable);
            }
            saveDataHandler.AddSerializable("elements", listElements);
            
            var containedType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("type", containedType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var saveElements = loadDataHandler.GetSaveElement<List<object>>("elements");
            var type = loadDataHandler.GetSaveElement<Type>("type");
            
            var listType = typeof(List<>).MakeGenericType(type);
            var list = (IList)Activator.CreateInstance(listType);
            
            loadDataHandler.InitializeInstance(list);
            
            foreach (var saveElement in saveElements)
            {
                loadDataHandler.EnqueueReferenceBuilding(saveElement, foundObject => list.Add(foundObject));
            }
        }
    }
    
    [UsedImplicitly]
    public class DictionaryConverter : BaseConverter<IDictionary>
    {
        protected override void OnSave(IDictionary data, SaveDataHandler saveDataHandler)
        {
            var listElements = new Dictionary<object, object>();

            var index = 0;
            foreach (var dataKey in data.Keys)
            {
                var savableKey = saveDataHandler.ToReferencableObject(index.ToString(), dataKey);
                var savableValue = saveDataHandler.ToReferencableObject(index.ToString(), data[dataKey]);
                listElements.Add(savableKey, savableValue);
                
                index++;
            }
            saveDataHandler.AddSerializable("elements", listElements);
            
            var keyType = data.GetType().GetGenericArguments()[0];
            saveDataHandler.AddSerializable("keyType", keyType);
            
            var valueType = data.GetType().GetGenericArguments()[1];
            saveDataHandler.AddSerializable("valueType", valueType);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var saveElements = loadDataHandler.GetSaveElement<Dictionary<object, object>>("elements");
            
            //the activator will always intialize value types with default values
            var keyType = loadDataHandler.GetSaveElement<Type>("keyType");
            var valueType = loadDataHandler.GetSaveElement<Type>("valueType");
            
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);
            
            loadDataHandler.InitializeInstance(dictionary);

            foreach (var (key, value) in saveElements)
            {
                var objectGroup = new[] { key, value };
                loadDataHandler.EnqueueReferenceBuilding(objectGroup, foundObject =>
                {
                    dictionary.Add(foundObject[0], foundObject[1]);
                });
            }
        }
    }
    
    [UsedImplicitly]
    public class Color32Converter : BaseConverter<Color32>
    {
        protected override void OnSave(Color32 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("r", data.r);
            saveDataHandler.AddSerializable("g", data.g);
            saveDataHandler.AddSerializable("b", data.b);
            saveDataHandler.AddSerializable("a", data.a);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var r = loadDataHandler.GetSaveElement<byte>("r");
            var g = loadDataHandler.GetSaveElement<byte>("g");
            var b = loadDataHandler.GetSaveElement<byte>("b");
            var a = loadDataHandler.GetSaveElement<byte>("a");
            
            loadDataHandler.InitializeInstance(new Color32(r, g, b, a));
        }
    }
    
    [UsedImplicitly]
    public class ColorConverter : BaseConverter<Color>
    {
        protected override void OnSave(Color data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("r", data.r);
            saveDataHandler.AddSerializable("g", data.g);
            saveDataHandler.AddSerializable("b", data.b);
            saveDataHandler.AddSerializable("a", data.a);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var r = loadDataHandler.GetSaveElement<float>("r");
            var g = loadDataHandler.GetSaveElement<float>("g");
            var b = loadDataHandler.GetSaveElement<float>("b");
            var a = loadDataHandler.GetSaveElement<float>("a");

            loadDataHandler.InitializeInstance(new Color(r, g, b, a));
        }
    }
    
    [UsedImplicitly]
    public class QuaternionConverter : BaseConverter<Quaternion>
    {
        protected override void OnSave(Quaternion data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("x", data.x);
            saveDataHandler.AddSerializable("y", data.y);
            saveDataHandler.AddSerializable("z", data.z);
            saveDataHandler.AddSerializable("w", data.w);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.GetSaveElement<float>("x");
            var y = loadDataHandler.GetSaveElement<float>("y");
            var z = loadDataHandler.GetSaveElement<float>("z");
            var w = loadDataHandler.GetSaveElement<float>("w");
            
            loadDataHandler.InitializeInstance(new Quaternion(x, y, z, w));
        }
    }
    
    [UsedImplicitly]
    public class Vector4Converter : BaseConverter<Vector4>
    {
        protected override void OnSave(Vector4 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("x", data.x);
            saveDataHandler.AddSerializable("y", data.y);
            saveDataHandler.AddSerializable("z", data.z);
            saveDataHandler.AddSerializable("w", data.w);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.GetSaveElement<float>("x");
            var y = loadDataHandler.GetSaveElement<float>("y");
            var z = loadDataHandler.GetSaveElement<float>("z");
            var w = loadDataHandler.GetSaveElement<float>("w");
            
            loadDataHandler.InitializeInstance(new Vector4(x, y, z, w));
        }
    }

    [UsedImplicitly]
    public class Vector3Converter : BaseConverter<Vector3>
    {
        protected override void OnSave(Vector3 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("x", data.x);
            saveDataHandler.AddSerializable("y", data.y);
            saveDataHandler.AddSerializable("z", data.z);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.GetSaveElement<float>("x");
            var y = loadDataHandler.GetSaveElement<float>("y");
            var z = loadDataHandler.GetSaveElement<float>("z");
            
            loadDataHandler.InitializeInstance(new Vector3(x, y, z));
        }
    }
    
    [UsedImplicitly]
    public class Vector2Converter : BaseConverter<Vector2>
    {
        protected override void OnSave(Vector2 data, SaveDataHandler saveDataHandler)
        {
            saveDataHandler.AddSerializable("x", data.x);
            saveDataHandler.AddSerializable("y", data.y);
        }

        public override void OnLoad(LoadDataHandler loadDataHandler)
        {
            var x = loadDataHandler.GetSaveElement<float>("x");
            var y = loadDataHandler.GetSaveElement<float>("y");
            
            loadDataHandler.InitializeInstance(new Vector2(x, y));
        }
    }
}
