using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.Serializable
{
    [Serializable]
    public class SaveData
    {
        public readonly Dictionary<string, SceneDataContainer> SceneDataLookup = new();

        public void SetSceneData(Scene scene, SceneDataContainer sceneDataContainer)
        {
            SceneDataLookup[scene.path] = sceneDataContainer;
        }

        public bool ContainsSceneData(Scene scene)
        {
            return SceneDataLookup.ContainsKey(scene.path);
        }

        public SceneDataContainer GetSceneData(Scene scene)
        {
            return SceneDataLookup[scene.path];
        }

        public bool TryGetSceneData(Scene scene, out SceneDataContainer sceneDataContainer)
        {
            return SceneDataLookup.TryGetValue(scene.path, out sceneDataContainer);
        }

        public void RemoveSceneData(Scene scene)
        {
            SceneDataLookup.Remove(scene.path);
        }
    }
}
