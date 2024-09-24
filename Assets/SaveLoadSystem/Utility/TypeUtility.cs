using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SaveLoadSystem.Utility
{
    public static class TypeUtility
    {
        private static BindingFlags InheritedBindingFlags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static BindingFlags DeclaredOnlyBindingFlags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        
        public static void TryApplyObjectToMember(object memberOwner, string memberName, object data, bool debug = false)
        {
            var fieldInfo = memberOwner.GetType().GetField(memberName, InheritedBindingFlags);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(memberOwner, data);
                return;
            }
            
            var propertyInfo = memberOwner.GetType().GetProperty(memberName, InheritedBindingFlags);
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
        
        public static void TryApplyJsonObjectToMember(object memberOwner, string memberName, JToken data, bool debug = false)
        {
            var fieldInfo = memberOwner.GetType().GetField(memberName, InheritedBindingFlags);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(memberOwner, data.ToObject(fieldInfo.FieldType));
                return;
            }
            
            var propertyInfo = memberOwner.GetType().GetProperty(memberName, InheritedBindingFlags);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(memberOwner, data.ToObject(propertyInfo.PropertyType));
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
            var fields = type.GetFields(InheritedBindingFlags);
            foreach (var field in fields)
            {
                // Check if the field has the specified attribute
                if (field.GetCustomAttributes<T>().Any())
                {
                    instances.Add(field.Name);
                }
            }

            // Get all properties of the type
            var properties = type.GetProperties(InheritedBindingFlags);
            foreach (var property in properties)
            {
                // Check if the property has the specified attribute
                if (property.GetCustomAttributes<T>().Any())
                {
                    instances.Add(property.Name);
                }
            }
        }
        
        public static FieldInfo[] GetFieldInfos(Type type, bool declaredOnly)
        {
            if (declaredOnly)
            {
                return type.GetFields(DeclaredOnlyBindingFlags);
            }

            return type.GetFields(InheritedBindingFlags);
        }
        
        public static List<FieldInfo> GetFieldInfosWithAttribute<T>(Type type) where T : Attribute
        {
            var foundFieldInfos = new List<FieldInfo>();
            
            // Get all fields of the type
            var fields = type.GetFields(InheritedBindingFlags);
            foreach (var field in fields)
            {
                // Check if the field has the specified attribute
                if (field.GetCustomAttributes<T>().Any())
                {
                    foundFieldInfos.Add(field);
                }
            }

            return foundFieldInfos;
        }
        
        public static PropertyInfo[] GetPropertyInfos(Type type, bool declaredOnly)
        {
            if (declaredOnly)
            {
                return type.GetProperties(DeclaredOnlyBindingFlags);
            }

            return type.GetProperties(InheritedBindingFlags);
        }

        public static List<PropertyInfo> GetPropertyInfosWithAttribute<T>(Type type) where T : Attribute
        {
            var foundPropertyInfos = new List<PropertyInfo>();
            
            // Get all properties of the type
            var properties = type.GetProperties(InheritedBindingFlags);
            foreach (var property in properties)
            {
                // Check if the property has the specified attribute
                if (property.GetCustomAttributes<T>().Any())
                {
                    foundPropertyInfos.Add(property);
                }
            }

            return foundPropertyInfos;
        }

        public static bool ContainsField<T>(Type type) where T : Attribute
        {
            // Get all fields of the type
            var fields = type.GetFields(InheritedBindingFlags);
            foreach (var field in fields)
            {
                // Check if the field has the specified attribute
                if (field.GetCustomAttributes<T>().Any())
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
        
        public static bool IsSerializable(this Type type)
        {
            // Check if the type is marked with the [Serializable] attribute
            if (type.IsSerializable)
            {
                return true;
            }

            // Check if the type implements the ISerializable interface
            if (typeof(ISerializable).IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }
        
        public static bool ClassHasAttribute<T>(Type type) where T : Attribute
        {
            return type.GetCustomAttributes<T>().Any();
        }

        public static bool TryGetAttribute<T>(Type type, out T attribute) where T : Attribute
        {
            attribute = type.GetCustomAttribute<T>();
            return attribute != null;
        }

        public static bool ContainsProperty<T>(Type type) where T : Attribute
        {
            // Get all properties of the type
            var properties = type.GetProperties(InheritedBindingFlags);
            foreach (var property in properties)
            {
                // Check if the property has the specified attribute
                if (property.GetCustomAttributes<T>().Any())
                {
                    return true;
                }
            }

            return false;
        }
        
        public static List<UnityEngine.Object> GetComponentsWithTypeCondition(GameObject gameObject, params Func<Type, bool>[] collectionConditions)
        {
            var componentsWithAttribute = new List<UnityEngine.Object>();
            var allComponents = gameObject.GetComponents<Component>();

            foreach (Component component in allComponents)
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
