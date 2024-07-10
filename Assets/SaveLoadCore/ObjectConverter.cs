using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SaveLoadCore.Utility;
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

        public static bool AddConverter(IConvertable convertable)
        {
            var type = convertable.GetConvertType();
            if (_convertableList.TryAdd(type, convertable)) return true;
            
            Debug.LogWarning($"Type of {type} is already registered!");
            return false;
        }

        public static bool RemoveConverter(IConvertable convertable)
        {
            var type = convertable.GetConvertType();
            if (!_convertableList.ContainsKey(type))
            {
                Debug.LogWarning($"Type of {type} is not registered!");
                return false;
            }
            
            _convertableList.Remove(type);
            return true;
        }
    }

    public interface IConvertable
    {
        Type GetConvertType();
        void OnSave(ObjectDataBuffer saveDataBuffer, object data, SaveElementLookup saveElementLookup);
        object OnLoad(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder);
    }

    public abstract class BaseConverter<T> : IConvertable
    {
        //anything, that it present in the saveElementLookup can be used -> IConvertables are not contained which means they are not usable as referencables
        //TODO: might not even be relevant
        protected SaveElementLookup SaveElementLookup;
        protected ReferenceBuilder ReferenceBuilder;
        
        public Type GetConvertType()
        {
            return typeof(T);
        }

        public void OnSave(ObjectDataBuffer saveDataBuffer, object data, SaveElementLookup saveElementLookup)
        {
            SaveElementLookup = saveElementLookup;
            InternalOnSave(saveDataBuffer, (T)data);
        }

        protected abstract void InternalOnSave(ObjectDataBuffer objectDataBuffer, T data);

        public object OnLoad(ObjectDataBuffer loadDataBuffer, ReferenceBuilder referenceBuilder)
        {
            ReferenceBuilder = referenceBuilder;
            return InternalOnLoad(loadDataBuffer);
        }
        
        protected abstract T InternalOnLoad(ObjectDataBuffer loadDataBuffer);
    }

    [UsedImplicitly]
    public class Vector3Converter : BaseConverter<Vector3>
    {
        protected override void InternalOnSave(ObjectDataBuffer objectDataBuffer, Vector3 data)
        {
            objectDataBuffer.SaveElements.Add(("x", data.x));
            objectDataBuffer.SaveElements.Add(("y", data.y));
            objectDataBuffer.SaveElements.Add(("z", data.z));
        }

        protected override Vector3 InternalOnLoad(ObjectDataBuffer loadDataBuffer)
        {
            //TODO: use dictionary
            var x = (float)loadDataBuffer.SaveElements.Find(x => x.fieldName == "x").obj;
            var y = (float)loadDataBuffer.SaveElements.Find(x => x.fieldName == "y").obj;
            var z = (float)loadDataBuffer.SaveElements.Find(x => x.fieldName == "z").obj;

            return new Vector3(x, y, z);
        }
    }
    
    /// <summary>
    /// TODO: this must act similar to the member system, just that the gathering of the savable elements is different -> counts the same for an savable class attribute
    /// in here, a nested layer of potentional collection is needed
    /// </summary>
    [UsedImplicitly]
    public class EnumerableConverter : BaseConverter<IList>
    {
        protected override void InternalOnSave(ObjectDataBuffer objectDataBuffer, IList data)
        {
            var index = 0;

            var listElements = new List<object>();
            foreach (var obj in data)
            {
                //it is definitely an object with a savable attribute on it
                if (SaveElementLookup.Elements.TryGetValue(obj, out SaveElement element))
                {
                    listElements.Add(element.CreatorPath);
                    
                }
                //or new buffer is needed
                else if (ObjectConverter.TryGetConverter(obj.GetType(), out IConvertable convertable))
                {
                    //listElements.Add(convertable.OnSave());
                }
                else if (SerializationHelper.IsSerializable(obj.GetType()))
                {
                    
                }
                else
                {
                    Debug.LogWarning($"The object of type {obj.GetType()} is not supported!");
                }

                index++;
            }
            
            objectDataBuffer.SaveElements.Add(("elements", listElements));
        }

        protected override IList InternalOnLoad(ObjectDataBuffer loadDataBuffer)
        {
            var list = (List<object>)loadDataBuffer.SaveElements.Find(x => x.fieldName == "elements").obj;
            ReferenceBuilder.StoreAction(BuildReference);
            return list;

            void BuildReference(SceneElementComposite sceneElementComposite)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i] is not GuidPath targetGuidPath) continue;
                    
                    if (sceneElementComposite.FindTargetComposite(targetGuidPath.ToStack()) is not ElementComposite targetComposite)
                    {
                        Debug.LogWarning("Wasn't able to find the corresponding composite!");
                        return;
                    }

                    list[i] = targetComposite.SavableObject;
                }
            }
        }
    }
}
