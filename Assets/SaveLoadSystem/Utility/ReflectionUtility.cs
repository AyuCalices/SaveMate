using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SaveLoadSystem.Utility
{
    public static class ReflectionUtility
    {
        public static BindingFlags DefaultBindingFlags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        
        public static void TryApplyMemberValue(object memberOwner, string memberName, object data, bool debug = false)
        {
            var fieldInfo = memberOwner.GetType().GetField(memberName, DefaultBindingFlags);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(memberOwner, data);
                return;
            }
            
            var propertyInfo = memberOwner.GetType().GetProperty(memberName, DefaultBindingFlags);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(memberOwner, data);
                return;
            }
            
            if (debug)
            {
                Debug.LogWarning($"The requested property with name '{memberName}' was not found on type '{memberOwner.GetType()}'!");
            }
        }

        public static void GetFieldsAndPropertiesWithAttributeOnType<T>(Type type, ref List<string> instances) where T : Attribute
        {
            // Get all fields of the type
            var fields = type.GetFields(DefaultBindingFlags);
            foreach (var field in fields)
            {
                // Check if the field has the specified attribute
                if (field.GetCustomAttributes(typeof(T), false).Length > 0)
                {
                    instances.Add(field.Name);
                }
            }

            // Get all properties of the type
            var properties = type.GetProperties(DefaultBindingFlags);
            foreach (var property in properties)
            {
                // Check if the property has the specified attribute
                if (property.GetCustomAttributes(typeof(T), false).Length > 0)
                {
                    instances.Add(property.Name);
                }
            }
        }
        
        public static FieldInfo[] GetFieldInfos(Type type)
        {
            return type.GetFields(DefaultBindingFlags);
        }
        
        public static List<FieldInfo> GetFieldInfosWithAttribute<T>(Type type) where T : Attribute
        {
            var foundFieldInfos = new List<FieldInfo>();
            
            // Get all fields of the type
            var fields = type.GetFields(DefaultBindingFlags);
            foreach (var field in fields)
            {
                // Check if the field has the specified attribute
                if (field.GetCustomAttributes(typeof(T), false).Length > 0)
                {
                    foundFieldInfos.Add(field);
                }
            }

            return foundFieldInfos;
        }
        
        public static PropertyInfo[] GetPropertyInfos(Type type)
        {
            return type.GetProperties(DefaultBindingFlags);
        }

        public static List<PropertyInfo> GetPropertyInfosWithAttribute<T>(Type type) where T : Attribute
        {
            var foundPropertyInfos = new List<PropertyInfo>();
            
            // Get all properties of the type
            var properties = type.GetProperties(DefaultBindingFlags);
            foreach (var property in properties)
            {
                // Check if the property has the specified attribute
                if (property.GetCustomAttributes(typeof(T), false).Length > 0)
                {
                    foundPropertyInfos.Add(property);
                }
            }

            return foundPropertyInfos;
        }

        public static bool ContainsField<T>(Type type) where T : Attribute
        {
            // Get all fields of the type
            var fields = type.GetFields(DefaultBindingFlags);
            foreach (var field in fields)
            {
                // Check if the field has the specified attribute
                if (field.GetCustomAttributes(typeof(T), false).Length > 0)
                {
                    return true;
                }
            }

            return false;
        }
        
        public static bool ContainsInterface<T>(Type type) where T : class
        {
            return typeof(T).IsAssignableFrom(type);
        }
        
        public static bool ClassHasAttribute<T>(Type type) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), false).Length > 0;
        }

        public static bool ContainsProperty<T>(Type type) where T : Attribute
        {
            // Get all properties of the type
            var properties = type.GetProperties(DefaultBindingFlags);
            foreach (var property in properties)
            {
                // Check if the property has the specified attribute
                if (property.GetCustomAttributes(typeof(T), false).Length > 0)
                {
                    return true;
                }
            }

            return false;
        }
        
        public static List<Component> GetComponentsWithTypeCondition(GameObject gameObject, params Func<Type, bool>[] collectionConditions)
        {
            var componentsWithAttribute = new List<Component>();
            var allComponents = gameObject.GetComponents<Component>();

            foreach (Component component in allComponents)
            {
                if (component == null) continue;
                
                var componentType = component.GetType();
                foreach (Func<Type,bool> condition in collectionConditions)
                {
                    if (condition.Invoke(componentType))
                    {
                        componentsWithAttribute.Add(component);
                    }
                }
            }

            return componentsWithAttribute;
        }
    }
}
