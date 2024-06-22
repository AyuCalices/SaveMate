using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SaveLoadCore.Utility
{
    public static class GameObjectExtensions
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
        
        public static List<T> FindObjectsOfTypeInScene<T>(Scene scene, bool includeInactive) where T : Object
        {
            List<T> objectsInScene = new List<T>();
            if (scene.isLoaded)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject go in rootObjects)
                {
                    T[] children = go.GetComponentsInChildren<T>(includeInactive);
                    objectsInScene.AddRange(children);
                }
            }
            return objectsInScene;
        }

        public static List<T> FindObjectsOfTypeInAllScenes<T>(bool includeInactive) where T : Object
        {
            List<T> objectsInAllScenes = new List<T>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                objectsInAllScenes.AddRange(FindObjectsOfTypeInScene<T>(scene, includeInactive));
            }
            return objectsInAllScenes;
        }
    }
    
    public static class SerializationHelper
    {
        public static bool IsSerializable(Type type)
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
    }
}
