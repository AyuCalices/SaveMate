using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class SaveData
    {
        [JsonProperty] public readonly Dictionary<string, SceneSaveData> SceneDataLookup = new();

        public void SetSceneData(Scene scene, SceneSaveData sceneSaveData)
        {
            SceneDataLookup[scene.path] = sceneSaveData;
        }

        public bool ContainsSceneData(Scene scene)
        {
            return SceneDataLookup.ContainsKey(scene.path);
        }

        public SceneSaveData GetSceneData(Scene scene)
        {
            return SceneDataLookup[scene.path];
        }

        public bool TryGetSceneData(Scene scene, out SceneSaveData sceneSaveData)
        {
            return SceneDataLookup.TryGetValue(scene.path, out sceneSaveData);
        }

        public void RemoveSceneData(Scene scene)
        {
            SceneDataLookup.Remove(scene.path);
        }
    }
}
