using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SaveLoadSystem.Utility
{
    internal static class SaveLoadUtility
    {
        public const string ScriptableObjectDataName = "ScriptableObjects";
        
        /// <summary>
        /// Checks if a GameObject has been destroyed.
        /// </summary>
        /// <param name="gameObject">GameObject reference to check for destructedness</param>
        /// <returns>If the game object has been marked as destroyed by UnityEngine</returns>
        internal static bool IsDestroyed(this GameObject gameObject)
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
        internal static bool IsUnityNull<T>(this T obj)
        {
            return obj == null || obj.Equals(null);
        }
        
        /// <summary>
        /// Changes made to serialized fields via script in Edit mode are not automatically saved.
        /// Unity only persists modifications made through the Inspector unless explicitly marked as dirty.
        /// </summary>
        internal static void SetDirty(Object target)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.SetDirty(target);
            }
#endif
        }
        
        #region Id Handling

        
        /// <summary>
        /// Generates a random ID of the specified length.
        /// </summary>
        /// <param name="length">The length of the ID to generate. Default is 5.</param>
        /// <returns>A random string ID.</returns>
        internal static string GenerateId(int length = 5)
        {
            const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz123456789";
            
            var id = new char[length];
            for (var i = 0; i < length; i++)
            {
                // Random.Range with int parameters is inclusive on the lower bound and exclusive on the upper bound.
                var index = UnityEngine.Random.Range(0, allowedChars.Length);
                id[i] = allowedChars[index];
            }
            return new string(id);
        }
        
        internal static void CheckUniqueGuidOnInspectorInput<T>(IEnumerable<T> items, 
            Func<T, UnityEngine.Object> getUnityObject, Func<T, string> getGuid, string errorMessagePrefix)
        {
            var guidLookup = new Dictionary<string, (bool Duplicated, HashSet<string> HashSet)>();

            foreach (var item in items)
            {
                var unityObject = getUnityObject(item);
                if (unityObject.IsUnityNull()) continue;
                
                var guid = getGuid(item);
                if (string.IsNullOrEmpty(guid)) continue;

                var stringToAdd = $"'{unityObject.name}'";
                if (!guidLookup.TryGetValue(guid, out var uniqueWithCount))
                {
                    guidLookup.Add(guid, (false, new HashSet<string> { stringToAdd }));
                }
                else
                {
                    uniqueWithCount.HashSet.Add(stringToAdd);
                    guidLookup[guid] = (true, uniqueWithCount.HashSet);
                }
            }

            foreach (var (guid, uniqueWithCount) in guidLookup)
            {
                if (uniqueWithCount.Duplicated)
                {
                    Debug.LogError($"{errorMessagePrefix}. Please ensure all GUIDs are unique. Duplicated guid: '{guid}'. Duplications detected on: {string.Join(" | ", uniqueWithCount.HashSet)}.");
                }
            }
        }

        
        #endregion

        #region Type Condition

        
        internal static bool ContainsType<T>(Type type) where T : class
        {
            return typeof(T).IsAssignableFrom(type);
        }
        
        internal static List<Component> GetComponentsWithTypeCondition(GameObject gameObject, params Func<Type, bool>[] collectionConditions)
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

        
        #endregion

        #region Duplicate Component

        
        /// <summary>
        /// Finds and returns all components that are attached multiple times to the given GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to check.</param>
        /// <returns>A flat list of all components that are added multiple times.</returns>
        internal static List<Component> GetDuplicateComponents(GameObject gameObject)
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
        

        #endregion

        #region Scene Handling

        
        /// <summary>
        /// Checks if the scene is completely gone — not loaded and not in the SceneManager at all.
        /// </summary>
        internal static bool IsSceneUnloaded(string sceneName)
        {
            return IsSceneUnloaded(SceneManager.GetSceneByName(sceneName));
        }
        
        /// <summary>
        /// Checks if the scene is completely gone — not loaded and not in the SceneManager at all.
        /// </summary>
        private static bool IsSceneUnloaded(Scene scene)
        {
            return !scene.isLoaded && !IsSceneInManager(scene);
        }

        /// <summary>
        /// Internal helper: determines whether the scene is still tracked by the SceneManager.
        /// </summary>
        private static bool IsSceneInManager(Scene scene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i) == scene)
                    return true;
            }
            return false;
        }

        
        #endregion
    }
}
