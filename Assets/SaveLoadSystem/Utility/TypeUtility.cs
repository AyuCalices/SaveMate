using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveLoadSystem.Utility
{
    public static class TypeUtility
    {
        public static bool ContainsType<T>(Type type) where T : class
        {
            return typeof(T).IsAssignableFrom(type);
        }
        
        public static bool TryConvertTo<T>(object instance, out T convertedType) where T : class
        {
            if (instance is T validInstance)
            {
                convertedType = validInstance;
                return true;
            }

            convertedType = null;
            return false;
        }
        
        public static List<Component> GetComponentsWithTypeCondition(GameObject gameObject, params Func<Type, bool>[] collectionConditions)
        {
            var componentsWithAttribute = new List<Component>();
            var allComponents = gameObject.GetComponents<Component>();

            foreach (var component in allComponents)
            {
                if (component == null) continue;
                
                var componentType = component.GetType();
                foreach (Func<Type,bool> condition in collectionConditions)
                {
                    if (condition.Invoke(componentType) && !componentsWithAttribute.Contains(component))
                    {
                        componentsWithAttribute.Add(component);
                    }
                }
            }

            return componentsWithAttribute;
        }
    }
}
