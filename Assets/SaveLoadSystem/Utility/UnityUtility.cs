using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
        
        public static List<T> FindObjectsOfTypeInScene<T>(Scene scene, bool includeInactive) where T : Object
        {
            var objectsInScene = new List<T>();
            if (!scene.isLoaded) return objectsInScene;
            
            var rootObjects = scene.GetRootGameObjects();
            foreach (GameObject go in rootObjects)
            {
                var children = go.GetComponentsInChildren<T>(includeInactive);
                objectsInScene.AddRange(children);
            }
            return objectsInScene;
        }
        
        public static T FindObjectOfTypeInScene<T>(Scene scene, bool includeInactive) where T : Object
        {
            if (!scene.isLoaded) return null;
            
            var rootObjects = scene.GetRootGameObjects();
            foreach (GameObject go in rootObjects)
            {
                var component = go.GetComponentInChildren<T>(includeInactive);
                if (component != null)
                {
                    return component;
                }
            }
            
            return null;
        }

        public static List<T> FindObjectsOfTypeInAllScenes<T>(bool includeInactive) where T : Object
        {
            var objectsInAllScenes = new List<T>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                objectsInAllScenes.AddRange(FindObjectsOfTypeInScene<T>(scene, includeInactive));
            }
            return objectsInAllScenes;
        }
        
        public static Scene[] GetActiveScenes()
        {
            Scene[] activeScenes = new Scene[SceneManager.sceneCount];
            
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                activeScenes[index] = SceneManager.GetSceneAt(index);
            }

            return activeScenes;
        }
    }
}
