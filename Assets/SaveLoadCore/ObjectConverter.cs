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
        private static readonly List<IConverterFactory> Factories = new List<IConverterFactory>();

        static ConverterFactoryRegistry()
        {
            Factories.Add(new ListConverter());
            Factories.Add(new Color32Converter());
            Factories.Add(new ColorConverter());
            Factories.Add(new Vector2Converter());
            Factories.Add(new Vector3Converter());
            Factories.Add(new Vector4Converter());
            Factories.Add(new QuaternionConverter());
            // Add other converter as needed
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
        void OnSave(ObjectDataBuffer saveDataBuffer, object data, SaveElementLookup saveElementLookup, int currentIndex);
        object OnLoad(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder);
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
        
        public void OnSave(ObjectDataBuffer saveDataBuffer, object data, SaveElementLookup saveElementLookup, int currentIndex)
        {
            SerializeData(saveDataBuffer, (T)data, saveElementLookup, currentIndex);
        }

        protected abstract void SerializeData(ObjectDataBuffer objectDataBuffer, T data, SaveElementLookup saveElementLookup, int currentIndex);

        public object OnLoad(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder)
        {
            return DeserializeData(loadDataBuffer, referenceBuilder);
        }
        
        protected abstract T DeserializeData(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder);
    }

    [UsedImplicitly]
    public class ListConverter : BaseConverter<IList>
    {
        protected override void SerializeData(ObjectDataBuffer objectDataBuffer, IList data, SaveElementLookup saveElementLookup, int currentIndex)
        {
            //TODO: developer just wants to add anything into a buffer and references are resolved magically
            
            var listElements = new List<object>();
            for (var index = 0; index < data.Count; index++)
            {
                var obj = data[index];

                //from here
                if (!saveElementLookup.ContainsElement(obj))
                {
                    var guidPath = new GuidPath(objectDataBuffer.OriginGuidPath, index.ToString());
                    SaveSceneManager.ProcessSavableElement(saveElementLookup, obj, guidPath, currentIndex + 1);
                }
                
                if (saveElementLookup.TryGetValue(obj, out SaveElement saveElement))
                {
                    if (saveElement.SaveStrategy == SaveStrategy.Serializable)
                    {
                        listElements.Add(saveElement.Obj);
                    }
                    else
                    {
                        listElements.Add(saveElement.CreatorGuidPath);
                    }
                }
                else
                {
                    Debug.LogWarning("Couldn't add!");
                }
                //to here is generic! -> just add
            }
            objectDataBuffer.SaveElements.Add("elements", listElements);
            
            var containedType = data.GetType().GetGenericArguments()[0];
            objectDataBuffer.SaveElements.Add("type", containedType);
        }

        protected override IList DeserializeData(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder)
        {
            var saveElements = (List<object>)loadDataBuffer.SaveElements["elements"];
            
            //the activator will always intialize value types with default values
            var type = (Type)loadDataBuffer.SaveElements["type"];
            var defaultValue = type.IsValueType ? Activator.CreateInstance(type) : null;
            
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
            for (int index = 0; index < saveElements.Count; index++)
            {
                var saveElement = saveElements[index];
                if (saveElement is GuidPath referenceGuidPath)
                {
                    list.Add(defaultValue);
                    ApplyReferenceBuilder(referenceBuilder, list, index, referenceGuidPath);
                }
                else
                {
                    list.Add(saveElement);
                }
            }

            return list;
        }

        private void ApplyReferenceBuilder(ReferenceBuilder referenceBuilder, IList list, int index, GuidPath targetGuidPath)
        {
            referenceBuilder.StoreAction(targetGuidPath, targetObject => list[index] = targetObject);
        }
    }
    
    [UsedImplicitly]
    public class Color32Converter : BaseConverter<Color32>
    {
        protected override void SerializeData(ObjectDataBuffer objectDataBuffer, Color32 data, SaveElementLookup saveElementLookup, int currentIndex)
        {
            objectDataBuffer.SaveElements.Add("r", data.r);
            objectDataBuffer.SaveElements.Add("g", data.g);
            objectDataBuffer.SaveElements.Add("b", data.b);
            objectDataBuffer.SaveElements.Add("a", data.a);
        }

        protected override Color32 DeserializeData(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder)
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
        protected override void SerializeData(ObjectDataBuffer objectDataBuffer, Color data, SaveElementLookup saveElementLookup, int currentIndex)
        {
            objectDataBuffer.SaveElements.Add("r", data.r);
            objectDataBuffer.SaveElements.Add("g", data.g);
            objectDataBuffer.SaveElements.Add("b", data.b);
            objectDataBuffer.SaveElements.Add("a", data.a);
        }

        protected override Color DeserializeData(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder)
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
        protected override void SerializeData(ObjectDataBuffer objectDataBuffer, Quaternion data, SaveElementLookup saveElementLookup, int currentIndex)
        {
            objectDataBuffer.SaveElements.Add("x", data.x);
            objectDataBuffer.SaveElements.Add("y", data.y);
            objectDataBuffer.SaveElements.Add("z", data.z);
            objectDataBuffer.SaveElements.Add("w", data.w);
        }

        protected override Quaternion DeserializeData(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder)
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
        protected override void SerializeData(ObjectDataBuffer objectDataBuffer, Vector4 data, SaveElementLookup saveElementLookup, int currentIndex)
        {
            objectDataBuffer.SaveElements.Add("x", data.x);
            objectDataBuffer.SaveElements.Add("y", data.y);
            objectDataBuffer.SaveElements.Add("z", data.z);
            objectDataBuffer.SaveElements.Add("w", data.w);
        }

        protected override Vector4 DeserializeData(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder)
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
        protected override void SerializeData(ObjectDataBuffer objectDataBuffer, Vector3 data, SaveElementLookup saveElementLookup, int currentIndex)
        {
            objectDataBuffer.SaveElements.Add("x", data.x);
            objectDataBuffer.SaveElements.Add("y", data.y);
            objectDataBuffer.SaveElements.Add("z", data.z);
        }

        protected override Vector3 DeserializeData(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder)
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
        protected override void SerializeData(ObjectDataBuffer objectDataBuffer, Vector2 data, SaveElementLookup saveElementLookup, int currentIndex)
        {
            objectDataBuffer.SaveElements.Add("x", data.x);
            objectDataBuffer.SaveElements.Add("y", data.y);
        }

        protected override Vector2 DeserializeData(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder)
        {
            var x = (float)loadDataBuffer.SaveElements["x"];
            var y = (float)loadDataBuffer.SaveElements["y"];

            return new Vector2(x, y);
        }
    }
}
