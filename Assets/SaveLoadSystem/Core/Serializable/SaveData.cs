using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.Serializable
{
    [Serializable]
    public class SaveData
    {
        private readonly Dictionary<string, SceneDataContainer> _sceneDataLookup = new();

        public void SetSceneData(Scene scene, SceneDataContainer sceneDataContainer)
        {
            _sceneDataLookup[scene.path] = sceneDataContainer;
        }

        public bool ContainsSceneData(Scene scene)
        {
            return _sceneDataLookup.ContainsKey(scene.path);
        }

        public SceneDataContainer GetSceneData(Scene scene)
        {
            return _sceneDataLookup[scene.path];
        }

        public bool TryGetSceneData(Scene scene, out SceneDataContainer sceneDataContainer)
        {
            return _sceneDataLookup.TryGetValue(scene.path, out sceneDataContainer);
        }

        public void RemoveSceneData(Scene scene)
        {
            _sceneDataLookup.Remove(scene.path);
        }
    }
}
