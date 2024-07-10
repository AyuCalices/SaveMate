using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SaveLoadCore
{
    //für jede instanz darf nur eine id vorhanden sein
    //1. alle instanzen genau einmal einsammeln und für jedes eine id vergeben
    //2. durch alle instanzen durchgehen und gucken, in welchen instanzen sie verwendet werden
    //3. in den instanzen, in denen sie verwendet werden, müssen die referenzen mit einer id ausgetauscht werden
    //4. beim deserialisieren werden erst alle klassen initialisiert -> der referenz type ist zunächst null
    //5. alle referenzen werden mittels der pointer aufgefüllt
    
    //1. gathering instances options:
    //1.1 eine RegisterSaveable methode für das manuelle registrieren
    //1.2 die hierarchy einer scene durchgehen & savables einsammeln -> scenen gebunden

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SavableAttribute : Attribute
    {
    }
    
    public static class ReflectionUtility
    {
        public static void ApplyMemberValue(MemberInfo memberInfo, object memberOwner, object data)
        {
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    fieldInfo.SetValue(memberOwner, data);
                    break;
                case PropertyInfo propertyInfo:
                    propertyInfo.SetValue(memberOwner, data);
                    break;
            }
        }
        
        public static Dictionary<Type, List<string>> GetFieldsAndPropertiesWithAttribute<T>() where T : Attribute
        {
            var typeLookup = new Dictionary<Type, List<string>>();
            
            // Get all types in the current assembly
            var types = Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
            {
                var newTypeElement = new List<string>();
                GetFieldsAndPropertiesWithAttributeOnType<T>(type, ref newTypeElement);
                typeLookup.Add(type, newTypeElement);
            }

            return typeLookup;
        }

        public static void GetFieldsAndPropertiesWithAttributeOnType<T>(Type type, ref List<string> instances) where T : Attribute
        {
            // Get all fields of the type
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var field in fields)
            {
                // Check if the field has the specified attribute
                if (field.GetCustomAttributes(typeof(T), false).Length > 0)
                {
                    instances.Add(field.Name);
                }
            }

            // Get all properties of the type
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var property in properties)
            {
                // Check if the property has the specified attribute
                if (property.GetCustomAttributes(typeof(T), false).Length > 0)
                {
                    instances.Add(property.Name);
                }
            }
        }
        
        public static FieldInfo GetFieldInfo(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }
        
        public static List<FieldInfo> GetFieldInfos<T>(Type type) where T : Attribute
        {
            var foundFieldInfos = new List<FieldInfo>();
            
            // Get all fields of the type
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
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
        
        public static PropertyInfo GetPropertyInfo(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }

        public static List<PropertyInfo> GetPropertyInfos<T>(Type type) where T : Attribute
        {
            var foundPropertyInfos = new List<PropertyInfo>();
            
            // Get all properties of the type
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
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
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
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

        public static bool ContainsProperty<T>(Type type) where T : Attribute
        {
            // Get all properties of the type
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
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