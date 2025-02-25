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
            SceneDataLookup[scene.name] = sceneSaveData;
        }

        public bool ContainsSceneData(Scene scene)
        {
            return SceneDataLookup.ContainsKey(scene.name);
        }

        public SceneSaveData GetSceneData(Scene scene)
        {
            return SceneDataLookup[scene.name];
        }

        public bool TryGetSceneData(Scene scene, out SceneSaveData sceneSaveData)
        {
            return SceneDataLookup.TryGetValue(scene.name, out sceneSaveData);
        }

        public void RemoveSceneData(Scene scene)
        {
            SceneDataLookup.Remove(scene.name);
        }
    }
}
