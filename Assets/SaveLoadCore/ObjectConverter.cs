using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace SaveLoadCore
{
    public static class ObjectConverter
    {
        private static Dictionary<Type, IConvertable> _convertableList = new();

        public static bool TryGetConverter(Type type, out IConvertable convertable)
        {
            _convertableList ??= new Dictionary<Type, IConvertable>();
            
            if (_convertableList.Count == 0)
            {
                Initialize();
            }
            
            return _convertableList.TryGetValue(type, out convertable);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInitialization()
        {
            Initialize();
        }

        private static void Initialize()
        {
            // Get the assembly containing the target classes
            Assembly assembly = Assembly.GetExecutingAssembly();
        
            // Find all types that implement IMyInterface
            IEnumerable<Type> types = assembly.GetTypes()
                .Where(t => typeof(IConvertable).IsAssignableFrom(t) && !t.IsAbstract);
        
            foreach (Type type in types)
            {
                IConvertable instance = (IConvertable)Activator.CreateInstance(type);
                AddConverter(instance);
            }
        }

        private static void AddConverter(IConvertable convertable)
        {
            var type = convertable.GetConvertType();
            if (_convertableList.TryAdd(type, convertable)) return;

            Debug.LogWarning($"Type of {type} is already registered!");
        }
    }

    public interface IConvertable
    {
        Type GetConvertType();
        void OnSave(ObjectDataBuffer saveDataBuffer, object data);
        object OnLoad(ObjectDataBuffer loadDataBuffer);
    }

    /// <summary>
    /// Doesn't support references by choice
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseConverter<T> : IConvertable
    {
        public Type GetConvertType()
        {
            return typeof(T);
        }

        public void OnSave(ObjectDataBuffer saveDataBuffer, object data)
        {
            InternalOnSave(saveDataBuffer, (T)data);
        }

        protected abstract void InternalOnSave(ObjectDataBuffer objectDataBuffer, T data);

        public object OnLoad(ObjectDataBuffer loadDataBuffer)
        {
            return InternalOnLoad(loadDataBuffer);
        }
        
        protected abstract T InternalOnLoad(ObjectDataBuffer loadDataBuffer);
    }
    
    [UsedImplicitly]
    public class Color32Converter : BaseConverter<Color32>
    {
        protected override void InternalOnSave(ObjectDataBuffer objectDataBuffer, Color32 data)
        {
            objectDataBuffer.SaveElements.Add("r", data.r);
            objectDataBuffer.SaveElements.Add("g", data.g);
            objectDataBuffer.SaveElements.Add("b", data.b);
            objectDataBuffer.SaveElements.Add("a", data.a);
        }

        protected override Color32 InternalOnLoad(ObjectDataBuffer loadDataBuffer)
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
        protected override void InternalOnSave(ObjectDataBuffer objectDataBuffer, Color data)
        {
            objectDataBuffer.SaveElements.Add("r", data.r);
            objectDataBuffer.SaveElements.Add("g", data.g);
            objectDataBuffer.SaveElements.Add("b", data.b);
            objectDataBuffer.SaveElements.Add("a", data.a);
        }

        protected override Color InternalOnLoad(ObjectDataBuffer loadDataBuffer)
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
        protected override void InternalOnSave(ObjectDataBuffer objectDataBuffer, Quaternion data)
        {
            objectDataBuffer.SaveElements.Add("x", data.x);
            objectDataBuffer.SaveElements.Add("y", data.y);
            objectDataBuffer.SaveElements.Add("z", data.z);
            objectDataBuffer.SaveElements.Add("w", data.w);
        }

        protected override Quaternion InternalOnLoad(ObjectDataBuffer loadDataBuffer)
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
        protected override void InternalOnSave(ObjectDataBuffer objectDataBuffer, Vector4 data)
        {
            objectDataBuffer.SaveElements.Add("x", data.x);
            objectDataBuffer.SaveElements.Add("y", data.y);
            objectDataBuffer.SaveElements.Add("z", data.z);
            objectDataBuffer.SaveElements.Add("w", data.w);
        }

        protected override Vector4 InternalOnLoad(ObjectDataBuffer loadDataBuffer)
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
        protected override void InternalOnSave(ObjectDataBuffer objectDataBuffer, Vector3 data)
        {
            objectDataBuffer.SaveElements.Add("x", data.x);
            objectDataBuffer.SaveElements.Add("y", data.y);
            objectDataBuffer.SaveElements.Add("z", data.z);
        }

        protected override Vector3 InternalOnLoad(ObjectDataBuffer loadDataBuffer)
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
        protected override void InternalOnSave(ObjectDataBuffer objectDataBuffer, Vector2 data)
        {
            objectDataBuffer.SaveElements.Add("x", data.x);
            objectDataBuffer.SaveElements.Add("y", data.y);
        }

        protected override Vector2 InternalOnLoad(ObjectDataBuffer loadDataBuffer)
        {
            var x = (float)loadDataBuffer.SaveElements["x"];
            var y = (float)loadDataBuffer.SaveElements["y"];

            return new Vector2(x, y);
        }
    }
}
