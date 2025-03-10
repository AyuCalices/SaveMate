using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;

namespace SaveLoadSystem.Core.DataTransferObject
{
    public class RootSaveData
    {
        public const string GlobalSaveDataName = "GLOBAL";
        
        [UsedImplicitly] public List<string> ActiveScenes { get; set; } = new();
        [UsedImplicitly] public BranchSaveData GlobalSaveData { get; set; } = new();
        [UsedImplicitly] public Dictionary<string, SceneData> SceneDataLookup { get; set; } = new();
        
        public void SetActiveScenes(List<string> activeScenes)
        {
            ActiveScenes = activeScenes;
        }

        public void SetSceneData(string sceneName, SceneData sceneData)
        {
            SceneDataLookup[sceneName] = new SceneData
            {
                ActivePrefabs = sceneData.ActivePrefabs, 
                ActiveSaveData = sceneData.ActiveSaveData
            };
        }

        public bool TryGetSceneData(string sceneName, out SceneData sceneData)
        {
            return SceneDataLookup.TryGetValue(sceneName, out sceneData);
        }

        public void RemoveSceneData(string sceneName)
        {
            SceneDataLookup.Remove(sceneName);
        }
    }
}
