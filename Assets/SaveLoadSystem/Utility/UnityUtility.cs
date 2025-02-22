using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Utility
{
    public static class UnityUtility
    {
        /// <summary>
        /// Checks if a GameObject has been destroyed.
        /// </summary>
        /// <param name="gameObject">GameObject reference to check for destructedness</param>
        /// <returns>If the game object has been marked as destroyed by UnityEngine</returns>
        public static bool IsDestroyed(this GameObject gameObject)
        {
            // UnityEngine overloads the == operator for the GameObject type
            // and returns null when the object has been destroyed, but 
            // actually the object is still there but has not been cleaned up yet
            // if we test both we can determine if the object has been destroyed.
            return gameObject == null && !ReferenceEquals(gameObject, null);
        }
        
        /// <summary>
        /// Checks if a UnityEngine.Object is null, taking into account Unity's special null handling.
        /// </summary>
        /// <typeparam name="T">Type of the UnityEngine.Object</typeparam>
        /// <param name="obj">The object to check</param>
        /// <returns>True if the object is null or has been destroyed, otherwise false.</returns>
        public static bool IsUnityNull<T>(this T obj)
        {
            return obj == null || obj.Equals(null);
        }
        
        public static List<T> FindObjectsOfTypeInScene<T>(Scene scene, bool includeInactive)
        {
            //if (!scene.isLoaded) return default;
            
            var objectsInScene = new List<T>();
            
            var rootObjects = scene.GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                var children = go.GetComponentsInChildren<T>(includeInactive);
                objectsInScene.AddRange(children);
            }
            
            return objectsInScene;
        }
        
        public static T FindObjectOfTypeInScene<T>(Scene scene, bool includeInactive)
        {
            //if (!scene.isLoaded) return default;
            
            var rootObjects = scene.GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                var component = go.GetComponentInChildren<T>(includeInactive);
                if (component != null)
                {
                    return component;
                }
            }
            
            return default;
        }
        
        /// <summary>
        /// Finds and returns all components that are attached multiple times to the given GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to check.</param>
        /// <returns>A flat list of all components that are added multiple times.</returns>
        public static List<Component> GetDuplicateComponents(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.LogError("Provided GameObject is null.");
                return null;
            }

            // Dictionary to track the count of each component type
            var componentMap = new Dictionary<System.Type, List<Component>>();

            // Get all components on the GameObject
            var allComponents = gameObject.GetComponents<Component>();

            // Populate the dictionary with component types and their instances
            foreach (var component in allComponents)
            {
                if (component == null) // Handle missing components
                    continue;

                System.Type type = component.GetType();
                if (!componentMap.ContainsKey(type))
                {
                    componentMap[type] = new List<Component>();
                }

                componentMap[type].Add(component);
            }

            // Collect components that occur more than once
            var duplicates = new List<Component>();
            foreach (var kvp in componentMap)
            {
                if (kvp.Value.Count > 1) // More than one instance of this component type
                {
                    duplicates.AddRange(kvp.Value);
                }
            }

            return duplicates;
        }
    }
}
